using Quartz;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Alidns.Model.V20150109;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace AlidnsSyncService
{
    public class AlidnsSyncJob : IJob
    {
        private static readonly SemaphoreSlim _semaphore = new(1);

        private readonly ILogger<AlidnsSyncJob> _logger;
        private readonly IServiceProvider _provider;

        private readonly string _domainName;
        private readonly string _rr;
        private readonly string _accessKeyId;
        private readonly string _accessKeySecret;

        public AlidnsSyncJob(IConfiguration configuration, ILogger<AlidnsSyncJob> logger, IServiceProvider provider)
        {
            _logger = logger;
            _accessKeyId = configuration.GetValue<string>("Alidns:AccessKeyId");
            _accessKeySecret = configuration.GetValue<string>("Alidns:AccessKeySecret");
            var dnsDomain = configuration.GetValue<string>("Alidns:DnsDomain");
            _domainName = GetDomainName(dnsDomain);
            _rr = GetRR(dnsDomain);
            _provider = provider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(_accessKeyId) || string.IsNullOrWhiteSpace(_accessKeySecret))
                {
                    throw new Exception("AccessKeyId and AccessKeySecret cant't be empty.");
                }

                var myIp = await GetMyIpAddress();
                _logger.LogInformation($"Current IP Address: {myIp}");
                var domainRecords = GetDnsRecords();
                var domainToUpdate = domainRecords.FirstOrDefault(r => r.RR == _rr);
                if (domainToUpdate.Equals(default(DomainRecord)))
                {
                    var domainRecord = new DomainRecord { RR = _rr, DomainName = _domainName, Value = myIp, TTL = 600 };
                    AddDnsRecord(domainRecord);
                    _logger.LogInformation($"Add: {domainRecord}");
                    PushMessage("阿里云DNS同步服务：添加DNS", domainRecord.ToString());
                }
                else if (domainToUpdate.Value != myIp)
                {
                    domainToUpdate.DomainName = _domainName;
                    domainToUpdate.Value = myIp;
                    UpdateDnsRecord(domainToUpdate);
                    _logger.LogInformation($"Update: {domainToUpdate}");
                    PushMessage("阿里云DNS同步服务：更新DNS", domainToUpdate.ToString());
                }
                else
                {
                    _logger.LogInformation($"Skipped.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            _semaphore.Release();
        }

        private static async Task<string> GetMyIpAddress()
        {
            HttpClient clipClient = new HttpClient();
            HttpResponseMessage ipResponse = await clipClient.GetAsync("http://whatismyip.akamai.com");
            return await ipResponse.Content.ReadAsStringAsync();
        }

        private IEnumerable<DomainRecord> GetDnsRecords()
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", _accessKeyId, _accessKeySecret);
            DefaultAcsClient client = new DefaultAcsClient(profile);
            var request = new DescribeDomainRecordsRequest { DomainName = _domainName };
            var response = client.GetAcsResponse(request);
            return response.DomainRecords.Select(
                r => new DomainRecord
                {
                    RecordId = r.RecordId,
                    DomainName = _domainName,
                    RR = r.RR,
                    Value = r.Value,
                    TTL = r.TTL
                });
        }

        private void AddDnsRecord(DomainRecord record)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", _accessKeyId, _accessKeySecret);
            DefaultAcsClient client = new DefaultAcsClient(profile);
            var request = new AddDomainRecordRequest
            {
                DomainName = record.DomainName,
                RR = record.RR,
                Value = record.Value,
                TTL = record.TTL,
                Type = "A"
            };
            client.GetAcsResponse(request);
        }

        private void UpdateDnsRecord(DomainRecord record)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", _accessKeyId, _accessKeySecret);
            DefaultAcsClient client = new DefaultAcsClient(profile);
            var request = new UpdateDomainRecordRequest
            {
                RecordId = record.RecordId,
                RR = record.RR,
                Value = record.Value,
                TTL = record.TTL,
                Type = "A"
            };
            client.GetAcsResponse(request);
        }

        private static string GetDomainName(string dnsDomain)
        {
            int index = dnsDomain.LastIndexOf('.');
            if (index == -1) throw new ArgumentIsNotDomainException();
            index = dnsDomain[..index].LastIndexOf('.');
            if (index == -1) return dnsDomain;
            return dnsDomain[(index + 1)..];
        }

        private static string GetRR(string dnsDomain)
        {
            int index = dnsDomain.LastIndexOf('.');
            if (index == -1) throw new ArgumentIsNotDomainException();
            index = dnsDomain[..index].LastIndexOf('.');
            if (index == -1) throw new DomainHasNoRRException();
            return dnsDomain[..index];
        }

        private void PushMessage(string title, string message)
        {
            foreach (IPush push in _provider.GetServices<IPush>())
            {
                if (push.CanPush)
                {
                    push.Push(title, message);
                }
            }
        }
    }

    public struct DomainRecord
    {
        public string RecordId { get; set; }
        public string RR { get; set; }
        public string DomainName { get; set; }
        public string Value { get; set; }
        public long? TTL { get; set; }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override string ToString()
        {
            return $"{RR}.{DomainName} : {Value}";
        }
    }

    public sealed class ArgumentIsNotDomainException : Exception { }
    public sealed class DomainHasNoRRException : Exception { }
}
