using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Collections.Generic;

namespace AlidnsSyncService
{
    /// <summary>
    /// Server酱推送
    /// </summary>
    public class ServerChan : IPush
    {
        private readonly string pushUrl = "https://sctapi.ftqq.com/";

        public ServerChan(IConfiguration configuration)
        {
            var scKey = configuration.GetValue<string>("ServerChanSCKey");
            if (!string.IsNullOrWhiteSpace(scKey))
            {
                pushUrl += scKey +".send";
                CanPush = true;
            }
        }

        public bool CanPush { get; } = false;

        public async void Push(string title, string content)
        {
            if (!CanPush) return;
            using var client = new HttpClient();
            var json = new
            {
                text = title,
                desp = content
            };
            var kvPairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("title", title),
                new KeyValuePair<string, string>("desp", content)
            };
            var httpContent = new FormUrlEncodedContent(kvPairs);
            await client.PostAsync(pushUrl, httpContent);
        }
    }
}
