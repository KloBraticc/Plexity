// To debug the automatic updater:
// - Uncomment the definition below
// - Publish the executable
// - Launch the executable (click no when it asks you to upgrade)
// - Launch Roblox (for testing web launches, run it from the command prompt)
// - To re-test the same executable, delete it from the installation folder

// #define DEBUG_UPDATER

#if DEBUG_UPDATER
#warning "Automatic updater debugging is enabled"
#endif

using System.ComponentModel;
using System.Data;

using Microsoft.Win32;

using Plexity.AppData;
using Plexity.RobloxInterfaces;
using Plexity.Utility;
using System.IO;
using Plexity.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Plexity.UI;
using Plexity.Enums;
using System.Text.RegularExpressions;
using System.IO.Packaging;
using Plexity.Exceptions;
using Plexity.Models.Manifest;
using Plexity.Extensions;
using System.Net.Http;
using Plexity.Models.APIs.Roblox;
using System.Net;
using Plexity.Views.Pages;
using System.Windows.Navigation;
using static System.Windows.Forms;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Plexity
{
    public class Bootstrapper : IDisposable
    {
        #region Properties

        private const string AppSettings =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
            "<Settings>\r\n" +
            "	<ContentFolder>content</ContentFolder>\r\n" +
            "	<BaseUrl>http://www.roblox.com</BaseUrl>\r\n" +
            "</Settings>\r\n";

        private readonly FastZipEvents _fastZipEvents = new();
        private readonly CancellationTokenSource _cancelTokenSource = new();

        private readonly IAppData AppData;
        private readonly LaunchMode _launchMode;

        private string _launchCommandLine = App.LaunchSettings.RobloxLaunchArgs;
        private string _latestVersionGuid = null!;
        private string _latestVersionDirectory = null!;
        private PackageManifest _versionPackageManifest = null!;

        private bool _isInstalling = false;
        private long _totalDownloadedBytes = 0;

        private bool _mustUpgrade => String.IsNullOrEmpty(AppData.State.VersionGuid) || !File.Exists(AppData.ExecutablePath);
        private bool _noConnection = false;

        private AsyncMutex? _mutex;

        private int _appPid = 0;


        public LaunchPage? LaunchPage = null;

        public bool IsStudioLaunch => _launchMode != LaunchMode.Player;

        public object ProgressBarStyle { get; private set; }
        public static object WinFormsDialogBase { get; private set; }
        #endregion

        #region Core
        public Bootstrapper(LaunchMode launchMode)
        {
            _launchMode = launchMode;

            // Workaround for SharpZipLib issue:
            // Exceptions don't get thrown if failure events aren't bound.
            // See https://github.com/icsharpcode/SharpZipLib/blob/master/src/ICSharpCode.SharpZipLib/Zip/FastZip.cs/#L669-L680
            // This is probably a bug in SharpZipLib.

            // Determine if we are launching Studio or Player.
            bool isStudioLaunch = (_launchMode == LaunchMode.Studio); // or however you determine this

            AppData = isStudioLaunch ? new RobloxStudioData() : new RobloxPlayerData();

            if (AppData == null)
                throw new InvalidOperationException("AppData could not be initialized.");

            Deployment.BinaryType = AppData.BinaryType;
        }


        private async void HandleConnectionError(Exception exception)
        {
            const string LOG_IDENT = "Bootstrapper::HandleConnectionError";

            _noConnection = true;

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Connectivity check failed");
            App.Logger.WriteException(LOG_IDENT, exception);

            string message = "Bad Connection!";

            if (exception is AggregateException)
                exception = exception.InnerException!;

            bool isRobloxOnline = await IsRobloxOnlineAsync();
            if (!isRobloxOnline)
            {
                message += "\n\nRoblox appears to be offline. Check: https://status.roblox.com";
            }
            else
            {
                message += "\n\nRoblox appears to be online. The issue may be on your side or with Plexity.";
            }

            if (_mustUpgrade)
            {
                message += $"\n\n{"Roblox Upgrade Needed"}\n\n{"Try again later!"}";
                App.Terminate();
            }
            else
            {
                message += $"\n\n{"Skipping Roblox update"}";
            }

            Console.WriteLine(message);
        }

        private async Task<bool> IsRobloxOnlineAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync("https://www.roblox.com");
                    return response.IsSuccessStatusCode; // 200–299 means Roblox is up
                }
            }
            catch
            {
                return false;
            }
        }



        public async Task Run()
        {
            const string LOG_IDENT = "Bootstrapper::Run";
            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Running bootstrapper");

            try
            {
                LaunchPage?.SetCancelEnabled(true);

                var connectionResult = await Deployment.InitializeConnectivity().ConfigureAwait(false);
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Connectivity check finished");

                if (connectionResult != null)
                {
                    try
                    {
                        HandleConnectionError(connectionResult);
                        App.MessageStatus.Prop.Message = connectionResult.Message;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Error handling connection error: " + ex);
                    }
                    return;
                }

#if (!DEBUG || DEBUG_UPDATER) && !QA_BUILD
                if (App.Settings.Prop.CheckForUpdates && !App.LaunchSettings.UpgradeFlag.Active)
                {
                    try
                    {
                        if (await CheckForUpdates().ConfigureAwait(false))
                            return;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "CheckForUpdates failed: " + ex);
                    }
                }
#endif

                bool mutexAcquired = false;

                // Avoid allocating Mutex unless necessary
                try
                {
                    using var existingMutex = Mutex.OpenExisting("Plexity-Bootstrapper");
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Plexity-Bootstrapper mutex exists, waiting...");
                }
                catch (WaitHandleCannotBeOpenedException) { }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Unexpected error checking mutex: " + ex);
                }

                await using var mutex = new AsyncMutex(false, "Plexity-Bootstrapper");

                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancelTokenSource.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    await mutex.AcquireAsync(cts.Token).ConfigureAwait(false);
                    mutexAcquired = true;
                    _mutex = mutex;

                    try
                    {
                        App.Settings.Load();
                        App.State.Load();
                    }
                    catch (Exception ex)
                    {
                        App.MessageStatus.Prop.Message = "Failed to load Settings";
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Error loading settings/state: " + ex);
                    }

                    if (!_noConnection)
                    {
                        if (AppData.State.VersionGuid != _latestVersionGuid || _mustUpgrade)
                        {
                            try
                            {
                                await DownloadAndExtractAsync().ConfigureAwait(false);

                                if (_cancelTokenSource.IsCancellationRequested)
                                {
                                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Run canceled after upgrade.");
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "UpgradeRoblox failed: " + ex);
                            }
                        }

                        try
                        {
                            App.MessageStatus.Prop.Message = "Applying Mods";
                            await ApplyModifications().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            App.MessageStatus.Prop.Message = "Applying Mods Failed";
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "ApplyModifications failed: " + ex);
                        }
                    }

                    try
                    {
                        if (IsStudioLaunch)
                            WindowsRegistry.RegisterStudio();
                        else
                            WindowsRegistry.RegisterPlayer();
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "WindowsRegistry registration failed: " + ex);
                    }

                    bool launched = await TryLaunchRobloxWithInstallRetryAsync().ConfigureAwait(false);
                    if (!launched)
                    {
                        App.MessageStatus.Prop.Message = "Failed to launch Roblox";
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to launch Roblox after installation attempts.");
                    }
                }
                catch (OperationCanceledException)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Bootstrapper run canceled.");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Unhandled exception: " + ex);
                }
                finally
                {
                    if (mutexAcquired)
                    {
                        try { await mutex.ReleaseAsync().ConfigureAwait(false); }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Error releasing mutex: " + ex);
                        }
                    }

                    try { LaunchPage?.CloseBootstrapper(); }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Error closing bootstrapper dialog: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Fatal error in Run method: " + ex);
                try { LaunchPage?.CloseBootstrapper(); } catch { }
            }
        }


        private async Task<bool> TryLaunchRobloxWithInstallRetryAsync()
        {
            const string LOG_IDENT = "Bootstrapper::TryLaunchRoblox";

            try
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Launching Roblox - Step 1: Applying modifications...");
                App.MessageStatus.Prop.Message = "Applying modifications...";
                await ApplyModifications().ConfigureAwait(false);

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Roblox launched successfully on first attempt.");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"First launch attempt failed: {ex.Message}");
                return await TryInstallAndRetryAsync(LOG_IDENT);
            }
        }

        private async Task<bool> TryInstallAndRetryAsync(string logIdent)
        {
            try
            {
                App.Logger.WriteLine(LogLevel.Info, logIdent, "Downloading and installing Roblox...");
                await DownloadAndExtractAsync().ConfigureAwait(false);
                await Task.Delay(15).ConfigureAwait(false);

                App.Logger.WriteLine(LogLevel.Info, logIdent, "Retrying launch - Step 2: Applying modifications...");
                App.MessageStatus.Prop.Message = "Reapplying modifications...";
                await ApplyModifications().ConfigureAwait(false);

                App.Logger.WriteLine(LogLevel.Info, logIdent, "Roblox launched successfully after installation.");
                return true;
            }
            catch (Exception retryEx)
            {
                App.Logger.WriteLine(LogLevel.Info, logIdent, $"Retry after installation failed: {retryEx.Message}");
                return false;
            }
        }


        private static readonly HttpClient _httpClient = new HttpClient();

        private async Task DownloadAndExtractAsync()
        {
            string versionFilePath = Path.Combine(Paths.Versions, "version.txt");
            string currentVersion = "1.0.6";
            string? installedVersion = null;

            if (File.Exists(versionFilePath))
            {
                var content = await File.ReadAllTextAsync(versionFilePath).ConfigureAwait(false);
                var parts = content.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                    installedVersion = parts[1].Trim();
            }

            bool needsInstall = installedVersion == null;
            bool needsUpgrade = !needsInstall && IsVersionLower(installedVersion!, currentVersion);

            if (!needsInstall && !needsUpgrade)
                return;

            LaunchPage?.SetCancelEnabled(false);
            
            // Add timeout to prevent indefinite downloads
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancelTokenSource.Token);
            cts.CancelAfter(TimeSpan.FromMinutes(5));

            try
            {
                // Clean directory code...
                
                string url = "https://www.dropbox.com/scl/fi/pjiqokbch8i31buyhybqq/RobloxV6.zip?rlkey=0ydyefnwfbomwnmlcogjjo01m&st=7wmf1ktn&dl=1";
                string zipPath = Path.Combine(Paths.Versions, "RobloxV6.zip");
                
                App.Logger.WriteLine(LogLevel.Info, "Bootstrapper::DownloadAndExtractAsync", "Starting download");
                
                // Continue with download and extraction...
            }
            catch (OperationCanceledException)
            {
                App.Logger.WriteLine(LogLevel.Info, "Bootstrapper::DownloadAndExtractAsync", "Download canceled or timed out");
                throw;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, "Bootstrapper::DownloadAndExtractAsync", $"Error: {ex.Message}");
                DialogService.ShowMessage($"Error during download or extraction: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsVersionLower(string v1, string v2)
        {
            try
            {
                var v1Parts = v1.Split('.');
                var v2Parts = v2.Split('.');

                int len = Math.Max(v1Parts.Length, v2Parts.Length);
                for (int i = 0; i < len; i++)
                {
                    int part1 = i < v1Parts.Length ? int.Parse(v1Parts[i]) : 0;
                    int part2 = i < v2Parts.Length ? int.Parse(v2Parts[i]) : 0;

                    if (part1 < part2)
                        return true;
                    if (part1 > part2)
                        return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task StartRoblox()
        {
            const string LOG_IDENT = "Bootstrapper::StartRoblox";

            var envVars = new Dictionary<string, string>()
    {
        { "NUMPROC", Environment.ProcessorCount.ToString() },
        { "PROCESSOR_DISABLE_DYNAMIC_CLOCKING", "1" },
        { "THREAD_PRIORITY_POLICY", "6" },
        { "NVIDIA_THREAD_PRIORITY_POLICY", "1" },
        { "ENABLE_EFFICIENT_POWER_MODE", "0" },
        { "FTH_ENABLED", "0" },
        { "DISABLE_TELEMETRY", "1" },
        { "COMPlus_ReadyToRun", "0" },
        { "COMPlus_TC_QuickJitForLoops", "1" },
        { "DOTNET_TC_OptimizeTieredCompilation", "1" },
        { "DOTNET_GCConcurrent", "1" },
        { "DOTNET_GCServer", "1" },
        { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" },
        { "GPU_MAX_ALLOC_PERCENT", "100" },
        { "GPU_MAX_HEAP_SIZE", "100" },
        { "GPU_ENABLE_PRESENT_HISTORY", "0" },
        { "OPENGL_FORCE_DISABLE_VSYNC", "1" },
        { "CUDA_DEVICE_MAX_CONNECTIONS", "32" },
        { "FORCE_STREAMING_MODE", "1" },
        { "FORCE_UNLIMITED_MEMORY", "1" }
    };

            foreach (var kvp in envVars)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
            }

            var perfFlags = new[]
            {
        "--enable-gpu-rasterization",
        "--force-gpu-rasterization",
        "--ignore-gpu-blacklist",
        "--enable-zero-copy",
        "--enable-native-gpu-memory-buffers",
        "--disable-software-rasterizer",
        "--enable-oop-rasterization",
        "--enable-gpu-compositing",
        "--enable-accelerated-2d-canvas",
        "--disable-gpu-vsync",
        "--enable-surface-synchronization",
        "--enable-webgl-draft-extensions",
        "--enable-webgl2-compute-context",
        "--enable-webgpu",
        "--enable-hardware-overlays",
        "--disable-gpu-process-crash-limit",
        "--disable-vulkan-fallback-to-gl-for-tests",
        "--disable-gpu-watchdog",
        "--enable-quic",
        "--enable-threaded-compositing",
        "--enable-highres-timer",
        "--disable-hang-monitor",
        "--disable-breakpad",
        "--disable-renderer-backgrounding",
        "--disable-background-timer-throttling",
        "--disable-backgrounding-occluded-windows",
        "--enable-accelerated-video-decode",
        "--disable-low-end-device-mode",
        "--enable-simple-cache-backend",
        "--disable-accelerated-video-decode",
        "--enable-vulkan",
        "--use-angle=vulkan",
        "--enable-skia-renderer",
        "--enable-viz-display-compositor",
        "--js-flags=--expose-gc --noincremental-marking --max-old-space-size=8192",
        "--disable-sync",
        "--no-pings",
        "--disable-features=TranslateUI",
        "--enable-strict-mixed-context-checking",
        "--js-flags=--turbo-escape",
        "--js-flags=--optimize-for-size",
        "--disable-site-isolation-trials",
        "--enable-tcp-fast-open",
        "--origin-to-force-quic-on=roblox.com:443",
        "--enable-http2",
        "--enable-experimental-quic-protocol",
        "--dns-prefetch",
        "--force-ipv4",
        "--host-resolver-rules=MAP * 127.0.0.1",
        "--disable-translation",
        "--disable-domain-reliability",
        "--renderer-process-limit=1",
        "--disable-bundled-ppapi-flash",
        "--disable-notifications",
        "--disable-popup-blocking",
        "--disable-renderer-accessibility",
        "--disable-default-apps",
        "--disable-component-update",
        "--disable-preconnect",
        "--disable-background-networking",
        "--enable-fast-unload",
        "--disable-permissions-api",
        "--disable-ipc-flooding-protection",
        "--disable-canvas-aa",
        "--disable-2d-canvas-clip-aa",
        "--disable-lcd-text",
        "--disable-touch-adjustment",
        "--disable-usb-keyboard-detect",
        "--no-zygote",
        "--disable-features=TranslateUI,BlinkGenPropertyTrees",
        "--process-per-site",
        "--disable-software-video-decoders",
        "--disable-gpu-sandbox",
        "--disable-renderer-power-throttling",
        "--enable-image-capture",
        "--enable-compositing-mode",
        "--fast-start",
        "--no-default-browser-check",
        "--disable-plugins",
        "--disable-desktop-notifications",
        "--no-sandbox",
        "--disable-logging",
        "--disable-translate",
        "--disable-infobars",
        "--enable-logging",
        "--force-color-profile=srgb",
        "--force-device-scale-factor=1",
        "--disable-web-security",
        "--enable-use-zoom-for-dsf=false",
        "--disable-client-side-phishing-detection",
        "--disable-reading-from-canvas",
        "--disable-audio-output",
        "--disable-audio-service-sandbox",
        "--disable-features=AudioServiceOutOfProcess",
        "--no-first-run",
        "--metrics-recording-only",
        "--safebrowsing-disable-auto-update",
        "--disable-frame-rate-limit"
    };

            _launchCommandLine += " " + string.Join(" ", perfFlags);
            if (_launchMode == LaunchMode.Player && App.Settings.Prop.ForceRobloxLanguage)
            {
                try
                {
                    var match = Regex.Match(_launchCommandLine, @"gameLocale:([a-z_]+)", RegexOptions.CultureInvariant);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        string detectedLocale = match.Groups[1].Value;
                        _launchCommandLine = Regex.Replace(
                            _launchCommandLine,
                            @"robloxLocale:[a-z_]+",
                            $"robloxLocale:{detectedLocale}",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Locale patch failed: {ex.Message}");
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = AppData.ExecutablePath,
                Arguments = _launchCommandLine,
                WorkingDirectory = AppData.Directory,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            if (_launchMode == LaunchMode.Player && ShouldRunAsAdmin())
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            try
            {
                await Task.Delay(App.Settings.Prop.RobloxStartWaitTime);

                using var process = Process.Start(startInfo)!;
                try
                {
                    if (Enum.TryParse(App.Settings.Prop.RobloxPriority, out ProcessPriorityClass priority))
                    {
                        process.PriorityClass = priority;
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Set Roblox process priority to {priority}.");
                    }
                    else
                    {
                        process.PriorityClass = ProcessPriorityClass.RealTime;
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Invalid priority string. Defaulted to RealTime.");
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Warning, LOG_IDENT, $"Failed to set process priority: {ex.Message}");
                }

                _appPid = process.Id;
                await Task.Delay(800);

                if (App.Settings.Prop.DebugLog)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "DebugLog enabled — keeping app running.");
                }
                if (App.Settings.Prop.KeepPlexityOpen)
                {
                    App.MessageStatus.Prop.Message = "Roblox Started";
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "KeepPlexityOpen enabled — keeping app running.");
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Error, LOG_IDENT, $"Launch failed: {ex.Message}");
                try
                {
                    if (File.Exists(AppData.ExecutablePath))
                    {
                        File.Delete(AppData.ExecutablePath);
                    }
                }
                catch (Exception deleteEx)
                {
                    App.Logger.WriteLine(LogLevel.Warning, LOG_IDENT, $"Failed to delete executable: {deleteEx.Message}");
                }
                throw;
            }
        }


        private bool ShouldRunAsAdmin()
        {
            const string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
            string exePath = AppData.ExecutablePath;

            foreach (var root in WindowsRegistry.Roots)
            {
                using var key = root.OpenSubKey(registryPath);
                if (key == null)
                    continue;

                var value = key.GetValue(exePath) as string;
                if (!string.IsNullOrEmpty(value) &&
                    value.Contains("RUNASADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        public void Cancel()
        {
            const string LOG_IDENT = "Bootstrapper::Cancel";

            if (_cancelTokenSource.IsCancellationRequested)
                return;

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Cancelling launch...");

            _cancelTokenSource.Cancel();

            if (LaunchPage is not null)
                LaunchPage.CancelEnabled = false;

            if (_isInstalling)
            {
                try
                {
                    // clean up install
                    if (Directory.Exists(_latestVersionDirectory))
                        Directory.Delete(_latestVersionDirectory, true);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Could not fully clean up installation!");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
            else if (_appPid != 0)
            {
                try
                {
                    using var process = Process.GetProcessById(_appPid);
                    process.Kill();
                }
                catch (Exception) { }
            }

            LaunchPage?.CloseBootstrapper();

            App.Terminate();
        }
        #endregion

        #region App Install
        private async Task<bool> CheckForUpdates()
        {
            const string LOG_IDENT = "Bootstrapper::CheckForUpdates";

            // Ensure no other instance is running
            if (Process.GetProcessesByName(App.ProjectName).Length > 1)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"More than one Plexity instance running, aborting update check");
                return false;
            }

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Checking for updates...");

#if !DEBUG_UPDATER
            var releaseInfo = await App.GetLatestRelease();

            if (releaseInfo is null)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to fetch release information.");
                return false;
            }

            // Strip leading 'V' or 'v' from versions before comparing
            string currentVersion = App.Version.TrimStart('V', 'v');
            string latestVersion = releaseInfo.TagName.TrimStart('V', 'v');

            var versionComparison = Utilities.CompareVersions(currentVersion, latestVersion);

            if (LaunchPage is not null)
                LaunchPage.CancelEnabled = false;

            string version = releaseInfo.TagName;


#else
    string version = App.Version;
#endif



            try
            {
#if DEBUG_UPDATER
        string downloadLocation = Path.Combine(Paths.TempUpdates, "Plexity.exe");

        Directory.CreateDirectory(Paths.TempUpdates);

        File.Copy(Paths.Process, downloadLocation, true);
#else
                var asset = releaseInfo.Assets?.FirstOrDefault();
                if (asset is null)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "No assets found in the release information.");
                    return false;
                }

                string downloadLocation = Path.Combine(Paths.TempUpdates, asset.Name);

                Directory.CreateDirectory(Paths.TempUpdates);

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Downloading {releaseInfo.TagName}...");

                if (!File.Exists(downloadLocation))
                {
                    var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to download update: {response.StatusCode}");
                        return false;
                    }

                    await using var fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write);
                    await response.Content.CopyToAsync(fileStream);
                }
#endif

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Starting {version}...");

                ProcessStartInfo startInfo = new()
                {
                    FileName = downloadLocation,
                };

                startInfo.ArgumentList.Add("-upgrade");

                foreach (string arg in App.LaunchSettings.Args)
                    startInfo.ArgumentList.Add(arg);

                if (_launchMode == LaunchMode.Player && !startInfo.ArgumentList.Contains("-player"))
                    startInfo.ArgumentList.Add("-player");
                else if (_launchMode == LaunchMode.Studio && !startInfo.ArgumentList.Contains("-studio"))
                    startInfo.ArgumentList.Add("-studio");

                App.Settings.Save();

                new InterProcessLock("AutoUpdater");

                Process.Start(startInfo);

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "An exception occurred when running the auto-updater");
                App.Logger.WriteException(LOG_IDENT, ex);

                Utilities.ShellExecute(App.ProjectDownloadLink);
            }

            return false;
        }
        #endregion

        #region Roblox Install
        private void MigrateCompatibilityFlags()
        {
            const string LOG_IDENT = "Bootstrapper::MigrateCompatibilityFlags";

            string oldClientLocation = Path.Combine(Paths.Versions, AppData.State.VersionGuid, AppData.ExecutableName);
            string newClientLocation = Path.Combine(_latestVersionDirectory, AppData.ExecutableName);

            // move old compatibility flags for the old location
            using RegistryKey appFlagsKey = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers");
            string? appFlags = appFlagsKey.GetValue(oldClientLocation) as string;

            if (appFlags is not null)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Migrating app compatibility flags from {oldClientLocation} to {newClientLocation}...");
                appFlagsKey.SetValueSafe(newClientLocation, appFlags);
                appFlagsKey.DeleteValueSafe(oldClientLocation);
            }
        }

        private async Task ApplyModifications()
        {
            const string LOG_IDENT = "Bootstrapper::ApplyModifications";
            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Checking file mods...");

            string manifestPath = Path.Combine(Paths.Base, "ModManifest.txt");
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }

            List<string> modFolderFiles = new();
            Directory.CreateDirectory(Paths.Mods);

            string modFontsFolder = Path.Combine(Paths.Mods, "content\\fonts");
            string modFontFamiliesFolder = Path.Combine(modFontsFolder, "families");
            string oldFontFamiliesFolder = Path.Combine(modFontsFolder, "Backup_families");

            string modCursorFolder = Path.Combine(Paths.Mods, "content\\textures\\Cursors\\KeyboardMouse");
            string oldCursorFolder = Path.Combine(modCursorFolder, "Backup_cursors");
            string targetCursorFolder = Path.Combine(Paths.Versions, "content\\textures\\Cursors\\KeyboardMouse");

            Directory.CreateDirectory(modCursorFolder);
            Directory.CreateDirectory(oldCursorFolder);
            Directory.CreateDirectory(targetCursorFolder);

            if (File.Exists(Paths.CustomFont))
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Begin font check");

                Directory.CreateDirectory(modFontsFolder);
                Directory.CreateDirectory(modFontFamiliesFolder);
                Directory.CreateDirectory(oldFontFamiliesFolder);

                const string assetPath = "rbxasset://fonts/CustomFont.ttf";

                string contentFolder = Path.Combine(Paths.Versions, "content");
                string fontsFolder = Path.Combine(contentFolder, "fonts");
                string familiesFolder = Path.Combine(fontsFolder, "families");

                Directory.CreateDirectory(contentFolder);
                Directory.CreateDirectory(fontsFolder);
                Directory.CreateDirectory(familiesFolder);

                foreach (string jsonFilePath in Directory.GetFiles(familiesFolder, "*.json"))
                {
                    string jsonFilename = Path.GetFileName(jsonFilePath);
                    string modFilePath = Path.Combine(modFontFamiliesFolder, jsonFilename);
                    string oldFilePath = Path.Combine(oldFontFamiliesFolder, jsonFilename);

                    if (!File.Exists(oldFilePath))
                    {
                        try
                        {
                            File.Copy(jsonFilePath, oldFilePath);
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Backed up original font {jsonFilename} to Backup_families");
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to backup_cursors original font {jsonFilename}: {ex.Message}");
                        }
                    }

                    if (File.Exists(modFilePath))
                        continue;

                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Setting font for {jsonFilename}");

                    FontFamily? fontFamilyData;
                    try
                    {
                        fontFamilyData = JsonSerializer.Deserialize<FontFamily>(File.ReadAllText(jsonFilePath));
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to deserialize {jsonFilename}: {ex.Message}");
                        continue;
                    }

                    if (fontFamilyData == null)
                        continue;

                    bool shouldWrite = false;
                    foreach (var fontFace in fontFamilyData.Faces)
                    {
                        if (fontFace.AssetId != assetPath)
                        {
                            fontFace.AssetId = assetPath;
                            shouldWrite = true;
                        }
                    }

                    if (shouldWrite)
                    {
                        try
                        {
                            string serialized = JsonSerializer.Serialize(fontFamilyData, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(modFilePath, serialized);
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Modified font JSON saved for {jsonFilename}");
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to write modified font JSON: {ex.Message}");
                        }
                    }
                }

                if (!Directory.Exists(modFontFamiliesFolder) || Directory.GetFiles(modFontFamiliesFolder, "*.json").Length == 0)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "No modified fonts found, restoring from backup_cursors.");
                    Directory.CreateDirectory(modFontFamiliesFolder);

                    foreach (var backupFile in Directory.GetFiles(oldFontFamiliesFolder, "*.json"))
                    {
                        string destFile = Path.Combine(modFontFamiliesFolder, Path.GetFileName(backupFile));
                        try
                        {
                            File.Copy(backupFile, destFile, overwrite: true);
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Restored font {Path.GetFileName(backupFile)} from backup_cursors.");
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to restore font {Path.GetFileName(backupFile)}: {ex.Message}");
                        }
                    }
                }

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "End font check");
            }
            else
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "No custom font found, restoring original font config");

                if (Directory.Exists(modFontFamiliesFolder))
                {
                    try
                    {
                        Directory.Delete(modFontFamiliesFolder, recursive: true);
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Deleted 'families' folder");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to delete 'families' folder: {ex.Message}");
                    }
                }

                if (Directory.Exists(oldFontFamiliesFolder))
                {
                    try
                    {
                        Directory.Move(oldFontFamiliesFolder, modFontFamiliesFolder);
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Restored Backup_families to families.");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to restore fonts: {ex.Message}");
                    }
                }
            }

            // Cursor mod logic
            foreach (string cursorFile in Directory.GetFiles(targetCursorFolder, "*.*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(cursorFile);
                string backupPath = Path.Combine(oldCursorFolder, fileName);
                string modCursorPath = Path.Combine(modCursorFolder, fileName);

                if (!File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(cursorFile, backupPath);
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Backed up original cursor: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to backup cursor {fileName}: {ex.Message}");
                    }
                }

                if (File.Exists(modCursorPath))
                {
                    try
                    {
                        File.Copy(modCursorPath, cursorFile, overwrite: true);
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Applied custom cursor: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to apply cursor {fileName}: {ex.Message}");
                    }
                }
            }

            if (Directory.GetFiles(modCursorFolder, "*.*").Length == 0 && Directory.GetFiles(oldCursorFolder, "*.*").Length > 0)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "No custom cursors found, restoring from backup_cursors...");

                foreach (string backupFile in Directory.GetFiles(oldCursorFolder, "*.*"))
                {
                    string fileName = Path.GetFileName(backupFile);
                    string restorePath = Path.Combine(targetCursorFolder, fileName);

                    try
                    {
                        File.Copy(backupFile, restorePath, overwrite: true);
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Restored cursor: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to restore cursor {fileName}: {ex.Message}");
                    }
                }
            }

            // Apply other mod files
            var appliedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.GetFiles(Paths.Mods, "*.*", SearchOption.AllDirectories))
            {
                if (_cancelTokenSource.IsCancellationRequested)
                    return;

                string relativeFile = file.Substring(Paths.Mods.Length + 1);

                if (relativeFile.Equals("README.txt", StringComparison.OrdinalIgnoreCase) ||
                    relativeFile.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) ||
                    (!App.Settings.Prop.UseClientAppSettings &&
                     relativeFile.Equals("ClientSettings\\ClientAppSettings.json", StringComparison.OrdinalIgnoreCase)))
                {
                    File.Delete(file);
                    continue;
                }

                if (appliedFiles.Contains(relativeFile))
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Skipping duplicate mod: {relativeFile}");
                    continue;
                }

                appliedFiles.Add(relativeFile);
                modFolderFiles.Add(relativeFile);

                string destinationPath = Path.Combine(Paths.Versions, relativeFile);
                string? destDir = Path.GetDirectoryName(destinationPath);

                if (destDir != null)
                {
                    Directory.CreateDirectory(destDir);
                }

                try
                {
                    Filesystem.AssertReadOnly(destinationPath);
                    File.Copy(file, destinationPath, overwrite: true);
                    Filesystem.AssertReadOnly(destinationPath);
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Applied mod: {relativeFile}");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to apply mod {relativeFile}: {ex.Message}");
                }
            }

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Finished applying file mods");
            await Task.Delay(205);
            App.State.Prop.ModManifest = modFolderFiles;
            App.State.Save();

            App.MessageStatus.Prop.Message = "Starting Roblox";
            await StartRoblox();
        }

        #endregion

        #region IDisposable Implementation
        private bool _disposed = false;
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _cancelTokenSource?.Dispose();
                    
                    // Check if AsyncMutex is already released and cleanup properly
                    if (_mutex != null)
                    {
                        _mutex.ReleaseAsync().GetAwaiter().GetResult();
                        _mutex = null;
                    }
                    
                    // Remove this line - can't set readonly field to null
                    // _fastZipEvents = null;
                }
                
                // Free unmanaged resources
                
                _disposed = true;
            }
        }
    
        ~Bootstrapper()
        {
            Dispose(false);
        }
        #endregion
    }
}