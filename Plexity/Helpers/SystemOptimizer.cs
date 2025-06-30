using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Plexity.Helpers
{
    public static class SystemOptimizer
    {
        public static bool IsLowEndMachine()
        {
            // Check CPU, RAM
            int cpuCount = Environment.ProcessorCount;
            
            // Use GetPhysicallyInstalledSystemMemory as it's more reliable
            long totalMemoryKb = 0;
            if (NativeMethods.GetPhysicallyInstalledSystemMemory(out totalMemoryKb))
            {
                long totalMemoryGb = totalMemoryKb / (1024 * 1024);
                return cpuCount <= 2 || totalMemoryGb < 4;
            }
            
            // Fallback
            return cpuCount <= 2;
        }
        
        public static void OptimizeForCurrentSystem()
        {
            // Set process priority
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    process.PriorityClass = ProcessPriorityClass.AboveNormal;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Warning, "SystemOptimizer", $"Failed to set process priority: {ex.Message}");
            }
            
            // Optimize GC for interactive application
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
            
            // If low-end machine, apply more aggressive optimizations
            if (IsLowEndMachine())
            {
                // Reduce animation frame rate
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new System.Windows.FrameworkPropertyMetadata { DefaultValue = 15 });
                
                // More aggressive memory management
                GC.Collect(2, GCCollectionMode.Optimized, false, true);
            }
        }
        
        // Native method for getting system memory
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            internal static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
        }
    }
}