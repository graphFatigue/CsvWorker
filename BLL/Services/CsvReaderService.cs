using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvWorker.BLL.Options;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvWorker.BLL.Services
{
    public class CsvReaderService
    {
        private readonly CsvReaderOptions _opts;

        public CsvReaderService(IOptions<CsvReaderOptions> opts)
        {
            _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        }

        public List<T> GetRecords<T>(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found.", filePath);

            if (!filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("File must have a .csv extension.");

            var fi = new FileInfo(filePath);
            if (fi.Length == 0)
                throw new InvalidDataException("CSV file is empty.");

            Encoding encoding = Encoding.GetEncoding(1251);
            using (var headerReader = new StreamReader(filePath, encoding))
            {
                var firstLine = headerReader.ReadLine();
                if (string.IsNullOrWhiteSpace(firstLine))
                    throw new InvalidDataException("CSV file does not contain a header or is empty.");

                if (!firstLine.Contains(","))
                    throw new InvalidDataException("CSV file does not appear to use the expected ',' delimiter.");
            }

            var culture = CultureInfo.GetCultureInfo(_opts.Culture);
            var csvConfiguration = new CsvConfiguration(culture)
            {
                Encoding = encoding,
                Delimiter = _opts.Delimiter,
                HasHeaderRecord = true,
                PrepareHeaderForMatch = args => args.Header.Trim()
            };

            try
            {
                using (var reader = new StreamReader(filePath, encoding))
                using (var csv = new CsvReader(reader, csvConfiguration))
                {
                    var formats = _opts.DateTimeFormats ?? Array.Empty<string>();

                    var options = new TypeConverterOptions { Formats = formats };
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);

                    var records = csv.GetRecords<T>().ToList();
                    return records;
                }
            }
            catch (HeaderValidationException hvex)
            {
                throw new InvalidDataException("CSV header validation failed.", hvex);
            }
            catch (CsvHelper.TypeConversion.TypeConverterException tcEx)
            {
                throw new InvalidDataException("CSV type conversion failed.", tcEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read or parse the CSV file.", ex);
            }
        }
    }
}
