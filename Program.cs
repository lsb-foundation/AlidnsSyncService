using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz.Spi;
using System.IO;
using Serilog;

namespace AlidnsSyncService
{
    public class Program
    {
        private static IConfiguration _configuration;
        public static void Main(string[] args)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var logPath = _configuration.GetValue<string>("BackgroundTask:LogPath");

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(_configuration)
                            .AddHostedService<Worker>()
                            .AddTransient<AlidnsSyncJob>()
                            .AddTransient<IJobFactory, AlidnsSyncJobFactory>();
                })
            .UseWindowsService()
            .UseSystemd();
            //.UseSerilog()
    }
}
