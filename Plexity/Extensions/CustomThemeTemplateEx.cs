using Plexity.Enums;

namespace Plexity.Extensions
{
    static class CustomThemeTemplateEx
    {
        public static string GetFileName(this CustomThemeTemplate template)
        {
            return $"CustomBootstrapperTemplate_{template}.xml";
        }
    }
}