using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace AlidnsSyncService
{
    public class BarkPush : IPush
    {
        private readonly string _barkKey;
        private const string barkUrlFormat = "https://api.day.app/{0}/{1}/{2}?isArchive=1";

        public BarkPush(IConfiguration configuration)
        {
            _barkKey = configuration.GetValue<string>("BARK_KEY");
            CanPush = !string.IsNullOrWhiteSpace(_barkKey);
        }

        public bool CanPush { get; }

        public async void Push(string title, string message)
        {
            if (!CanPush) return;
            var barkUrl = string.Format(barkUrlFormat, _barkKey, title, message);
            await new HttpClient().GetAsync(barkUrl);
        }
    }
}
