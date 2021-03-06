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
                .AddJsonFile("appsettings.json")
                .Build();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(_configuration)
                            .AddHostedService<Worker>()
                            .AddTransient<AlidnsSyncJob>()
                            .AddTransient<CleanLogJob>()
                            .AddTransient<IJobFactory, JobFactoryImplement>()
                            .AddTransient<LoggerConfiguration>()
                            .AddTransient<IPush, ServerChan>()
                            .AddTransient<IPush, BarkPush>();

                    var serviceProvider = services.BuildServiceProvider();
                    var logPath = Path.Combine(serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath, "logs/log.txt");
                    Log.Logger = serviceProvider.GetRequiredService<LoggerConfiguration>()
                                .Enrich.FromLogContext()
                                .WriteTo.Console()
                                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                                .CreateLogger();
                })
                .UseWindowsService()
                .UseSystemd()
                .UseSerilog();
    }
}
