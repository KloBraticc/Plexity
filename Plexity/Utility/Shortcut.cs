using System;
using System.IO;
using Plexity.Enums;

namespace Plexity.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        public static void Create(string exePath, string exeArgs, string lnkPath)
        {
            const string LOG_IDENT = "Shortcut::Create";

            if (string.IsNullOrWhiteSpace(exePath))
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Executable path is null or empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lnkPath))
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Shortcut path is null or empty.");
                return;
            }

            if (System.IO.File.Exists(lnkPath))
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Shortcut already exists at {lnkPath}");
                return;
            }

            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(lnkPath);

                shortcut.TargetPath = exePath;
                shortcut.Arguments = exeArgs ?? string.Empty;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.IconLocation = exePath;
                shortcut.Save();

                if (_loadStatus != GenericTriState.Successful)
                    _loadStatus = GenericTriState.Successful;

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Shortcut created successfully at {lnkPath}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to create a shortcut for {lnkPath}!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (_loadStatus != GenericTriState.Failed)
                    _loadStatus = GenericTriState.Failed;
            }
        }
    }
}
