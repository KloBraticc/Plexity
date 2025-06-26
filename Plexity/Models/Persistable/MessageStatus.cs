using System.Collections.ObjectModel;
using System.IO;
using Plexity.Enums;

namespace Plexity.Models.Persistable
{
    /// <summary>
    /// Represents configuration settings for Plexity.
    /// </summary>
    public class MessageStatus
    {
        public string Message { get; set; } = "Please Wait"; // for the dum a**s out there asking why tf its not in appsettings its because it will save in app settings and thats bad it will save the last applyed message BAD BOYY..


    }
}
