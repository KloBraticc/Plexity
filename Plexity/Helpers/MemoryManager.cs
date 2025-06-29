using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Plexity.Helpers
{
    public static class MemoryManager
    {
        private static ILogger? _logger;
        private static Timer? _memoryCleanupTimer;
        private static readonly object _lockObject = new();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern bool EmptyWorkingSet(IntPtr processHandle);

        [DllImport("psapi.dll")]
        private static extern bool GetProcessMemoryInfo(IntPtr process, out PROCESS_MEMORY_COUNTERS counters, uint size);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_MEMORY_COUNTERS
        {
            public uint cb;
            public uint PageFaultCount;
            public IntPtr PeakWorkingSetSize;
            public IntPtr WorkingSetSize;
            public IntPtr QuotaPeakPagedPoolUsage;
            public IntPtr QuotaPagedPoolUsage;
            public IntPtr QuotaPeakNonPagedPoolUsage;
            public IntPtr QuotaNonPagedPoolUsage;
            public IntPtr PagefileUsage;
            public IntPtr PeakPagefileUsage;
        }

        public static void InitializeMemoryManagement()
        {
            try
            {
                // Get logger without using generic type parameter
                _logger = App.Services.GetService(typeof(ILogger<App>)) as ILogger;
                
                // Configure garbage collection for better performance
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                
                // Start memory monitoring timer (every 5 minutes)
                _memoryCleanupTimer = new Timer(PerformMemoryCleanup, null, 
                    TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
                
                // Use App.Logger instead of Microsoft.Extensions.Logging to avoid EventLog issues
                App.Logger.WriteLine(LogLevel.Info, "MemoryManager", "Memory management initialized");
            }
            catch (Exception ex)
            {
                // Use App.Logger instead of Microsoft.Extensions.Logging
                App.Logger.WriteException("MemoryManager", ex);
            }
        }

        public static async Task OptimizeMemoryAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        var process = Process.GetCurrentProcess();
                        var beforeMemory = GC.GetTotalMemory(false);

                        // Force full garbage collection
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                        GC.WaitForPendingFinalizers();
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

                        // Compact the Large Object Heap
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();

                        // Try to trim working set (may fail on some systems)
                        try
                        {
                            SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
                            EmptyWorkingSet(process.Handle);
                        }
                        catch (EntryPointNotFoundException)
                        {
                            // API not available on this system, continue without it
                            App.Logger.WriteLine(LogLevel.Warning, "MemoryManager", "Working set optimization not available on this system");
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Warning, "MemoryManager", $"Working set optimization failed: {ex.Message}");
                        }

                        var afterMemory = GC.GetTotalMemory(false);
                        var freedBytes = beforeMemory - afterMemory;

                        App.Logger.WriteLine(LogLevel.Info, "MemoryManager", $"Memory optimization completed. Freed: {freedBytes / 1024 / 1024} MB");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteException("MemoryManager", ex);
                    }
                }
            });
        }

        private static void PerformMemoryCleanup(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var memoryInfo = GetMemoryInfo();
                    if (memoryInfo.WorkingSetMB > 200) // If using more than 200MB
                    {
                        await OptimizeMemoryAsync();
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("MemoryManager", ex);
                }
            });
        }

        public static MemoryInfo GetMemoryInfo()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var managedMemory = GC.GetTotalMemory(false);
                
                try
                {
                    if (GetProcessMemoryInfo(process.Handle, out var counters, (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS>()))
                    {
                        return new MemoryInfo
                        {
                            ManagedMemoryMB = managedMemory / 1024 / 1024,
                            WorkingSetMB = (long)counters.WorkingSetSize / 1024 / 1024,
                            PagedMemoryMB = (long)counters.PagefileUsage / 1024 / 1024,
                            PeakWorkingSetMB = (long)counters.PeakWorkingSetSize / 1024 / 1024
                        };
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // API not available, return basic info
                    return new MemoryInfo
                    {
                        ManagedMemoryMB = managedMemory / 1024 / 1024,
                        WorkingSetMB = process.WorkingSet64 / 1024 / 1024,
                        PagedMemoryMB = process.PagedMemorySize64 / 1024 / 1024,
                        PeakWorkingSetMB = process.PeakWorkingSet64 / 1024 / 1024
                    };
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MemoryManager", ex);
            }

            return new MemoryInfo();
        }

        public static void Dispose()
        {
            _memoryCleanupTimer?.Dispose();
        }
    }

    public class MemoryInfo
    {
        public long ManagedMemoryMB { get; set; }
        public long WorkingSetMB { get; set; }
        public long PagedMemoryMB { get; set; }
        public long PeakWorkingSetMB { get; set; }
    }
}