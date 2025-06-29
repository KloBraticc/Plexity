using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Plexity.Helpers
{
    public static class SystemAccentColorHelper
    {
        private const string PersonalizeRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string DWMRegistryPath = @"SOFTWARE\Microsoft\Windows\DWM";

        public static bool IsSystemUsingDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking system theme: {ex.Message}");
                return false;
            }
        }

        public static Color GetSystemAccentColor()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DWMRegistryPath, false))
                {
                    if (key != null)
                    {
                        object colorValue = key.GetValue("AccentColor");
                        if (colorValue != null)
                        {
                            int abgrValue = Convert.ToInt32(colorValue);
                            byte a = (byte)((abgrValue >> 24) & 0xFF);
                            byte b = (byte)((abgrValue >> 16) & 0xFF);
                            byte g = (byte)((abgrValue >> 8) & 0xFF);
                            byte r = (byte)(abgrValue & 0xFF);
                            
                            return Color.FromArgb(a, r, g, b);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get system accent color: {ex.Message}");
            }
            
            // Return a default color if unable to get the system accent
            return Colors.DodgerBlue;
        }

        public static void ApplySystemAccentColor(Application app, bool useDarkMode)
        {
            var accentColor = GetSystemAccentColor();
            
            // Create darker and lighter versions for hover states, etc.
            var darkerAccent = DarkenColor(accentColor, 0.2);
            var lighterAccent = LightenColor(accentColor, 0.2);
            
            // Update application resources
            app.Resources["SystemAccentColor"] = accentColor;
            app.Resources["SystemAccentColorDark"] = darkerAccent;
            app.Resources["SystemAccentColorLight"] = lighterAccent;
            
            // Update text colors based on theme
            app.Resources["SystemTextColor"] = useDarkMode ? Colors.White : Colors.Black;
            app.Resources["SystemBackgroundColor"] = useDarkMode ? Color.FromRgb(32, 32, 32) : Colors.White;
        }

        public static void ApplySystemAccentColor(Application app)
        {
            var accentColor = GetSystemAccentColor();
            
            // Create a SolidColorBrush from the accent color
            var accentBrush = new SolidColorBrush(accentColor);
            
            // Set it in the application resources
            app.Resources["SystemAccentBrush"] = accentBrush;
            app.Resources["SystemAccentColor"] = accentColor;
        }

        public static bool IsSystemAccentColorEnabled()
        {
            // Check if the current system supports accent colors
            return Environment.OSVersion.Version.Major >= 10;
        }

        private static Color DarkenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R * (1 - factor)),
                (byte)Math.Max(0, color.G * (1 - factor)),
                (byte)Math.Max(0, color.B * (1 - factor))
            );
        }

        private static Color LightenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }
    }
}