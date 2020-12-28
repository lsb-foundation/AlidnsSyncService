using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace AlidnsSyncService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IJobFactory _jobFactory;

        private IScheduler _scheduler;
        private int intervalSeconds;

        //public static string AccessKeyId { get; private set; }
        //public static string AccessKeySecret { get; private set; }
        //public static string DnsDomain { get; private set; }

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IJobFactory jobFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _jobFactory = jobFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //AccessKeyId = _configuration.GetValue<string>("Alidns:AccessKeyId");
            //AccessKeySecret = _configuration.GetValue<string>("Alidns:AccessKeySecret");
            //DnsDomain = _configuration.GetValue<string>("Alidns:DnsDomain");
            intervalSeconds = _configuration.GetValue<int>("BackgroundTask:IntervalSeconds");

            _scheduler = await new StdSchedulerFactory().GetScheduler(cancellationToken);
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start(cancellationToken);

            IJobDetail job = JobBuilder.Create<AlidnsSyncJob>().Build();
            ITrigger trigger = TriggerBuilder.Create()
                .StartNow()
                .WithSimpleSchedule(x => 
                    x.WithIntervalInSeconds(intervalSeconds)
                     .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(job, trigger, cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.Shutdown(cancellationToken);
            await base.StopAsync(cancellationToken);
        }

        //public static void Log
    }
}
