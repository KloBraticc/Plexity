using Plexity.UI.ViewModels.Bootstrapper;

namespace Plexity.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string Text => string.Format(
    "For help, visit {0}",
    "https://github.com/BloxstrapLabs/Bloxstrap/wiki/Roblox-crashes-or-does-not-launch"
);

        public bool CanContinue { get; set; } = false;
    }
}
