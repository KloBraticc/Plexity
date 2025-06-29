using Plexity.Models.Attributes;
using System;
using System.Linq;

namespace Plexity.Enums.FlagPresets
{
    public static class EnumExtensions
    {
        public static string GetStaticName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttributes(typeof(EnumNameAttribute), false)
                              .FirstOrDefault() as EnumNameAttribute;
            return attr?.StaticName ?? value.ToString();
        }
    }

    public enum MSAAMode
    {
        [EnumName(StaticName = "Auto")]
        Default,
        [EnumName(StaticName = "1x")]
        x1,
        [EnumName(StaticName = "2x")]
        x2,
        [EnumName(StaticName = "4x")]
        x4,
        [EnumName(StaticName = "8x")]
        x8
    }
}
