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

namespace AlidnsSyncService
{
    public class AlidnsSyncJob : IJob
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public async Task Execute(IJobExecutionContext context)
        {
            await _semaphore.WaitAsync();
            var domainName = GetDomainName();
            var rr = GetRR();
            try
            {
                var myIp = await GetMyIpAddress();
                var domainRecords = GetDnsRecordsByDomainName(domainName);
                var domainToUpdate = domainRecords.FirstOrDefault(r => r.RR == rr);
                if (domainToUpdate.Equals(default(DomainRecord)))
                {
                    AddDnsDomainRecord(domainName, new DomainRecord { RR = rr, Value = myIp, TTL = 600 });
                }
                else if (domainToUpdate.Value != myIp)
                {
                    domainToUpdate.Value = myIp;
                    UpdateDnsDomainRecord(domainToUpdate);
                }
            }
            catch(Exception e)
            {

            }
            _semaphore.Release();
        }

        private static async Task<string> GetMyIpAddress()
        {
            HttpClient clipClient = new HttpClient();
            HttpResponseMessage ipResponse = await clipClient.GetAsync("http://whatismyip.akamai.com");
            return await ipResponse.Content.ReadAsStringAsync();
        }

        private static IEnumerable<DomainRecord> GetDnsRecordsByDomainName(string domainName)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", Worker.AccessKeyId, Worker.AccessKeySecret);
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

        private static void AddDnsDomainRecord(string domainName, DomainRecord record)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", Worker.AccessKeyId, Worker.AccessKeySecret);
            DefaultAcsClient client = new DefaultAcsClient(profile);
            var request = new AddDomainRecordRequest
            {
                DomainName = domainName,
                RR = record.RR,
                Value = record.Value,
                TTL = record.TTL,
                Type = "A"
            };
            client.GetAcsResponse(request);
        }

        private static void UpdateDnsDomainRecord(DomainRecord record)
        {
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", Worker.AccessKeyId, Worker.AccessKeySecret);
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

        private static string GetDomainName()
        {
            int index = Worker.DnsDomain.LastIndexOf('.');
            if (index == -1) return null;
            index = Worker.DnsDomain[..index].LastIndexOf('.');
            if (index == -1) return Worker.DnsDomain;
            return Worker.DnsDomain[(index + 1)..];
        }

        private static string GetRR()
        {
            int index = Worker.DnsDomain.LastIndexOf('.');
            if (index == -1) return null;
            index = Worker.DnsDomain[..index].LastIndexOf('.');
            if (index == -1) return Worker.DnsDomain;
            return Worker.DnsDomain[..index];
        }
    }

    public struct DomainRecord
    {
        public string RecordId { get; set; }
        public string RR { get; set; }
        public string Value { get; set; }
        public long? TTL { get; set; }
    }
}
