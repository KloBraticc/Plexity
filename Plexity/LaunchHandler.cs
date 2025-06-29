using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Plexity.Enums;
using Plexity.Integrations;
using Plexity.Utility;
using Plexity.Views.Pages;

namespace Plexity
{
    public static class LaunchHandler
    {
        private const string LOG_IDENT = "LaunchHandler";

        internal static class NativeMethods
        {
            [DllImport("psapi.dll")]
            public static extern bool EmptyWorkingSet(IntPtr hProcess);
        }

        public static void ProcessLaunchArgs()
        {
            if (App.LaunchSettings.UninstallFlag.Active)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Opening uninstaller");
                LaunchUninstaller();
            }
            else if (App.LaunchSettings.WatcherFlag.Active)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Opening watcher");
                LaunchWatcher();
            }
            else if (App.LaunchSettings.RobloxLaunchMode != LaunchMode.None)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Opening bootstrapper ({App.LaunchSettings.RobloxLaunchMode})");
                LaunchRoblox(App.LaunchSettings.RobloxLaunchMode);
            }
            else if (IsRobloxProtocolLaunch())
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Detected roblox-player protocol launch.");
                LaunchRoblox(LaunchMode.Protocol);
            }
        }

        public static void LaunchWatcher()
        {
            const string TAG = $"{LOG_IDENT}::LaunchWatcher";

            Task.Run(async () =>
            {
                var watcher = new Watcher();
                try
                {
                    await watcher.Run();
                    App.Logger.WriteLine(LogLevel.Info, TAG, "Watcher task completed.");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, TAG, "Exception in Watcher.");
                    App.FinalizeExceptionHandling(ex);
                }
                finally
                {
                    watcher.Dispose();
                    if (App.Settings.Prop.CleanerOptions != CleanerOptions.Never)
                        Cleaner.DoCleaning();

                    App.Terminate();
                }
            });
        }

        public static void LaunchUninstaller()
        {
            using var interlock = new InterProcessLock("Uninstaller");
            if (!interlock.IsAcquired)
            {
                App.Terminate();
                return;
            }

            var dialog = new UninstallPage();
            dialog.ShowDialog();

            if (!dialog.Confirmed)
            {
                App.Terminate();
                return;
            }

            Installer.DoUninstall(dialog.KeepData);
            App.Terminate();
        }

        public static void LaunchInstaller()
        {
            using var interlock = new InterProcessLock("Installer");
            if (!interlock.IsAcquired || App.LaunchSettings.UninstallFlag.Active)
            {
                App.Terminate();
            }
        }

        public static void LaunchRoblox(LaunchMode launchMode)
        {
            const string TAG = $"{LOG_IDENT}::LaunchRoblox";
            const string MutexName = "ROBLOX_singletonMutex";

            if (launchMode == LaunchMode.None)
                throw new InvalidOperationException("LaunchMode cannot be None.");

            string mfplatPath = Path.Combine(Paths.System, "mfplat.dll");
            if (!File.Exists(mfplatPath))
            {
                App.Logger.WriteLine(LogLevel.Info, TAG, $"Missing system file: {mfplatPath}");
                DialogService.ShowMessage("Missing system component: mfplat.dll", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Terminate();
                return;
            }

            App.Logger.WriteLine(LogLevel.Info, TAG, "Starting Roblox Bootstrapper...");
            App.Bootstrapper = new Bootstrapper(launchMode);

            bool ownsMutex = false;
            Mutex? mutex = null;

            if (App.Settings.Prop.MultiInstanceLaunching)
            {
                try
                {
                    mutex = new Mutex(true, MutexName, out ownsMutex);
                    App.Logger.WriteLine(LogLevel.Info, TAG, ownsMutex ? "Acquired mutex." : "Mutex already held.");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, TAG, $"Failed to acquire mutex: {ex}");
                }
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await App.Bootstrapper.Run();

                    // Optimize memory usage for running Roblox instances
                    string processName = Path.GetFileNameWithoutExtension(App.RobloxPlayerAppName);
                    foreach (var proc in Process.GetProcessesByName(processName))
                    {
                        try
                        {
                            proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                            NativeMethods.EmptyWorkingSet(proc.Handle);
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Info, TAG, $"Optimization failed for process {proc.Id}: {ex.Message}");
                        }
                    }

                    App.Logger.WriteLine(LogLevel.Info, TAG, "Roblox launched and memory optimized.");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, TAG, $"Bootstrapper failed: {ex}");
                    App.FinalizeExceptionHandling(ex);
                }

                if (mutex != null && ownsMutex)
                {
                    string processName = Path.GetFileNameWithoutExtension(App.RobloxPlayerAppName);
                    while (Process.GetProcessesByName(processName).Any())
                    {
                        await Task.Delay(3000);
                    }

                    App.Logger.WriteLine(LogLevel.Info, TAG, "Roblox closed. Releasing mutex.");
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                App.Terminate();
            });

            App.Logger.WriteLine(LogLevel.Info, TAG, "LaunchRoblox invoked.");
        }

        private static bool IsRobloxProtocolLaunch()
        {
            string[] args = Environment.GetCommandLineArgs();
            return args.Any(arg => arg.StartsWith("roblox-player:", StringComparison.OrdinalIgnoreCase));
        }
    }
}
