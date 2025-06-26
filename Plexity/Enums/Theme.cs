using Plexity.Models.Attributes;

namespace Plexity.Enums
{
    public enum Theme
    {
        [EnumName(FromTranslation = "Common.SystemDefault")]
        Default,
        Dark,
        Light,
        Plexity,
        Blue,
        Cyan,
        Green,
        Orange,
        Pink,
        Red,
        Yellow,
    }
}