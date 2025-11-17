
using CsvWorker.BLL.Options;
using CsvWorker.BLL.Services;
using CsvWorker.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(conn));

        services.AddTransient<CsvReaderService>();
        services.AddTransient<CsvImporterService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
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

    var csvFilePath = host.Services.GetRequiredService<IConfiguration>()
        .GetValue<string>("CsvFilePath");

    int totalInserted = 0;
    try
    {
        importer.Import(csvFilePath, out totalInserted);
    }
    catch(Exception e)
    {
        Console.WriteLine(e.Message);        
    }
    finally
    {
        Console.WriteLine($"Inserted {totalInserted} trips.");
    }

}
