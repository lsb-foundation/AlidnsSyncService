using Quartz;
using Quartz.Spi;
using System;

namespace AlidnsSyncService
{
    public class JobFactoryImplement : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFactoryImplement(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            (job as IDisposable)?.Dispose();
        }
    }
}
