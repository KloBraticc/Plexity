using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plexity.Exceptions
{
    internal class InvalidHTTPResponseException : Exception
    {
        public InvalidHTTPResponseException(string message) : base(message) { }
    }
}
