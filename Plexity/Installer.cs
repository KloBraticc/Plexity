using System.Windows;
using Plexity;
using Microsoft.Win32;
using System.IO;
using Plexity.Utility;
using Plexity.Extensions;
using Plexity.Enums;
using System.Diagnostics;

namespace Plexity
{
    internal class Installer
    {
        /// <summary>
        /// Should this version automatically open the release notes page?
        /// Recommended for major updates only.
        /// </summary>
        private const bool OpenReleaseNotes = false;

        private static string DesktopShortcut => Path.Combine(Paths.Desktop, $"{App.ProjectName}.lnk");

        private static string StartMenuShortcut => Path.Combine(Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");

        public string InstallLocation = Path.Combine(Paths.LocalAppData, App.ProjectName);

        public bool ExistingDataPresent => File.Exists(Path.Combine(InstallLocation, "Settings.json"));

        public bool CreateDesktopShortcuts = true;

        public bool CreateStartMenuShortcuts = true;

        public bool EnableAnalytics = true;

        public bool PlexityRPCReal = true;

        public bool IsImplicitInstall = false;

        public string InstallLocationError { get; set; } = "";

        public void DoInstall()
        {
            const string LOG_IDENT = "Installer::DoInstall";
            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Beginning installation");
            Directory.CreateDirectory(InstallLocation);

            Paths.Initialize(InstallLocation);

            if (!IsImplicitInstall)
            {
                Filesystem.AssertReadOnly(Paths.Application);

                try
                {
                    File.Copy(Paths.Process, Paths.Application, true);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Could not overwrite executable");
                    App.Logger.WriteException(LOG_IDENT, ex);
                    App.Terminate();
                }
            }

            using (var uninstallKey = Registry.CurrentUser.CreateSubKey(App.UninstallKey))
            {
                uninstallKey.SetValueSafe("DisplayIcon", $"{Paths.Application},0");
                uninstallKey.SetValueSafe("DisplayName", App.ProjectName);

                uninstallKey.SetValueSafe("DisplayVersion", App.Version);

                if (uninstallKey.GetValue("InstallDate") is null)
                    uninstallKey.SetValueSafe("InstallDate", DateTime.Now.ToString("yyyyMMdd"));

                uninstallKey.SetValueSafe("InstallLocation", Paths.Base);
                uninstallKey.SetValueSafe("NoRepair", 1);
                uninstallKey.SetValueSafe("Publisher", App.ProjectOwner);
                uninstallKey.SetValueSafe("ModifyPath", $"\"{Paths.Application}\" -settings");
                uninstallKey.SetValueSafe("QuietUninstallString", $"\"{Paths.Application}\" -uninstall -quiet");
                uninstallKey.SetValueSafe("UninstallString", $"\"{Paths.Application}\" -uninstall");
                uninstallKey.SetValueSafe("HelpLink", App.ProjectHelpLink);
                uninstallKey.SetValueSafe("URLInfoAbout", App.ProjectSupportLink);
                uninstallKey.SetValueSafe("URLUpdateInfo", App.ProjectDownloadLink);
            }

            WindowsRegistry.RegisterApis();

            // only register player, for the scenario where the user installs Plexity, closes it,
            // and then launches from the website expecting it to work
            // studio can be implicitly registered when it's first launched manually
            WindowsRegistry.RegisterPlayer();

            if (CreateDesktopShortcuts)
                Shortcut.Create(Paths.Application, "", DesktopShortcut);

            if (CreateStartMenuShortcuts)
                Shortcut.Create(Paths.Application, "", StartMenuShortcut);

            // existing configuration persisting from an earlier install
            App.Settings.Load(false);
            App.State.Load(false);
            App.FastFlags.Load(false);

            App.Settings.Prop.EnableAnalytics = EnableAnalytics;

            App.Settings.Prop.PlexityRPCReal = PlexityRPCReal;

            if (App.IsStudioVisible)
                WindowsRegistry.RegisterStudio();

            App.Settings.Save();

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Installation finished");

        }

        private bool ValidateLocation()
        {
            // prevent from installing to the root of a drive
            if (InstallLocation.Length <= 3)
                return false;

            // unc path, just to be safe
            if (InstallLocation.StartsWith("\\\\"))
                return false;

            if (InstallLocation.StartsWith(Path.GetTempPath(), StringComparison.InvariantCultureIgnoreCase)
                || InstallLocation.Contains("\\Temp\\", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // prevent from installing to a onedrive folder
            if (InstallLocation.Contains("OneDrive", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // prevent from installing to an essential user profile folder (e.g. Documents, Downloads, Contacts idk)
            if (String.Compare(Directory.GetParent(InstallLocation)?.FullName, Paths.UserProfile, StringComparison.InvariantCultureIgnoreCase) == 0)
                return false;

            // prevent from installing into the program files folder
            if (InstallLocation.Contains("Program Files"))
                return false;

            return true;
        }

        public bool CheckInstallLocation()
        {
            if (string.IsNullOrEmpty(InstallLocation))
            {
                InstallLocationError = "Install Location Not Set";
            }
            else if (!ValidateLocation())
            {
                InstallLocationError = "Cant Install Plexity!";
            }
            else
            {
                if (!IsImplicitInstall
                    && !InstallLocation.EndsWith(App.ProjectName, StringComparison.InvariantCultureIgnoreCase)
                    && Directory.Exists(InstallLocation)
                    && Directory.EnumerateFileSystemEntries(InstallLocation).Any())
                {
                    string suggestedChange = Path.Combine(InstallLocation, App.ProjectName);
                        InstallLocation = suggestedChange;
                        return false;
                }

                try
                {
                    // check if we can write to the directory (a bit hacky but eh)
                    string testFile = Path.Combine(InstallLocation, $"{App.ProjectName}WriteTest.txt");

                    Directory.CreateDirectory(InstallLocation);
                    File.WriteAllText(testFile, "");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    InstallLocationError = "No Write Perms ERROR";
                }
                catch (Exception ex)
                {
                    InstallLocationError = ex.Message;
                }
            }

            return String.IsNullOrEmpty(InstallLocationError);
        }

        public static void DoUninstall(bool keepData)
        {
            const string LOG_IDENT = "Installer::DoUninstall";

            var processes = new List<Process>();

            if (!string.IsNullOrEmpty(App.State.Prop.Player.VersionGuid))
                processes.AddRange(Process.GetProcessesByName(App.RobloxPlayerAppName));

            if (App.IsStudioVisible)
                processes.AddRange(Process.GetProcessesByName(App.RobloxStudioAppName));

            // prompt to shutdown Roblox if it's currently running
            if (processes.Any())
            {
                try
                {
                    App.Terminate();

                    foreach (var process in processes)
                    {
                        process.Kill();
                        process.Close();
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to close process! {ex}");
                }

                return;
            }
        
        string robloxFolder = Path.Combine(Paths.LocalAppData, "Roblox");
            bool playerStillInstalled = true;
            bool studioStillInstalled = true;

            // check if stock bootstrapper is still installed
            using var playerKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-player");
            var playerFolder = playerKey?.GetValue("InstallLocation");

            if (playerKey is null || playerFolder is not string)
            {
                playerStillInstalled = false;

                WindowsRegistry.Unregister("roblox");
                WindowsRegistry.Unregister("roblox-player");
            }
            else
            {
                string playerPath = Path.Combine((string)playerFolder, "RobloxPlayerBeta.exe");

                WindowsRegistry.RegisterPlayer(playerPath, "%1");
            }

            using var studioKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-studio");
            var studioFolder = studioKey?.GetValue("InstallLocation");

            if (studioKey is null || studioFolder is not string)
            {
                studioStillInstalled = false;

                WindowsRegistry.Unregister("roblox-studio");
                WindowsRegistry.Unregister("roblox-studio-auth");

                WindowsRegistry.Unregister("Roblox.Place");
                WindowsRegistry.Unregister(".rbxl");
                WindowsRegistry.Unregister(".rbxlx");
            }
            else
            {
                string studioPath = Path.Combine((string)studioFolder, "RobloxStudioBeta.exe");
                string studioLauncherPath = Path.Combine((string)studioFolder, "RobloxStudioLauncherBeta.exe");

                WindowsRegistry.RegisterStudioProtocol(studioPath, "%1");
                WindowsRegistry.RegisterStudioFileClass(studioPath, "-ide \"%1\"");
            }

            Registry.CurrentUser.DeleteSubKey(App.ApisKey);

            var cleanupSequence = new List<Action>
            {
                () =>
                {
                    foreach (var file in Directory.GetFiles(Paths.Desktop).Where(x => x.EndsWith("lnk")))
                    {
                            File.Delete(file);
                    }
                },

                () => File.Delete(StartMenuShortcut),

                () => Directory.Delete(Paths.Versions, true),

                () => Directory.Delete(Paths.Downloads, true),

                () => File.Delete(App.State.FileLocation),

                () =>
                {
                if (Paths.Roblox == Path.Combine(Paths.Base, "Roblox")) // checking if roblox is installed in base directory
                    Directory.Delete(Paths.Roblox, true);               // made that to prevent accidental removals of different builds
                }
            };


            if (!keepData)
            {
                cleanupSequence.AddRange(new List<Action>
                {
                    () => Directory.Delete(Paths.Mods, true),
                    () => Directory.Delete(Paths.Logs, true),

                    () => File.Delete(App.Settings.FileLocation)
                });
            }

            bool deleteFolder = Directory.GetFiles(Paths.Base).Length <= 3;

            if (deleteFolder)
                cleanupSequence.Add(() => Directory.Delete(Paths.Base, true));

            if (!playerStillInstalled && !studioStillInstalled && Directory.Exists(robloxFolder))
                cleanupSequence.Add(() => Directory.Delete(robloxFolder, true));

            cleanupSequence.Add(() => Registry.CurrentUser.DeleteSubKey(App.UninstallKey));

            foreach (var process in cleanupSequence)
            {
                try
                {
                    process();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Encountered exception when running cleanup sequence (#{cleanupSequence.IndexOf(process)})");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            if (Directory.Exists(Paths.Base))
            {
                // this is definitely one of the workaround hacks of all time

                string deleteCommand;

                if (deleteFolder)
                    deleteCommand = $"del /Q \"{Paths.Base}\\*\" && rmdir \"{Paths.Base}\"";
                else
                    deleteCommand = $"del /Q \"{Paths.Application}\"";

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout 5 && {deleteCommand}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
        }

        public static void HandleUpgrade()
        {
            const string LOG_IDENT = "Installer::HandleUpgrade";

            if (!File.Exists(Paths.Application) || Paths.Process == Paths.Application)
                return;

            // 2.0.0 downloads updates to <BaseFolder>/Updates so lol
            bool isAutoUpgrade = App.LaunchSettings.UpgradeFlag.Active
                || Paths.Process.StartsWith(Path.Combine(Paths.Base, "Updates"))
                || Paths.Process.StartsWith(Path.Combine(Paths.LocalAppData, "Temp"))
                || Paths.Process.StartsWith(Paths.TempUpdates);

            var existingVer = FileVersionInfo.GetVersionInfo(Paths.Application).ProductVersion;
            var currentVer = FileVersionInfo.GetVersionInfo(Paths.Process).ProductVersion;

            if (MD5Hash.FromFile(Paths.Process) == MD5Hash.FromFile(Paths.Application))
                return;
            // silently upgrade version if the command line flag is set or if we're launching from an auto update
            if (!isAutoUpgrade)
            {
                    return;
            }

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Doing upgrade");

            Filesystem.AssertReadOnly(Paths.Application);

            using (var ipl = new InterProcessLock("AutoUpdater", TimeSpan.FromSeconds(5)))
            {
                if (!ipl.IsAcquired)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to update! (Could not obtain singleton mutex)");
                    return;
                }
            }

            // prior to 1.0.3.6, auto-updating was handled with this... bruteforce method
            // now it's handled with the system mutex you see above, but we need to keep this logic for <1.0.3.6 versions
            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    File.Copy(Paths.Process, Paths.Application, true);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 1)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Waiting for write permissions to update version");
                    }
                    else if (i == 10)
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to update! (Could not get write permissions after 10 tries/5 seconds)");
                        App.Logger.WriteException(LOG_IDENT, ex);
                        return;
                    }

                    Thread.Sleep(500);
                }
            }

            using (var uninstallKey = Registry.CurrentUser.CreateSubKey(App.UninstallKey))
            {
                uninstallKey.SetValueSafe("DisplayVersion", App.Version);

                uninstallKey.SetValueSafe("Publisher", App.ProjectOwner);
                uninstallKey.SetValueSafe("HelpLink", App.ProjectHelpLink);
                uninstallKey.SetValueSafe("URLInfoAbout", App.ProjectSupportLink);
                uninstallKey.SetValueSafe("URLUpdateInfo", App.ProjectDownloadLink);
            }


                    string oldDesktopPath = Path.Combine(Paths.Desktop, "Play Roblox.lnk");
                    string oldStartPath = Path.Combine(Paths.WindowsStartMenu, "Plexity");

                    if (File.Exists(oldDesktopPath))
                        File.Move(oldDesktopPath, DesktopShortcut, true);

                    if (Directory.Exists(oldStartPath))
                    {
                        try
                        {
                            Directory.Delete(oldStartPath, true);
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteException(LOG_IDENT, ex);
                        }

                        Shortcut.Create(Paths.Application, "", StartMenuShortcut);
                    }

                    Registry.CurrentUser.DeleteSubKeyTree("Software\\Plexity", false);

                    WindowsRegistry.RegisterPlayer();

                    App.FastFlags.SetValue("FFlagDisableNewIGMinDUA", null);
                    App.FastFlags.SetValue("FFlagFixGraphicsQuality", null);
        }

    }

}
