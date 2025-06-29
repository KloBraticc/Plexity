using System;
using System.Globalization;
using System.Resources;
using System.Text;
using Microsoft.VisualBasic;

namespace Plexity.Exceptions
{
    internal class CustomThemeException : Exception
    {
        /// <summary>
        /// The exception message in English (for logging)
        /// </summary>
        public string EnglishMessage { get; }
        private static readonly ResourceManager ResourceManager = new ResourceManager("Plexity.Messages", typeof(YourClass).Assembly);

        public CustomThemeException(string translationString)
            : base(GetLocalizedMessage(translationString))
        {
            EnglishMessage = GetLocalizedMessage(translationString, new CultureInfo("en-GB"));
        }

        public CustomThemeException(Exception innerException, string translationString)
            : base(GetLocalizedMessage(translationString), innerException)
        {
            EnglishMessage = GetLocalizedMessage(translationString, new CultureInfo("en-GB"));
        }

        public CustomThemeException(string translationString, params object?[] args)
            : base(FormatSafe(GetLocalizedMessage(translationString), args))
        {
            EnglishMessage = FormatSafe(GetLocalizedMessage(translationString, new CultureInfo("en-GB")), args);
        }

        public CustomThemeException(Exception innerException, string translationString, params object?[] args)
            : base(FormatSafe(GetLocalizedMessage(translationString), args), innerException)
        {
            EnglishMessage = FormatSafe(GetLocalizedMessage(translationString, new CultureInfo("en-GB")), args);
        }


        private static string GetLocalizedMessage(string key, CultureInfo? culture = null)
        {
            var cultureInfo = culture ?? CultureInfo.CurrentCulture;
            string? message = ResourceManager.GetString(key, cultureInfo);

            return message ?? $"[{key}]";
        }

        private static string FormatSafe(string? format, params object?[] args)
        {
            try
            {
                return string.Format(format ?? string.Empty, args);
            }
            catch
            {
                return format ?? string.Empty;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(GetType().ToString());

            if (!string.IsNullOrEmpty(Message))
                sb.Append($": {Message}");

            if (!string.IsNullOrEmpty(EnglishMessage) && Message != EnglishMessage)
                sb.Append($" ({EnglishMessage})");

            if (InnerException != null)
                sb.Append($"\r\n ---> {InnerException}\r\n   ");

            if (StackTrace != null)
                sb.Append($"\r\n{StackTrace}");

            return sb.ToString();
        }
    }
}
