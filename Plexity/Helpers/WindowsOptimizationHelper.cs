using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Plexity.Helpers
{
    public static class WindowsOptimizationHelper
    {
        private static ILogger? _logger;

        static WindowsOptimizationHelper()
        {
            _logger = App.Services?.GetService(typeof(ILogger<App>)) as ILogger;
        }

        // Windows 11 specific APIs
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int value);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;

        public static async Task OptimizeForCurrentWindowsAsync(Window window)
        {
            await Task.Run(() =>
            {
                try
                {
                    var windowsVersion = GetWindowsVersion();
                    _logger?.LogInformation("Optimizing for Windows version: {Version}", windowsVersion);

                    // Apply version-specific optimizations
                    if (windowsVersion.Major >= 10)
                    {
                        OptimizeForWindows10Plus(window);
                        
                        if (windowsVersion.Build >= 22000) // Windows 11
                        {
                            OptimizeForWindows11(window);
                        }
                    }

                    // Universal optimizations
                    ApplyUniversalOptimizations(window);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to apply Windows optimizations");
                }
            });
        }

        private static void OptimizeForWindows10Plus(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Enable dark mode if system is using dark theme
                if (SystemAccentColorHelper.IsSystemUsingDarkMode())
                {
                    int value = 1;
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                }

                _logger?.LogDebug("Applied Windows 10+ optimizations");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply Windows 10+ optimizations");
            }
        }

        private static void OptimizeForWindows11(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Set rounded corners for Windows 11
                int cornerPreference = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                // Set accent border color
                var accentColor = SystemAccentColorHelper.GetSystemAccentColor();
                int borderColor = (accentColor.R << 16) | (accentColor.G << 8) | accentColor.B;
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

                _logger?.LogDebug("Applied Windows 11 optimizations");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply Windows 11 optimizations");
            }
        }

        private static void ApplyUniversalOptimizations(Window window)
        {
            try
            {
                // Enable per-monitor DPI awareness
                try
                {
                    SetProcessDpiAwareness(2); // PROCESS_PER_MONITOR_DPI_AWARE
                }
                catch
                {
                    SetProcessDPIAware(); // Fallback for older systems
                }

                // Optimize rendering
                window.UseLayoutRounding = true;
                window.SnapsToDevicePixels = true;
                RenderOptions.SetBitmapScalingMode(window, BitmapScalingMode.HighQuality);
                
                _logger?.LogDebug("Applied universal optimizations");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply universal optimizations");
            }
        }

        public static Version GetWindowsVersion()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var major = Convert.ToInt32(key.GetValue("CurrentMajorVersionNumber", 10));
                    var minor = Convert.ToInt32(key.GetValue("CurrentMinorVersionNumber", 0));
                    var build = Convert.ToInt32(key.GetValue("CurrentBuildNumber", 0));
                    
                    return new Version(major, minor, build);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get Windows version");
            }

            return Environment.OSVersion.Version;
        }

        public static bool IsWindows11OrLater()
        {
            var version = GetWindowsVersion();
            return version.Major >= 10 && version.Build >= 22000;
        }

        public static bool IsWindows10OrLater()
        {
            var version = GetWindowsVersion();
            return version.Major >= 10;
        }

        public static async Task OptimizeSystemSettingsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Optimize Windows for better performance
                    using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                    key?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); // Custom performance settings

                    _logger?.LogInformation("Applied system performance optimizations");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to apply system optimizations");
                }
            });
        }
    }
}