using Plexity.UI.ViewModels.Bootstrapper;

namespace Plexity.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string MainText => String.Format(
            "Wecome",
            "Thank you for downloading Plexity. This installation process will be quick and simple, and you will be able to configure any of Plexity's settings after installation."
        );

        public bool CanContinue { get; set; } = false;
    }
}
