using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows;

namespace Plexity.ViewModels
{
    public class TweaksViewModel : INotifyPropertyChanged, IDisposable
    {
        // Configuration file path
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tweaks.config");
        private bool suspendSave = false;

        public TweaksViewModel()
        {
            LoadSettings();
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Properties and Backing Fields for Toggles

        private bool launchOnStartup;
        public bool LaunchOnStartup
        {
            get => launchOnStartup;
            set
            {
                if (launchOnStartup != value)
                {
                    launchOnStartup = value;
                    OnPropertyChanged(nameof(LaunchOnStartup));
                    SetLaunchOnStartup(value);
                    SaveSettings();
                }
            }
        }

        private bool disableWindowsTips;
        public bool DisableWindowsTips
        {
            get => disableWindowsTips;
            set
            {
                if (disableWindowsTips != value)
                {
                    disableWindowsTips = value;
                    OnPropertyChanged(nameof(DisableWindowsTips));
                    SetWindowsTips(value);
                    SaveSettings();
                }
            }
        }

        private bool disableWindowsDefender;
        public bool DisableWindowsDefender
        {
            get => disableWindowsDefender;
            set
            {
                if (disableWindowsDefender != value)
                {
                    disableWindowsDefender = value;
                    OnPropertyChanged(nameof(DisableWindowsDefender));
                    SetWindowsDefender(value);
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
                    OnPropertyChanged(nameof(NetworkBoost));

                    if (networkBoost)
                        EnableNetworkBoost();
                    else
                        DisableNetworkBoost();

                    SaveSettings();
                }
            }
        }

        private bool disableFirewall;
        public bool DisableFirewall
        {
            get => disableFirewall;
            set
            {
                if (disableFirewall != value)
                {
                    disableFirewall = value;
                    OnPropertyChanged(nameof(DisableFirewall));
                    SetFirewall(value);
                    SaveSettings();
                }
            }
        }

        private bool disableCortana;
        public bool DisableCortana
        {
            get => disableCortana;
            set
            {
                if (disableCortana != value)
                {
                    disableCortana = value;
                    OnPropertyChanged(nameof(DisableCortana));
                    SetCortana(value);
                    SaveSettings();
                }
            }
        }

        private bool disableErrorReporting;
        public bool DisableErrorReporting
        {
            get => disableErrorReporting;
            set
            {
                if (disableErrorReporting != value)
                {
                    disableErrorReporting = value;
                    OnPropertyChanged(nameof(DisableErrorReporting));
                    SetErrorReporting(value);
                    SaveSettings();
                }
            }
        }

        private bool disableLiveTiles;
        public bool DisableLiveTiles
        {
            get => disableLiveTiles;
            set
            {
                if (disableLiveTiles != value)
                {
                    disableLiveTiles = value;
                    OnPropertyChanged(nameof(DisableLiveTiles));
                    SetLiveTiles(value);
                    SaveSettings();
                }
            }
        }

        private bool disableBackgroundApps;
        public bool DisableBackgroundApps
        {
            get => disableBackgroundApps;
            set
            {
                if (disableBackgroundApps != value)
                {
                    disableBackgroundApps = value;
                    OnPropertyChanged(nameof(DisableBackgroundApps));
                    SetBackgroundApps(value);
                    SaveSettings();
                }
            }
        }

        private bool disableWindowsSearchIndexing;
        public bool DisableWindowsSearchIndexing
        {
            get => disableWindowsSearchIndexing;
            set
            {
                if (disableWindowsSearchIndexing != value)
                {
                    disableWindowsSearchIndexing = value;
                    OnPropertyChanged(nameof(DisableWindowsSearchIndexing));
                    SetWindowsSearchIndexing(value);
                    SaveSettings();
                }
            }
        }

        private bool disableWindowsUpdateDeliveryOptimization;
        public bool DisableWindowsUpdateDeliveryOptimization
        {
            get => disableWindowsUpdateDeliveryOptimization;
            set
            {
                if (disableWindowsUpdateDeliveryOptimization != value)
                {
                    disableWindowsUpdateDeliveryOptimization = value;
                    OnPropertyChanged(nameof(DisableWindowsUpdateDeliveryOptimization));
                    SetDeliveryOptimization(value);
                    SaveSettings();
                }
            }
        }

        private bool disableWindowsDefenderRealtimeProtection;
        public bool DisableWindowsDefenderRealtimeProtection
        {
            get => disableWindowsDefenderRealtimeProtection;
            set
            {
                if (disableWindowsDefenderRealtimeProtection != value)
                {
                    disableWindowsDefenderRealtimeProtection = value;
                    OnPropertyChanged(nameof(DisableWindowsDefenderRealtimeProtection));
                    SetDefenderRealtimeProtection(value);
                    SaveSettings();
                }
            }
        }

        private bool enableDarkMode;
        public bool EnableDarkMode
        {
            get => enableDarkMode;
            set
            {
                if (enableDarkMode != value)
                {
                    enableDarkMode = value;
                    OnPropertyChanged(nameof(EnableDarkMode));
                    SetDarkMode(value);
                    SaveSettings();
                }
            }
        }

        private bool disableTelemetry;
        public bool DisableTelemetry
        {
            get => disableTelemetry;
            set
            {
                if (disableTelemetry != value)
                {
                    disableTelemetry = value;
                    OnPropertyChanged(nameof(DisableTelemetry));
                    SetTelemetry(value);
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
                    OnPropertyChanged(nameof(DisableAnimations));
                    SetAnimations(value);
                    SaveSettings();
                }
            }
        }

        private bool disableLockScreen;
        public bool DisableLockScreen
        {
            get => disableLockScreen;
            set
            {
                if (disableLockScreen != value)
                {
                    disableLockScreen = value;
                    OnPropertyChanged(nameof(DisableLockScreen));
                    SetLockScreen(value);
                    SaveSettings();
                }
            }
        }

        private bool disableActionCenter;
        public bool DisableActionCenter
        {
            get => disableActionCenter;
            set
            {
                if (disableActionCenter != value)
                {
                    disableActionCenter = value;
                    OnPropertyChanged(nameof(DisableActionCenter));
                    SetActionCenter(value);
                    SaveSettings();
                }
            }
        }

        private bool disableFeedback;
        public bool DisableFeedback
        {
            get => disableFeedback;
            set
            {
                if (disableFeedback != value)
                {
                    disableFeedback = value;
                    OnPropertyChanged(nameof(DisableFeedback));
                    SetFeedback(value);
                    SaveSettings();
                }
            }
        }

        private bool disableRemoteAssistance;
        public bool DisableRemoteAssistance
        {
            get => disableRemoteAssistance;
            set
            {
                if (disableRemoteAssistance != value)
                {
                    disableRemoteAssistance = value;
                    OnPropertyChanged(nameof(DisableRemoteAssistance));
                    SetRemoteAssistance(value);
                    SaveSettings();
                }
            }
        }

        private bool disableLocationTracking;
        public bool DisableLocationTracking
        {
            get => disableLocationTracking;
            set
            {
                if (disableLocationTracking != value)
                {
                    disableLocationTracking = value;
                    OnPropertyChanged(nameof(DisableLocationTracking));
                    SetLocationTracking(value);
                    SaveSettings();
                }
            }
        }

        private bool _enableExplorerPreview;
        public bool EnableExplorerPreview
        {
            get => _enableExplorerPreview;
            set
            {
                if (_enableExplorerPreview != value)
                {
                    _enableExplorerPreview = value;
                    OnPropertyChanged();
                    SetRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowPreviewHandlers", value);
                    SaveSettings();
                }
            }
        }

        private bool _disableUsbSelectiveSuspend;
        public bool DisableUsbSelectiveSuspend
        {
            get => _disableUsbSelectiveSuspend;
            set
            {
                if (_disableUsbSelectiveSuspend != value)
                {
                    _disableUsbSelectiveSuspend = value;
                    OnPropertyChanged();
                    SetPowerSetting("54533251-82be-4824-96c1-47b60b740d00", "5c5bb349-ad29-4ee2-9d0b-2b25270f7a81", value ? 0 : 1);
                    SaveSettings();
                }
            }
        }

        private bool _enableRemoteDesktop;
        public bool EnableRemoteDesktop
        {
            get => _enableRemoteDesktop;
            set
            {
                if (_enableRemoteDesktop != value)
                {
                    _enableRemoteDesktop = value;
                    OnPropertyChanged();
                    SetRegistryValue("SYSTEM\\CurrentControlSet\\Control\\Terminal Server", "fDenyTSConnections", !value);
                    SaveSettings();
                }
            }
        }

        private bool _enableQuickAccess;
        public bool EnableQuickAccess
        {
            get => _enableQuickAccess;
            set
            {
                if (_enableQuickAccess != value)
                {
                    _enableQuickAccess = value;
                    OnPropertyChanged();
                    SetRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", value);
                    SaveSettings();
                }
            }
        }

        private bool _enableIndexing;
        public bool EnableIndexing
        {
            get => _enableIndexing;
            set
            {
                if (_enableIndexing != value)
                {
                    _enableIndexing = value;
                    OnPropertyChanged();
                    SetServiceStartup("WSearch", value);
                    SaveSettings();
                }
            }
        }

        private bool _disableSMBv1;
        public bool DisableSMBv1
        {
            get => _disableSMBv1;
            set
            {
                if (_disableSMBv1 != value)
                {
                    _disableSMBv1 = value;
                    OnPropertyChanged();
                    SetRegistryValue("SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters", "SMB1", !value);
                    SaveSettings();
                }
            }
        }

        private bool _enableStorageSpaces;
        public bool EnableStorageSpaces
        {
            get => _enableStorageSpaces;
            set
            {
                if (_enableStorageSpaces != value)
                {
                    _enableStorageSpaces = value;
                    OnPropertyChanged();
                    SetServiceStartup("SpacePort", value);
                    SaveSettings();
                }
            }
        }

        private bool _disableSyncCenter;
        public bool DisableSyncCenter
        {
            get => _disableSyncCenter;
            set
            {
                if (_disableSyncCenter != value)
                {
                    _disableSyncCenter = value;
                    OnPropertyChanged();
                    SetServiceStartup("OfflineFiles", !value);
                    SaveSettings();
                }
            }
        }

        private bool _enableWindowsHello;
        public bool EnableWindowsHello
        {
            get => _enableWindowsHello;
            set
            {
                if (_enableWindowsHello != value)
                {
                    _enableWindowsHello = value;
                    OnPropertyChanged();
                    SetRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\HelloFace", "UseWindowsHello", value);
                    SaveSettings();
                }
            }
        }

        private bool _disableXboxGameBar;
        public bool DisableXboxGameBar
        {
            get => _disableXboxGameBar;
            set
            {
                if (_disableXboxGameBar != value)
                {
                    _disableXboxGameBar = value;
                    OnPropertyChanged();
                    SetRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", !value);
                    SetRegistryValue("Software\\Microsoft\\GameBar", "ShowGameBar", !value);
                    SaveSettings();
                }
            }
        }

        private bool disableOneDrive;
        public bool DisableOneDrive
        {
            get => disableOneDrive;
            set
            {
                if (disableOneDrive != value)
                {
                    disableOneDrive = value;
                    OnPropertyChanged(nameof(DisableOneDrive));
                    SetOneDrive(value);
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Toggle Implementations

        private void SetRegistryValue(string path, string key, object value)
        {
            try
            {
                using var regKey = Registry.CurrentUser.CreateSubKey(path);
                regKey?.SetValue(key, value is bool b ? (b ? 1 : 0) : value);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage(
    $"Error writing to registry: {ex.Message}",
    "Error",
    MessageBoxButton.OK,
    MessageBoxImage.Error);

            }
        }

        private void SetServiceStartup(string serviceName, bool enable)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                var key = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Services\\{serviceName}", true);
                key?.SetValue("Start", enable ? 2 : 4);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Error setting service startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetPowerSetting(string subgroup, string setting, int value)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/SETACVALUEINDEX SCHEME_CURRENT {subgroup} {setting} {value}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Error setting power option: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetLaunchOnStartup(bool enable)
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true);
                string appName = "WindowsTweakerApp";
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                if (enable)
                    key.SetValue(appName, $"\"{exePath}\"");
                else
                    key.DeleteValue(appName, false);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error setting launch on startup: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetLocationTracking(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("Value", disable ? "Deny" : "Allow", RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling location tracking: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetRemoteAssistance(bool disable)
        {
            try
            {
                string keyPath = @"SYSTEM\CurrentControlSet\Control\Remote Assistance";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("fAllowToGetHelp", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Remote Assistance: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetFeedback(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Siuf\Rules";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("NumberOfSIUFInPeriod", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling feedback requests: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetLockScreen(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("NoLockScreen", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Lock Screen: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActionCenter(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("DisableNotificationCenter", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Action Center: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetTelemetry(bool disable)
        {
            try
            {
                string keyPath1 = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath1))
                {
                    key.SetValue("AllowTelemetry", disable ? 0 : 3, RegistryValueKind.DWord);
                }

                string keyPath2 = @"SYSTEM\CurrentControlSet\Services\DiagTrack";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath2, true))
                {
                    if (key != null)
                        key.SetValue("Start", disable ? 4 : 2, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling telemetry: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetAnimations(bool disable)
        {
            try
            {
                string keyPath = @"Control Panel\Desktop\WindowMetrics";
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                if (key != null)
                {
                    key.SetValue("MinAnimate", disable ? "0" : "1", RegistryValueKind.String);
                }

                string performanceKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                using RegistryKey perfKey = Registry.CurrentUser.OpenSubKey(performanceKeyPath, true);
                if (perfKey != null)
                {
                    perfKey.SetValue("VisualFXSetting", disable ? 2 : 0, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling animations: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetWindowsTips(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("SubscribedContent-338388Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Windows tips: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetWindowsDefender(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows Defender";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("DisableAntiSpyware", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Windows Defender: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetFirewall(bool disable)
        {
            try
            {
                ServiceController firewallSvc = new ServiceController("MpsSvc");
                if (disable && firewallSvc.Status == ServiceControllerStatus.Running)
                    firewallSvc.Stop();
                else if (!disable && firewallSvc.Status != ServiceControllerStatus.Running)
                    firewallSvc.Start();
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Firewall: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetCortana(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("AllowCortana", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Cortana: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetErrorReporting(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("Disabled", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling error reporting: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetLiveTiles(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("NoTileApplicationNotification", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling live tiles: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetBackgroundApps(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("GlobalUserDisabled", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling background apps: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetWindowsSearchIndexing(bool disable)
        {
            try
            {
                ServiceController sc = new ServiceController("WSearch");
                if (disable && sc.Status == ServiceControllerStatus.Running)
                    sc.Stop();
                else if (!disable && sc.Status != ServiceControllerStatus.Running)
                    sc.Start();
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Windows Search Indexing: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDeliveryOptimization(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\DeliveryOptimization\Config";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("DODownloadMode", disable ? 0 : 3, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Delivery Optimization: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableNetworkBoost()
        {
            RunCommand("sc stop wuauserv");
            RunCommand("sc config wuauserv start= disabled");
            RunCommand("sc stop bits");
            RunCommand("sc config bits start= disabled");
            RunPowerShellCommand("Set-DeliveryOptimizationStatus -Policy Disabled");
        }

        private void DisableNetworkBoost()
        {
            RunCommand("sc config wuauserv start= auto");
            RunCommand("sc start wuauserv");
            RunCommand("sc config bits start= delayed-auto");
            RunCommand("sc start bits");
            RunPowerShellCommand("Set-DeliveryOptimizationStatus -Policy Default");
        }

        private void RunCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                };
                using var process = Process.Start(processInfo);
                process.WaitForExit();
            }
            catch (Exception)
            {
            }
        }

        private void RunPowerShellCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo("powershell.exe", "-Command " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                };
                using var process = Process.Start(processInfo);
                process.WaitForExit();
            }
            catch (Exception)
            {
            }
        }

        private void SetDefenderRealtimeProtection(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("DisableRealtimeMonitoring", disable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Defender realtime protection: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDarkMode(bool enable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);
                key.SetValue("AppsUseLightTheme", enable ? 0 : 1, RegistryValueKind.DWord);
                key.SetValue("SystemUsesLightTheme", enable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling Dark Mode: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetOneDrive(bool disable)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive";
                using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue("DisableFileSync", disable ? 1 : 0, RegistryValueKind.DWord);

                if (disable)
                {
                    foreach (var proc in Process.GetProcessesByName("OneDrive"))
                    {
                        try { proc.Kill(); }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Error toggling OneDrive: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save/Load

        public void SaveSettings()
        {
            if (suspendSave) return;

            try
            {
                using StreamWriter sw = new StreamWriter(configPath);
                foreach (var prop in GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(bool))
                    {
                        sw.WriteLine($"{prop.Name}={prop.GetValue(this)}");
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Failed to save settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSettings()
        {
            if (!File.Exists(configPath)) return;

            suspendSave = true;
            try
            {
                string[] lines = File.ReadAllLines(configPath);
                var props = GetType().GetProperties();

                foreach (string line in lines)
                {
                    string[] parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    string name = parts[0];
                    bool value = bool.TryParse(parts[1], out bool val) && val;

                    foreach (var prop in props)
                    {
                        if (prop.Name == name && prop.PropertyType == typeof(bool))
                        {
                            prop.SetValue(this, value);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage("Failed to load settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                suspendSave = false;
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}
