using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AlidnsSyncService
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IJobFactory _jobFactory;
        private IScheduler scheduler;

        public Worker(IConfiguration configuration, IJobFactory jobFactory)
        {
            _configuration = configuration;
            _jobFactory = jobFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            int intervalSeconds = _configuration.GetValue<int>("Alidns:IntervalSeconds");
            string[] cleanTimeStrings = _configuration.GetValue<string>("DailyCleanTime").Split(':');
            
            int cleanTimeHour = int.Parse(cleanTimeStrings[0]);
            int cleanTimeMinute = int.Parse(cleanTimeStrings[1]);
            int cleanTimeSecond = int.Parse(cleanTimeStrings[2]);

            scheduler = await new StdSchedulerFactory().GetScheduler(cancellationToken);
            scheduler.JobFactory = _jobFactory;
            await scheduler.Start(cancellationToken);

            IJobDetail alidnsJob = JobBuilder.Create<AlidnsSyncJob>().Build();
            ITrigger alidnsTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithSimpleSchedule(x => 
                    x.WithIntervalInSeconds(intervalSeconds)
                     .RepeatForever())
                .Build();

            IJobDetail cleanLogJob = JobBuilder.Create<CleanLogJob>().Build();
            ITrigger cleanLogTrigger = TriggerBuilder.Create()
                .WithDailyTimeIntervalSchedule(x =>
                    x.OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(cleanTimeHour, cleanTimeMinute, cleanTimeSecond))
                    .EndingDailyAfterCount(1))
                .Build();

            var triggerJobsDict = new Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>>
            {
                { alidnsJob, new List<ITrigger>{ alidnsTrigger }.AsReadOnly() },
                { cleanLogJob, new List<ITrigger>{ cleanLogTrigger }.AsReadOnly() }
            };
            var triggerAndJobs = new ReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>>(triggerJobsDict);
            await scheduler.ScheduleJobs(triggerAndJobs, true, cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await scheduler.Shutdown(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
