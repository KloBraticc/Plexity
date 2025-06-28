using System.IO;
using System.Diagnostics;
using Plexity.Models.SettingTasks.Base;

namespace Plexity.Models.SettingTasks
{
    public class ShortcutTask : BoolBaseTask
    {
        private readonly string _shortcutPath;
        private readonly string _exeFlags;
        private readonly string _targetPath;

        public ShortcutTask(string name, string lnkFolder, string lnkName, string targetPath, string exeFlags = "")
            : base("Shortcut", name)
        {
            _shortcutPath = Path.Combine(lnkFolder, lnkName + ".lnk");
            _exeFlags = exeFlags;
            _targetPath = targetPath;
        }

        public override void Execute()
        {
            // Delete existing shortcut if it exists
            if (System.IO.File.Exists(_shortcutPath))
            {
                System.IO.File.Delete(_shortcutPath);
            }

            // Create shortcut using PowerShell (compatible with .NET 8)
            var workingDirectory = Path.GetDirectoryName(_targetPath) ?? "";
            var script = $@"
                $WshShell = New-Object -comObject WScript.Shell
                $Shortcut = $WshShell.CreateShortcut('{_shortcutPath}')
                $Shortcut.TargetPath = '{_targetPath}'
                $Shortcut.Arguments = '{_exeFlags}'
                $Shortcut.WorkingDirectory = '{workingDirectory}'
                $Shortcut.IconLocation = '{_targetPath},0'
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
        }
    }
}