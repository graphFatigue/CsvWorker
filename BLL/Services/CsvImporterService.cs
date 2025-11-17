using CsvHelper;
using CsvWorker.BLL.DTOs;
using CsvWorker.BLL.Options;
using CsvWorker.DAL;
using CsvWorker.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text;

namespace CsvWorker.BLL.Services
{
    public class CsvImporterService
    {
        private readonly AppDbContext _db;
        private readonly CsvReaderService _reader;
        private readonly CsvImporterOptions _opts;
        private readonly TimeZoneInfo _estZone;

        public CsvImporterService(AppDbContext db, CsvReaderService reader, CsvImporterOptions? options = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _opts = options ?? new CsvImporterOptions();
            if (_opts.BatchSize <= 0) throw new ArgumentException("BatchSize must be positive.", nameof(options));

            _estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }


        public void Import(string csvFilePath, out int totalInserted)
        {
            totalInserted = 0;

            if (string.IsNullOrWhiteSpace(csvFilePath))
                throw new ArgumentException("CSV path must be provided.", nameof(csvFilePath));

            List<TripCsvDTO> dtos = _reader.GetRecords<TripCsvDTO>(csvFilePath);

            if (dtos == null || dtos.Count == 0)
                return;                          

            var seenKeys = new HashSet<string>();
            var duplicates = new List<TripCsvDTO>();
            var errors = new List<string>();

            foreach (var batch in dtos.Chunk(_opts.BatchSize))
            {
                var tripsToInsert = new List<Trip>();

                foreach (var dto in batch)
                {
                    try
                    {
                        dto.StoreAndFwdFlag = dto.StoreAndFwdFlag?.Trim();

                        if (!dto.PickupDateTime.HasValue || !dto.DropoffDateTime.HasValue)
                        {
                            errors.Add($"Skipped: missing pickup/dropoff datetime. Pickup='{dto.PickupDateTime}', Dropoff='{dto.DropoffDateTime}'");
                            continue;
                        }

                        var sFlag = dto.StoreAndFwdFlag?.ToUpperInvariant();
                        if (sFlag != "Y" && sFlag != "N")
                        {
                            errors.Add($"Skipped: invalid StoreAndFwdFlag '{dto.StoreAndFwdFlag}'");
                            continue;
                        }

                        var pickupUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.PickupDateTime.Value, DateTimeKind.Unspecified), _estZone);
                        var dropoffUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.DropoffDateTime.Value, DateTimeKind.Unspecified), _estZone);

                        string key = $"{pickupUtc:O}_{dropoffUtc:O}_{dto.PassengerCount}";
                        if (!seenKeys.Add(key))
                        {
                            duplicates.Add(new TripCsvDTO
                            {
                                PickupDateTime = pickupUtc,
                                DropoffDateTime = dropoffUtc,
                                PassengerCount = dto.PassengerCount,
                                TripDistance = dto.TripDistance,
                                StoreAndFwdFlag = dto.StoreAndFwdFlag,
                                PuLocationId = dto.PuLocationId,
                                DoLocationId = dto.DoLocationId,
                                FareAmount = dto.FareAmount,
                                TipAmount = dto.TipAmount
                            });
                            continue;
                        }

                        tripsToInsert.Add(new Trip
                        {
                            PickupDateTime = pickupUtc,
                            DropoffDateTime = dropoffUtc,
                            PassengerCount = dto.PassengerCount ?? 0,
                            TripDistance = dto.TripDistance ?? 0m,
                            StoreAndFwdFlag = sFlag == "Y" ? "Yes" : "No",
                            PuLocationId = dto.PuLocationId,
                            DoLocationId = dto.DoLocationId,
                            FareAmount = dto.FareAmount ?? 0m,
                            TipAmount = dto.TipAmount ?? 0m
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Skipped: exception processing row: {ex.Message}");
                        continue;
                    }
                }

