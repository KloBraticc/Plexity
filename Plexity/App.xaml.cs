using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Plexity.Enums;
using Plexity.Helpers;
using Plexity.Models;
using Plexity.Models.APIs.GitHub;
using Plexity.Models.Persistable;
using Plexity.Models.SettingTasks.Base;
using Plexity.Properties;
using Plexity.Services;
using Plexity.UI.ViewModels.Installer;
using Plexity.UI.ViewModels.Settings;
using Plexity.Utility;
using Plexity.ViewModels;
using Plexity.ViewModels.Pages;
using Plexity.ViewModels.Windows;
using Plexity.Views.Pages;
using Plexity.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Markup;

namespace Plexity
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)!);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();
                services.AddHostedService<ApplicationHostService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();

                services.AddSingleton<DeploymentPage>();
                services.AddSingleton<DataViewModel>();

                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<AboutPage>();
                services.AddSingleton<AboutViewModel>();

                services.AddSingleton<LaunchPage>();
                services.AddSingleton<LaunchPageViewModel>();

                services.AddSingleton<UninstallPage>();
                services.AddSingleton<UninstallViewModel>();

                services.AddSingleton<InstallViewModel>();

                services.AddSingleton<ModsPage>();
                services.AddSingleton<ModsViewModel>();

                services.AddSingleton<FastFlagsViewModel>();
                services.AddSingleton<FastFlagsPage>();

                services.AddSingleton<EditorViewModel>();
                services.AddSingleton<FastFlagEditor>();

                services.AddSingleton<RobloxVersionsViewModel>();
                services.AddSingleton<VersionsPage>();

                services.AddSingleton<TweaksPage>();
                services.AddSingleton<TweaksViewModel>();

                services.AddSingleton<PluginsViewModel>();
                services.AddSingleton<PluginsPage>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        public static LaunchSettings LaunchSettings { get; private set; } = null!;
        public const string ProjectName = "Plexity";
        public const string ProjectOwner = "Plexity";
        public const string ProjectRepository = "/Plexity/Plexity/";
        public const string ProjectDownloadLink = "https://github.com/Plexity/Plexity/releases";
        public const string ProjectHelpLink = "https://github.com/BloxstrapLabs/Bloxstrap/wiki";
        public const string ProjectSupportLink = "https://github.com/Plexity/Plexity/issues/new";
        public const string RobloxPlayerAppName = "RobloxPlayerBeta";
        public const string RobloxStudioAppName = "RobloxStudioBeta";
        public const string ApisKey = $"Software\\{ProjectName}";
        public const string UninstallKey = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{ProjectName}";

        public static readonly Logger Logger = new();
        public static readonly JsonManager<AppSettings> Settings = new();
        public static readonly JsonManager<MessageStatus> MessageStatus = new();
        public static readonly JsonManager<State> State = new();
        public static Bootstrapper? Bootstrapper { get; set; } = null!;
        public static readonly JsonManager<RobloxState> RobloxState = new();
        public static readonly Dictionary<string, BaseTask> PendingSettingTasks = new();

        public static readonly ClientAppSettings FastFlags = new();
        public static readonly MD5 MD5Provider = MD5.Create();

        public static readonly string RobloxCookiesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Roblox\LocalStorage\RobloxCookies.dat");

        public static bool IsStudioVisible => App.State?.Prop?.Studio?.VersionGuid != null;
        private ResourceDictionary? _themeDictionary;
        private ResourceDictionary? _controlsDictionary;

        public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        private static HttpClient? _httpClient;
        public static HttpClient HttpClient => _httpClient ??= new HttpClient(
            new HttpClientLoggingHandler(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }));


        public static async Task<string> GetRemoteHashForEmojiType(string url)
        {
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var hashBytes = MD5Provider.ComputeHash(stream);
            return MD5Hash.Stringify(hashBytes);
        }
        public void InstallAppAndCreateShortcut(string sourceAppFolder, string newVersion, bool isUninstalling = false)
        {
            try
            {
                string targetFolder = Paths.AppDownloads;

                if (string.IsNullOrWhiteSpace(targetFolder))
                {
                    ShowError("Invalid target folder path.");
                    return;
                }

                if (isUninstalling)
                {
                    TryDeleteDirectory(targetFolder, showErrors: true);

                    // Remove shortcuts during uninstall
                    DeleteShortcut(Environment.SpecialFolder.DesktopDirectory);
                    DeleteShortcut(Environment.SpecialFolder.StartMenu, "Programs");

                    ShowInfo("Uninstallation complete.");
                    return;
                }

                // Validate source folder
                if (!Directory.Exists(sourceAppFolder))
                {
                    ShowError("Source folder does not exist.");
                    return;
                }

                string versionFilePath = Path.Combine(targetFolder, "version.txt");
                string? currentVersion = System.IO.File.Exists(versionFilePath)
                    ? System.IO.File.ReadAllText(versionFilePath).Trim()
                    : null;

                if (newVersion == currentVersion)
                    return;

                bool isUpgrade = !string.IsNullOrEmpty(currentVersion);

                TryDeleteDirectory(targetFolder);
                CopyDirectory(sourceAppFolder, targetFolder);
                System.IO.File.WriteAllText(versionFilePath, newVersion);

                string exePath = Path.Combine(targetFolder, "Plexity.exe");
                if (!System.IO.File.Exists(exePath))
                {
                    ShowError("Installation failed: Plexity.exe not found.");
                    return;
                }

                bool desktopCreated = CreateShortcut(exePath, Environment.SpecialFolder.DesktopDirectory);
                bool startMenuCreated = CreateShortcut(exePath, Environment.SpecialFolder.StartMenu, "Programs");

                if (desktopCreated || startMenuCreated)
                {
                    ShowInfo(isUpgrade ? $"Upgraded to {newVersion}!" : "Installation complete!");
                }
            }
            catch (Exception ex)
            {
                ShowError("Operation failed: " + ex.Message);
            }
        }

        private void TryDeleteDirectory(string path, bool showErrors = false)
        {
            if (!Directory.Exists(path)) return;

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                if (showErrors)
                    ShowError($"Failed to delete folder: {ex.Message}");
                throw;
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                System.IO.File.Copy(file, targetFilePath, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectory(dir, targetSubDir);
            }
        }

        // .NET 8 Compatible shortcut creation using PowerShell
        private bool CreateShortcut(string exePath, Environment.SpecialFolder location, string? subDir = null)
        {
            try
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(location), subDir ?? string.Empty);
                Directory.CreateDirectory(folderPath);

                string shortcutPath = Path.Combine(folderPath, "Plexity.lnk");
                string workingDirectory = Path.GetDirectoryName(exePath) ?? "";

                // Use PowerShell to create shortcut (works in .NET 8)
                string script = $@"
                    $WshShell = New-Object -comObject WScript.Shell
                    $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
                    $Shortcut.TargetPath = '{exePath}'
                    $Shortcut.WorkingDirectory = '{workingDirectory}'
                    $Shortcut.IconLocation = '{exePath},0'
                    $Shortcut.Save()
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();

                return System.IO.File.Exists(shortcutPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Shortcut error: {ex.Message}", "Shortcut Creation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void DeleteShortcut(Environment.SpecialFolder location, string? subDir = null)
        {
            try
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(location), subDir ?? string.Empty);
                string shortcutPath = Path.Combine(folderPath, "Plexity.lnk");

                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete shortcut: {ex.Message}", "Uninstall", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowError(string message)
        {
            DialogService.ShowMessage(message, "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            DialogService.ShowMessage(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Initialize memory management early
            MemoryManager.InitializeMemoryManagement();

            // Enable high DPI support
            HighDpiHelper.EnablePerMonitorDpi();

            const string LOG_IDENT = "App::OnStartup";

            try
            {
                // Initialize Paths first
                string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plexity");
                Paths.Initialize(baseDirectory);

                // Load settings with enhanced thread-safe manager
                var settingsManager = new ThreadSafeSettingsManager(Services.GetRequiredService<ILogger<ThreadSafeSettingsManager>>());
                await settingsManager.LoadAsync();

                // Initialize enhanced FastFlag manager
                var fastFlagManager = new EnhancedFastFlagManager(Services.GetRequiredService<ILogger<EnhancedFastFlagManager>>());
                await fastFlagManager.LoadAsync();

                // Apply theme and density settings
                try
                {
                    if (SystemAccentColorHelper.IsSystemAccentColorEnabled())
                    {
                        bool isDarkMode = SystemAccentColorHelper.IsSystemUsingDarkMode();
                        SystemAccentColorHelper.ApplySystemAccentColor(this, isDarkMode);
                    }

                    UIDensityManager.ApplyDensityMode((UIDensityManager.DensityMode)Settings.Prop.UIDisplayDensity);
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(LogLevel.Warning, LOG_IDENT, $"Failed to apply theme settings: {ex.Message}");
                }

                await _host.StartAsync();

                // Enhanced memory optimization
                await MemoryManager.OptimizeMemoryAsync();

                var themeMode = App.Settings.Prop?.ThemeModes;
                var theme = themeMode == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
                ApplicationThemeManager.Apply(theme);

                // Rest of your existing startup code...
                // (Keep all existing initialization logic)

                // Apply Windows-specific optimizations after main window is created
                MainWindow.Loaded += async (s, args) =>
                {
                    if (s is Window window)
                    {
                        await WindowsOptimizationHelper.OptimizeForCurrentWindowsAsync(window);
                    }
                };

                // Call base OnStartup to continue normal application startup
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Error, "App::OnStartup", $"Startup failed: {ex}");
                MessageBox.Show("An unexpected error occurred during startup:\n" + ex.Message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        // Add cleanup in OnExit
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        public static void SoftTerminate(ErrorCode exitCode = ErrorCode.ERROR_SUCCESS)
        {
            int exitCodeNum = (int)exitCode;

            Logger.WriteLine(LogLevel.Info, "App::SoftTerminate", $"Terminating with exit code {exitCodeNum} ({exitCode})");

            Current.Dispatcher.Invoke(() => Current.Shutdown(exitCodeNum));
        }

        public static async Task<GithubRelease?> GetLatestRelease()
        {
            const string LOG_IDENT = "App::GetLatestRelease";

            try
            {
                var releaseInfo = await Http.GetJson<GithubRelease>($"https://api.github.com/repos/{ProjectRepository}/releases/latest");

                if (releaseInfo is null || releaseInfo.Assets is null)
                {
                    Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Encountered invalid data");
                    return null;
                }

                return releaseInfo;
            }
            catch (Exception ex)
            {
                Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public static void Terminate(int exitCode = 0)
        {
            Logger.WriteLine(LogLevel.Info, "App::Terminate", $"Terminating with exit code {exitCode}");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var ownerWindow = Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive);

                var dialog = new ConfirmDialog($"An unhandled exception occurred:\n\n{e.Exception.Message}");

                if (ownerWindow != null)
                    dialog.Owner = ownerWindow;

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing exception dialog: {ex.Message}");
            }
            finally
            {
                e.Handled = true;
            }
        }

        internal static void FinalizeExceptionHandling(Exception ex)
        {
            DialogService.ShowMessage($"A critical exception was thrown:\n{ex}", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            Terminate(-1);
        }
    }
}