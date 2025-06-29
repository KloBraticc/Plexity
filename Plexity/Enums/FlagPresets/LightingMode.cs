using Plexity.Models.Attributes;

namespace Plexity.Enums.FlagPresets
{
    public enum LightingMode
    {
        [EnumName(StaticName = "Chosen by game")]
        Default,
        [EnumName(StaticName = "Voxel (Phase 1)")]
        Voxel,
        [EnumName(StaticName = "ShadowMap (Phase 2)")]
        ShadowMap,
        [EnumName(StaticName = "Future (Phase 3)")]
        Future,
        [EnumName(StaticName = "Unified (Phase 4)")]
        Unified,
    }

}

