using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using System.ServiceProcess;

namespace Plexity.ViewModels
{
    public class TweaksViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly string configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "tweaks_config.txt");


        private bool suspendSave = false;

        public TweaksViewModel()
        {
            LoadSettings();
        }

        private bool launchOnStartup;
        public bool LaunchOnStartup
        {
            get => launchOnStartup;
            set
            {
                if (launchOnStartup != value)
                {
                    launchOnStartup = value;
                    OnPropertyChanged();

                    if (value != IsStartupEnabled())
                        ToggleStartup(value);

                    SaveSettings();
                }
            }
        }

        private bool optimizePerformance;
        public bool OptimizePerformance
        {
            get => optimizePerformance;
            set
            {
                if (optimizePerformance != value)
                {
                    optimizePerformance = value;
                    OnPropertyChanged();

                    if (value) KillBloatware(); else RestartBloatware();

                    SaveSettings();
                }
            }
        }

        private bool autoClearTemp;
        public bool AutoClearTemp
        {
            get => autoClearTemp;
            set
            {
                if (autoClearTemp != value)
                {
                    autoClearTemp = value;
                    OnPropertyChanged();

                    if (value) ClearTempFiles();

                    SaveSettings();
                }
            }
        }

        private bool lowPowerMode;
        public bool LowPowerMode
        {
            get => lowPowerMode;
            set
            {
                if (lowPowerMode != value)
                {
                    lowPowerMode = value;
                    OnPropertyChanged();

                    if (value != IsLowPowerModeActive())
                    {
                        if (value) EnableLowPowerMode(); else DisableLowPowerMode();
                    }

                    SaveSettings();
                }
            }
        }

        private bool enableGameMode;
        public bool EnableGameMode
        {
            get => enableGameMode;
            set
            {
                if (enableGameMode != value)
                {
                    enableGameMode = value;
                    OnPropertyChanged();

                    if (value != IsGameModeEnabled())
                    {
                        if (value) EnableGameModeTweaks(); else DisableGameModeTweaks();
                    }

                    SaveSettings();
                }
            }
        }

        private bool muteDiscord;
        public bool MuteDiscord
        {
            get => muteDiscord;
            set
            {
                if (muteDiscord != value)
                {
                    muteDiscord = value;
                    OnPropertyChanged();

                    if (value != IsMicMuted())
                    {
                        if (value) MuteMic(); else UnmuteMic();
                    }

                    SaveSettings();
                }
            }
        }

        private bool disableAnimations;
        public bool DisableAnimations
        {
            get => disableAnimations;
            set
            {
                if (disableAnimations != value)
                {
                    disableAnimations = value;
                    OnPropertyChanged();

                    if (value != AreAnimationsDisabled())
                        ToggleAnimations(value);

                    SaveSettings();
                }
            }
        }

        private bool networkBoost;
        public bool NetworkBoost
        {
            get => networkBoost;
            set
            {
                if (networkBoost != value)
                {
                    networkBoost = value;
                    OnPropertyChanged();

                    if (value != IsNetworkBoostActive())
                        ToggleNetworkBoost(value);

                    SaveSettings();
                }
            }
        }

        private bool autoRestoreTweaks;
        public bool AutoRestoreTweaks
        {
            get => autoRestoreTweaks;
            set
            {
                if (autoRestoreTweaks != value)
                {
                    autoRestoreTweaks = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #region State Check Helpers

        private bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                var value = key?.GetValue("Plexity") as string;
                return value == Process.GetCurrentProcess().MainModule.FileName;
            }
            catch { return false; }
        }

        private bool IsLowPowerModeActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg", "/getactivescheme")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                const string lowPowerGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";
                return output.IndexOf(lowPowerGuid, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private bool IsGameModeEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\GameBar", false);
                var val = key?.GetValue("AutoGameModeEnabled");
                if (val is int i) return i == 1;
                return false;
            }
            catch { return false; }
        }

        private bool IsMicMuted()
        {
            // Placeholder heuristic - always false here
            return false;
        }

        private bool AreAnimationsDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
                var val = key?.GetValue("UserPreferencesMask") as byte[];
                if (val == null) return false;

                byte[] disabledMask = new byte[] { 0x90, 0x12, 0x03, 0x80 };
                return val.Length == disabledMask.Length && StructuralComparisons.StructuralEqualityComparer.Equals(val, disabledMask);
            }
            catch { return false; }
        }

        private bool IsNetworkBoostActive()
        {
            try
            {
                ServiceController sc = new ServiceController("wuauserv");
                return sc.Status == ServiceControllerStatus.Stopped;
            }
            catch { return false; }
        }

        #endregion

        #region Toggle Implementations

        private void ToggleStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (enable)
                    key?.SetValue("Plexity", Process.GetCurrentProcess().MainModule.FileName);
                else
                    key?.DeleteValue("Plexity", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ToggleStartup error: {ex}");
            }
        }

        private void KillBloatware()
        {
            foreach (var process in new[] { "OneDrive", "Cortana", "Teams" })
            {
                foreach (var p in Process.GetProcessesByName(process))
                {
                    try { p.Kill(); } catch { }
                }
            }
        }

        private void RestartBloatware()
        {
            try
            {
                var oneDrivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive\OneDrive.exe");
                if (File.Exists(oneDrivePath))
                    Process.Start(oneDrivePath);
                var teamsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Teams\current\Teams.exe");
                if (File.Exists(teamsPath))
                    Process.Start(teamsPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RestartBloatware error: {ex}");
            }
        }

        private void ClearTempFiles()
        {
            try
            {
                var temp = Path.GetTempPath();
                foreach (var file in Directory.GetFiles(temp)) File.Delete(file);
                foreach (var dir in Directory.GetDirectories(temp)) Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClearTempFiles error: {ex}");
            }
        }

        private void EnableLowPowerMode()
        {
            try
            {
                Process.Start(new ProcessStartInfo("powercfg", "/setactive a1841308-3541-4fab-bc81-f71556f20b4a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnableLowPowerMode error: {ex}");
            }
        }

        private void DisableLowPowerMode()
        {
            try
            {
                Process.Start(new ProcessStartInfo("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisableLowPowerMode error: {ex}");
            }
        }

        private void EnableGameModeTweaks()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
                key?.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnableGameModeTweaks error: {ex}");
            }
        }

        private void DisableGameModeTweaks()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
                key?.SetValue("AutoGameModeEnabled", 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisableGameModeTweaks error: {ex}");
            }
        }

        private void MuteMic()
        {
            try
            {
                Process.Start("nircmd.exe", "mutesysvolume 1 mic")?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MuteMic error: {ex}");
            }
        }

        private void UnmuteMic()
        {
            try
            {
                Process.Start("nircmd.exe", "mutesysvolume 0 mic")?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UnmuteMic error: {ex}");
            }
        }

        private void ToggleAnimations(bool disable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
                if (disable)
                {
                    key?.SetValue("UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80 }, RegistryValueKind.Binary);
                }
                else
                {
                    key?.SetValue("UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x00 }, RegistryValueKind.Binary);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ToggleAnimations error: {ex}");
            }
        }

        private void ToggleNetworkBoost(bool enable)
        {
            try
            {
                ServiceController sc = new ServiceController("wuauserv");
                if (enable && sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
                else if (!enable && sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ToggleNetworkBoost error: {ex}");
            }
        }

        #endregion

        private void SaveSettings()
        {
            if (suspendSave) return;

            try
            {
                var data = string.Join("|",
                    launchOnStartup,
                    optimizePerformance,
                    autoClearTemp,
                    lowPowerMode,
                    enableGameMode,
                    muteDiscord,
                    disableAnimations,
                    networkBoost,
                    autoRestoreTweaks);

                File.WriteAllText(configPath, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveSettings error: {ex}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                var values = File.ReadAllText(configPath).Split('|');
                if (values.Length < 9) return;

                suspendSave = true;

                launchOnStartup = bool.Parse(values[0]);
                optimizePerformance = bool.Parse(values[1]);
                autoClearTemp = bool.Parse(values[2]);
                lowPowerMode = bool.Parse(values[3]);
                enableGameMode = bool.Parse(values[4]);
                muteDiscord = bool.Parse(values[5]);
                disableAnimations = bool.Parse(values[6]);
                networkBoost = bool.Parse(values[7]);
                autoRestoreTweaks = bool.Parse(values[8]);

                OnPropertyChanged(null);

                suspendSave = false;

                if (autoRestoreTweaks)
                {
                    ApplyAllTweaks();
                }
                else
                {
                    // Just sync startup toggle minimally
                    if (launchOnStartup != IsStartupEnabled())
                        ToggleStartup(launchOnStartup);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSettings error: {ex}");
            }
        }

        private void ApplyAllTweaks()
        {
            suspendSave = true;

            try
            {
                if (launchOnStartup != IsStartupEnabled())
                    ToggleStartup(launchOnStartup);

                if (optimizePerformance) KillBloatware(); else RestartBloatware();

                if (autoClearTemp) ClearTempFiles();

                if (lowPowerMode != IsLowPowerModeActive())
                {
                    if (lowPowerMode) EnableLowPowerMode(); else DisableLowPowerMode();
                }

                if (enableGameMode != IsGameModeEnabled())
                {
                    if (enableGameMode) EnableGameModeTweaks(); else DisableGameModeTweaks();
                }

                if (muteDiscord != IsMicMuted())
                {
                    if (muteDiscord) MuteMic(); else UnmuteMic();
                }

                if (disableAnimations != AreAnimationsDisabled())
                    ToggleAnimations(disableAnimations);

                if (networkBoost != IsNetworkBoostActive())
                    ToggleNetworkBoost(networkBoost);
            }
            finally
            {
                suspendSave = false;
            }
        }

        public void Dispose()
        {

        }
    }
}
