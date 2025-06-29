using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using Plexity.UI;

namespace Plexity
{
    static class Utilities
    {
        public static void ShellExecute(string website)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = website,
                    UseShellExecute = true
                });
            }
            catch (Win32Exception ex)
            {
                const int CO_E_APPNOTFOUND = unchecked((int)0x80040154); // or use specific constant

                if (ex.NativeErrorCode != CO_E_APPNOTFOUND)
                    throw;

                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"shell32,OpenAs_RunDLL {website}",
                    UseShellExecute = true
                });
            }
        }

        public static Version? GetVersionFromString(string? version)
        {
            var logger = new Logger();

            if (string.IsNullOrWhiteSpace(version))
                return null;

            version = version.Trim();

            if (version.StartsWith('v') || version.StartsWith('V'))
                version = version[1..];

            int plusIndex = version.IndexOf('+');
            if (plusIndex != -1)
                version = version[..plusIndex];

            int dashIndex = version.IndexOf('-');
            if (dashIndex != -1)
                version = version[..dashIndex];

            version = version.Trim();

            try
            {
#if NET7_0_OR_GREATER
                if (Version.TryParse(version, out var parsedVersion))
                    return parsedVersion;
                else
                    throw new ArgumentException("Invalid version format");
#else
                return new Version(version);
#endif
            }
            catch (Exception ex)
            {
                logger.WriteLine(LogLevel.Info, "App::GetVersionFromString", $"Invalid version string '{version}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses the input version string and logs if it fails
        /// </summary>
        public static Version? ParseVersionSafe(string versionStr)
        {
            const string LOG_IDENT = "Utilities::ParseVersionSafe";

            if (!Version.TryParse(versionStr, out Version? version))
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to convert '{versionStr}' to a valid Version type.");
                return null;
            }

            return version;
        }

        public static Process[] GetProcessesSafe()
        {
            const string LOG_IDENT = "Utilities::GetProcessesSafe";

            try
            {
                return Process.GetProcesses();
            }
            catch (ArithmeticException ex) // thanks Microsoft
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Unable to fetch processes!");
                App.Logger.WriteException(LOG_IDENT, ex);
                return Array.Empty<Process>(); // could retry if needed
            }
        }



        public static void KillBackgroundUpdater()
        {
            using EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.AutoReset, "Plexity-BackgroundUpdaterKillEvent");
            handle.Set();
        }

        public static void RemoveTeleportFix()
        {
            const string LOG_IDENT = "Utilities::RemoveTeleportFix";

            string user = Environment.UserDomainName + "\\" + Environment.UserName;

            try
            {
                var fileInfo = new FileInfo(App.RobloxCookiesFilePath);
                var fileSecurity = fileInfo.GetAccessControl();

                fileSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Read, AccessControlType.Deny));
                fileSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Write, AccessControlType.Allow));

                fileInfo.SetAccessControl(fileSecurity);

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Successfully removed teleport fix.");
            }
            catch (Exception)
            {
                return;
            }
        }

        internal static object CompareVersions(string version1, string version2)
        {
            throw new NotImplementedException();
        }
    }
}
