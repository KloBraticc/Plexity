using Plexity.Models.SettingTasks;
using Plexity.UI.ViewModels.Bootstrapper;

namespace Plexity.ViewModels.Pages
{
    public class ShortcutsViewModel : NotifyPropertyChangedViewModel
    {
        public bool IsStudioOptionVisible => App.IsStudioVisible;

        public ShortcutTask DesktopIconTask { get; } = new(
            "Desktop",
            Paths.Desktop,
            $"{App.ProjectName}.lnk",
            Paths.Application
        );

        public ShortcutTask StartMenuIconTask { get; } = new(
            "StartMenu",
            Paths.WindowsStartMenu,
            $"{App.ProjectName}.lnk",
            Paths.Application
        );

        public ShortcutTask PlayerIconTask { get; } = new(
            "RobloxPlayer",
            Paths.Desktop,
            $"LaunchRoblox.lnk",
            Paths.Application,
            "-player"
        );

        public ShortcutTask StudioIconTask { get; } = new(
            "RobloxStudio",
            Paths.Desktop,
            $"LaunchStudio.lnk",
            Paths.Application,
            "-studio"
        );
    }
}
