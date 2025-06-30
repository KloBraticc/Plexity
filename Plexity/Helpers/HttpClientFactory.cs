using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plexity.Helpers
{
    public static class HttpClientFactory
    {
        private static readonly HttpClient _sharedClient;
        
        static HttpClientFactory()
        {
            _sharedClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
            
            // Configure default headers
            _sharedClient.DefaultRequestHeaders.Add("User-Agent", $"Plexity/{App.Version}");
        }
        
        public static HttpClient GetClient() => _sharedClient;
        
        public static HttpClient CreateClient()
        {
            // Use only when specific settings are needed that differ from the shared client
            return new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
        }
    }
}