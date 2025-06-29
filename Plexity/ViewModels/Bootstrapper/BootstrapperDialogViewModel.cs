using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

using CommunityToolkit.Mvvm.Input;
using Plexity.Extensions;
using Plexity.Views.Pages;

namespace Plexity.UI.ViewModels.Bootstrapper
{
    public class BootstrapperDialogViewModel : NotifyPropertyChangedViewModel
    {
        private readonly LaunchPage _dialog;

        public ICommand CancelInstallCommand => new RelayCommand(CancelInstall);
        public string Title => App.Settings.Prop.BootstrapperTitle;

        public string Message { get; set; } = "Please wait..";
        public bool ProgressIndeterminate { get; set; } = true;
        public int ProgressMaximum { get; set; } = 0;
        public int ProgressValue { get; set; } = 0;

        public TaskbarItemProgressState TaskbarProgressState { get; set; } = TaskbarItemProgressState.Indeterminate;
        public double TaskbarProgressValue { get; set; } = 0;

        public bool CancelEnabled { get; set; } = false;
        public Visibility CancelButtonVisibility => CancelEnabled ? Visibility.Visible : Visibility.Collapsed;

        [Obsolete("Do not use this! This is for the designer only.", true)]
        public BootstrapperDialogViewModel()
        {
            _dialog = null!;
        }

        public BootstrapperDialogViewModel(LaunchPage dialog)
        {
            _dialog = dialog;
        }

        private void CancelInstall()
        {
            //_dialog.Bootstrapper?.Cancel();
            _dialog.CloseBootstrapper();
        }
    }
}
