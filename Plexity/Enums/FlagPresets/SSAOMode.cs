using Plexity.Models.Attributes;
using System;
using System.Linq;

namespace Plexity.Enums.FlagPresets
{
    public static class EnumExtensions2
    {
        public static string GetStaticName2(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttributes(typeof(EnumNameAttribute), false)
                              .FirstOrDefault() as EnumNameAttribute;
            return attr?.StaticName ?? value.ToString();
        }
    }

    public enum SSAOMode
    {
        [EnumName(StaticName = "Disabled")]
        Default,
        [EnumName(StaticName = "1x")]
        x1,
        [EnumName(StaticName = "2x")]
        x2,
        [EnumName(StaticName = "4x")]
        x4,
        [EnumName(StaticName = "5x")]
        x5,
        [EnumName(StaticName = "6x")]
        x6,
        [EnumName(StaticName = "7x")]
        x7,
        [EnumName(StaticName = "8x")]
        x8
    }
}
