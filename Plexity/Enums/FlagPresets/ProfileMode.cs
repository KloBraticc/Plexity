using Plexity.Models.Attributes;

namespace Plexity.Enums.FlagPresets
{
    public enum ProfileMode
    {
        [EnumName(FromTranslation = "Common.Automatic")]
        Default,
        [EnumName(StaticName = "Plexitys Official")]
        Plexity,
        [EnumName(StaticName = "Stoofs")]
        Stoof
    }
}
