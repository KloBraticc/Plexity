using Plexity.Models.Attributes;

namespace Plexity.Enums.FlagPresets
{
    public enum RenderingMode
    {
        [EnumName(FromTranslation = "Common.Automatic")]
        Default,
        Vulkan,
        [EnumName(StaticName = "Metal")]
        Metal,
        OpenGL,
        D3D11,
        D3D10,
    }
}
