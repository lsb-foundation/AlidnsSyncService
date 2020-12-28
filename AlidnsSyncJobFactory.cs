using Quartz;
using Quartz.Spi;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace AlidnsSyncService
{
    public class AlidnsSyncJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AlidnsSyncJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetService<AlidnsSyncJob>();
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
