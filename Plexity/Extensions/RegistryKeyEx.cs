using Microsoft.Win32;
using Plexity.Enums;

namespace Plexity.Extensions
{
    public static class RegistryKeyEx
    {
        public static void SetValueSafe(this RegistryKey registryKey, string? name, object value)
        {
            try
            {
                App.Logger.WriteLine(LogLevel.Info, "RegistryKeyEx::SetValueSafe", $"Writing '{value}' to {registryKey}\\{name}");
                registryKey.SetValue(name, value);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }

        public static void DeleteValueSafe(this RegistryKey registryKey, string name)
        {
            try
            {
                App.Logger.WriteLine(LogLevel.Info, "RegistryKeyEx::DeleteValueSafe", $"Deleting {registryKey}\\{name}");
                registryKey.DeleteValue(name);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
