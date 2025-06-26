using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Plexity
{
    internal class HttpClientLoggingHandler : DelegatingHandler
    {
        public HttpClientLoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log request
            App.Logger.WriteLine(LogLevel.Info, "HttpClientLoggingHandler::Request", $"{request.Method} {request.RequestUri}");

            // Send request
            var response = await base.SendAsync(request, cancellationToken);

            // Log response
            var uri = response.RequestMessage?.RequestUri?.ToString() ?? "<null>";
            App.Logger.WriteLine(LogLevel.Info, "HttpClientLoggingHandler::Response", $"{(int)response.StatusCode} {response.ReasonPhrase} {uri}");

            return response;
        }
    }
}
