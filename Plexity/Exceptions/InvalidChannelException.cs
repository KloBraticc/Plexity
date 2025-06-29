using System.Net;

namespace Plexity.Exceptions
{
    public class InvalidChannelException : Exception
    {
        public HttpStatusCode? StatusCode;

        public InvalidChannelException(HttpStatusCode? statusCode) : base()
            => StatusCode = statusCode;
    }
}
