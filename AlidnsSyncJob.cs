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
using Newtonsoft.Json;

namespace AlidnsSyncService
{
    public class AlidnsSyncJob : IJob
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly IConfiguration _configuration;
        private readonly ILogger<AlidnsSyncJob> _logger;

        private readonly string _dnsDomain;
        private readonly string _domainName;
        private readonly string _rr;
        private readonly string _accessKeyId;
        private readonly string _accessKeySecret;

        public AlidnsSyncJob(IConfiguration configuration, ILogger<AlidnsSyncJob> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _accessKeyId = _configuration.GetValue<string>("Alidns:AccessKeyId");
            _accessKeySecret = _configuration.GetValue<string>("Alidns:AccessKeySecret");
            _dnsDomain = _configuration.GetValue<string>("Alidns:DnsDomain");
            _domainName = GetDomainName();
            _rr = GetRR();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _semaphore.WaitAsync();
            try
            {
                var myIp = await GetMyIpAddress();
                var domainRecords = GetDnsRecordsByDomainName(_domainName);
                var domainToUpdate = domainRecords.FirstOrDefault(r => r.RR == _rr);
                if (domainToUpdate.Equals(default(DomainRecord)))
                {
                    var domainRecord = new DomainRecord { RR = _rr, DomainName = _domainName, Value = myIp, TTL = 600 };
                    AddDnsDomainRecord(domainRecord);
                    _logger.LogInformation($"Add: {domainRecord}");
                }
                else if (domainToUpdate.Value != myIp)
                {
                    domainToUpdate.DomainName = _domainName;
                    domainToUpdate.Value = myIp;
                    UpdateDnsDomainRecord(domainToUpdate);
                    _logger.LogInformation($"Update: {domainToUpdate}");
                }
                else
                {
                    _logger.LogInformation($"Skipped.");
                }
            }
            catch(Exception e)
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

        private IEnumerable<DomainRecord> GetDnsRecordsByDomainName(string domainName)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", _accessKeyId, _accessKeySecret);
            DefaultAcsClient client = new DefaultAcsClient(profile);
            var request = new DescribeDomainRecordsRequest { DomainName = domainName };
            var response = client.GetAcsResponse(request);
            return response.DomainRecords.Select(
                r => new DomainRecord
                {
                    RecordId = r.RecordId,
                    RR = r.RR,
                    Value = r.Value,
                    TTL = r.TTL
                });
        }

        private void AddDnsDomainRecord(DomainRecord record)
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

        private void UpdateDnsDomainRecord(DomainRecord record)
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

        private string GetDomainName()
        {
            int index = _dnsDomain.LastIndexOf('.');
            if (index == -1) return null;
            index = _dnsDomain[..index].LastIndexOf('.');
            if (index == -1) return _dnsDomain;
            return _dnsDomain[(index + 1)..];
        }

        private string GetRR()
        {
            int index = _dnsDomain.LastIndexOf('.');
            if (index == -1) return null;
            index = _dnsDomain[..index].LastIndexOf('.');
            if (index == -1) return _dnsDomain;
            return _dnsDomain[..index];
        }
    }

    public struct DomainRecord
    {
        public string RecordId { get; set; }
        public string RR { get; set; }
        public string DomainName { get; set; }
        public string Value { get; set; }
        public long? TTL { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
