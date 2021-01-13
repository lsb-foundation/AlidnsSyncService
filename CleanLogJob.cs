using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AlidnsSyncService
{
    public class CleanLogJob : IJob
    {
        private readonly string _logPath;
        private readonly ILogger<CleanLogJob> _logger;

        public CleanLogJob(IServiceProvider serviceProvider, ILogger<CleanLogJob> logger)
        {
            _logger = logger;
            _logPath = Path.Combine(serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath, "logs");
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (!Directory.Exists(_logPath)) Directory.CreateDirectory(_logPath);
                var logFiles = new List<string>();
                foreach (var fileName in Directory.GetFiles(_logPath))
                {
                    if (Regex.IsMatch(Path.GetFileName(fileName), @"^log\d{8}.txt$"))
                        logFiles.Add(fileName);
                }
                logFiles = logFiles.OrderBy(file => new FileInfo(file).CreationTime).ToList();
                if (logFiles.Count > 30)
                {
                    int count = logFiles.Count - 30;
                    foreach (var file in logFiles.Take(count))
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Deleted log file {Path.GetFileName(file)}.");
                    }
                }
                return Task.CompletedTask;
            }
            catch(Exception e)
            {
                _logger.LogTrace(e, e.Message);
                return Task.CompletedTask;
            }
        }
    }
}
