using CsvHelper;
using CsvWorker.BLL.DTOs;
using CsvWorker.BLL.Options;
using CsvWorker.DAL;
using CsvWorker.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvWorker.BLL.Services
{
    public class CsvImporterService
    {
        private readonly AppDbContext _db;
        private readonly CsvReaderService _reader;
        private readonly CsvImporterOptions _opts;

        public CsvImporterService(AppDbContext db, CsvReaderService reader, CsvImporterOptions? options = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _opts = options ?? new CsvImporterOptions();
            if (_opts.BatchSize <= 0) throw new ArgumentException("BatchSize must be positive.", nameof(options));
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

            foreach (var batch in dtos.Chunk(_opts.BatchSize))
            {
                var batchDtos = batch.ToList();
                var tripsToInsert = new List<Trip>();

                foreach (var dto in batchDtos)
                {
                    if (!dto.PickupDateTime.HasValue || !dto.DropoffDateTime.HasValue)
                    {
                        throw new InvalidDataException($"CSV contains a row with missing pickup or dropoff datetime. Pickup='{dto.PickupDateTime}', Dropoff='{dto.DropoffDateTime}'");
                    }
                    if (dto.StoreAndFwdFlag != "Y" && dto.StoreAndFwdFlag != "N")
                    {
                        throw new InvalidDataException("Invalid StoreAndFwdFlag value");
                    }

                    string key = $"{dto.PickupDateTime.Value:O}_{dto.DropoffDateTime.Value:O}_{dto.PassengerCount}";
                    if (!seenKeys.Add(key))
                    {
                        duplicates.Add(dto);
                        continue;
                    }

                    tripsToInsert.Add(new Trip
                    {
                        PickupDateTime = dto.PickupDateTime.Value,
                        DropoffDateTime = dto.DropoffDateTime.Value,
                        PassengerCount = dto.PassengerCount,
                        TripDistance = dto.TripDistance,
                        StoreAndFwdFlag = dto.StoreAndFwdFlag == "Y" ? "Yes" : "No",
                        PuLocationId = dto.PuLocationId,
                        DoLocationId = dto.DoLocationId,
                        FareAmount = dto.FareAmount,
                        TipAmount = dto.TipAmount
                    });
                }

                if (tripsToInsert.Count > 0)
                {
                    _db.Trips.AddRange(tripsToInsert);
                    _db.SaveChanges();
                    totalInserted += tripsToInsert.Count;
                }
            }

            if (duplicates.Count > 0)
            {
                var duplicatesFile = Path.Combine(Path.GetDirectoryName(csvFilePath) ?? ".", "duplicates.csv");
                using (var writer = new StreamWriter(duplicatesFile, false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(duplicates);
                }
            }
        }
    }
}
