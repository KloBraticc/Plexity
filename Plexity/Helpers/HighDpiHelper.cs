using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Plexity.Helpers
{
    public static class HighDpiHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDpiAwareness value);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

        private enum ProcessDpiAwareness
        {
            ProcessDpiUnaware = 0,
            ProcessSystemDpiAware = 1,
            ProcessPerMonitorDpiAware = 2
        }

        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        public static void EnablePerMonitorDpi()
        {
            try
            {
                if (Environment.OSVersion.Version >= new Version(6, 3, 0)) // Windows 8.1 and later
                {
                    SetProcessDpiAwareness(ProcessDpiAwareness.ProcessPerMonitorDpiAware);
                }
                else if (Environment.OSVersion.Version >= new Version(6, 0, 0)) // Windows Vista and later
                {
                    SetProcessDPIAware();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set DPI awareness: {ex.Message}");
            }
        }

        public static double GetScalingFactor(Visual visual)
        {
            try
            {
                var source = PresentationSource.FromVisual(visual);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get scaling factor: {ex.Message}");
            }
            return 1.0;
        }

        public static void OptimizeForHighDpi(FrameworkElement element)
        {
            try
            {
                RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.HighQuality);
                element.UseLayoutRounding = true;
                element.SnapsToDevicePixels = true;
                
                TextOptions.SetTextRenderingMode(element, TextRenderingMode.ClearType);
                TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);
                RenderOptions.SetClearTypeHint(element, ClearTypeHint.Enabled);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to optimize for high DPI: {ex.Message}");
            }
        }
    }
}