                if (tripsToInsert.Count > 0)
                {
                    try
                    {
                        BulkInsertTrips(tripsToInsert);
                        totalInserted += tripsToInsert.Count;
                    }
                    catch (Exception)
                    {
                        foreach (var t in tripsToInsert)
                        {
                            try
                            {
                                _db.Trips.Add(t);
                                _db.SaveChanges();
                                totalInserted++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Failed to insert row (Pickup={t.PickupDateTime:o}): {ex.Message}");
                                _db.Entry(t).State = EntityState.Detached;
                            }
                        }
                    }
                }
            }

            if (duplicates.Count > 0)
            {
                var duplicatesFile = Path.Combine(AppContext.BaseDirectory, "duplicates.csv");
                try
                {
                    using var writer = new StreamWriter(duplicatesFile, false, Encoding.UTF8);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    csv.WriteRecords(duplicates);
                    Console.WriteLine($"Wrote {duplicates.Count} duplicates to: {duplicatesFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write duplicates: {ex.Message}");
                    errors.Add($"Failed to write duplicates file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No duplicates detected.");
            }

            if (errors.Count > 0)
            {
                Console.WriteLine("Errors occurred:");
                foreach (var err in errors)
                {
                    Console.WriteLine($"  - {err}");
                }                    
            }
        }

        private void BulkInsertTrips(IList<Trip> trips)
        {
            var table = CreateTripsDataTable(trips);

            var et = _db.Model.FindEntityType(typeof(Trip));
            var tableName = et.GetTableName();
            var schema = et.GetSchema() ?? "dbo";
            var destination = $"[{schema}].[{tableName}]";

            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
                opened = true;
            }

            try
            {
                using var bulk = new SqlBulkCopy((SqlConnection)conn, SqlBulkCopyOptions.Default, null)
                {
                    DestinationTableName = destination,
                    BatchSize = Math.Max(1, _opts.BatchSize)
                };

                bulk.ColumnMappings.Add("PickupDateTime", "tpep_pickup_datetime");
                bulk.ColumnMappings.Add("DropoffDateTime", "tpep_dropoff_datetime");
                bulk.ColumnMappings.Add("PassengerCount", "passenger_count");
                bulk.ColumnMappings.Add("TripDistance", "trip_distance");
                bulk.ColumnMappings.Add("StoreAndFwdFlag", "store_and_fwd_flag");
                bulk.ColumnMappings.Add("PuLocationId", "PULocationID");
                bulk.ColumnMappings.Add("DoLocationId", "DOLocationID");
                bulk.ColumnMappings.Add("FareAmount", "fare_amount");
                bulk.ColumnMappings.Add("TipAmount", "tip_amount");

                bulk.WriteToServer(table);
            }
            finally
            {
                if (opened) conn.Close();
            }
        }

        private static DataTable CreateTripsDataTable(IList<Trip> trips)
        {
            var table = new DataTable();
            table.Columns.Add("PickupDateTime", typeof(DateTime));
            table.Columns.Add("DropoffDateTime", typeof(DateTime));
            table.Columns.Add("PassengerCount", typeof(short));
            table.Columns.Add("TripDistance", typeof(decimal));
            table.Columns.Add("StoreAndFwdFlag", typeof(string));
            table.Columns.Add("PuLocationId", typeof(int));
            table.Columns.Add("DoLocationId", typeof(int));
            table.Columns.Add("FareAmount", typeof(decimal));
            table.Columns.Add("TipAmount", typeof(decimal));

            foreach (var t in trips)
            {
                var row = table.NewRow();
                row["PickupDateTime"] = t.PickupDateTime;
                row["DropoffDateTime"] = t.DropoffDateTime;
                row["PassengerCount"] = t.PassengerCount;
                row["TripDistance"] = t.TripDistance;
                row["StoreAndFwdFlag"] = t.StoreAndFwdFlag;
                row["PuLocationId"] = t.PuLocationId.HasValue ? (object)t.PuLocationId.Value : DBNull.Value;
                row["DoLocationId"] = t.DoLocationId.HasValue ? (object)t.DoLocationId.Value : DBNull.Value;
                row["FareAmount"] = t.FareAmount;
                row["TipAmount"] = t.TipAmount;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
