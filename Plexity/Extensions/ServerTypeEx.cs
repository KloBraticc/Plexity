using Microsoft.VisualBasic;
using Plexity.Enums;

namespace Plexity.Extensions
{
    static class ServerTypeEx
    {
        public static string ToTranslatedString(this ServerType value) => value switch
        {
            ServerType.Public => "Public",
            ServerType.Private => "Private",
            ServerType.Reserved => "Reserved",
            _ => "No Server Type Detected?"
        };
    }
}
