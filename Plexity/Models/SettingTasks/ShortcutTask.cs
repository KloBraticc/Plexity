using System.IO;
using Plexity.Models.SettingTasks.Base;
using IWshRuntimeLibrary;

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

            // Create shortcut
            var shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(_shortcutPath);
            shortcut.TargetPath = _targetPath;
            shortcut.Arguments = _exeFlags;
            shortcut.WorkingDirectory = Path.GetDirectoryName(_targetPath);
            shortcut.Save();
        }
    }
}
