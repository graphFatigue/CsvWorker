namespace CsvWorker.BLL.Options
{
    public class CsvReaderOptions
    {
        public string Delimiter { get; set; } = ",";
        public string[] DateTimeFormats { get; set; } = new[] { "MM/dd/yyyy hh:mm:ss tt" };
        public string Culture { get; set; } = "en-US";
        public int EncodingCodePage { get; set; } = 1251;
    }
}
