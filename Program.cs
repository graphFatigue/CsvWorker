using CsvWorker.BLL.Options;
using CsvWorker.BLL.Services;
using CsvWorker.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables()
           .AddCommandLine(args);
    })
    .ConfigureServices((ctx, services) =>
    {
        var configuration = ctx.Configuration;
        services.Configure<CsvReaderOptions>(configuration.GetSection("Importer"));
        var conn = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));
        services.AddTransient<CsvReaderService>();
        services.AddTransient<CsvImporterService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.Migrate();
}

using (var scope = host.Services.CreateScope())
{
    var importer = scope.ServiceProvider.GetRequiredService<CsvImporterService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var csvFilePath = configuration.GetValue<string>("CsvFilePath") ?? throw new InvalidOperationException("CsvFilePath is not set.");

    var allowedDir = Path.Combine(AppContext.BaseDirectory);
    Directory.CreateDirectory(allowedDir);
    csvFilePath = EnsureSafeCsvPath(csvFilePath, allowedDir);

    try
    {
        importer.Import(csvFilePath, out var totalInserted);
        Console.WriteLine($"Inserted {totalInserted} trips.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Import failed: {ex.Message}");
    }
}

static string EnsureSafeCsvPath(string filePath, string allowedBaseDirectory)
{
    var full = Path.GetFullPath(filePath);
    var baseDir = Path.GetFullPath(allowedBaseDirectory);
    if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        throw new UnauthorizedAccessException("CSV path not allowed.");
    return full;
}