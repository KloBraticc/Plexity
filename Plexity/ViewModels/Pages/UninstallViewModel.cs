using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic;
using Plexity.Models;
using Wpf.Ui.Abstractions.Controls;
using Plexity.Resources;
using Plexity;

namespace Plexity.ViewModels.Pages
{
    public class UninstallViewModel
    {
        public string Text => string.Format(
            "For help, visit",
            "https://github.com/BloxstrapLabs/Bloxstrap/wiki/Roblox-crashes-or-does-not-launch",
            Paths.Base
        );

        public bool KeepData { get; set; } = true;

        public ICommand ConfirmUninstallCommand => new RelayCommand(ConfirmUninstall);

        public event EventHandler? ConfirmUninstallRequest;

        private void ConfirmUninstall() => ConfirmUninstallRequest?.Invoke(this, new EventArgs());
    }
}

