using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Plexity.UI.ViewModels.Bootstrapper;


namespace Plexity.UI.ViewModels.Installer
{
    public class InstallViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Plexity.Installer installer = new();
        private readonly string _originalInstallLocation;

        public event EventHandler<bool>? SetCanContinueEvent;

        public string InstallLocation
        {
            get => installer.InstallLocation;
            set
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    SetCanContinueEvent?.Invoke(this, true);
                    installer.InstallLocationError = "";


                    OnPropertyChanged(nameof(ErrorMessage));
                }

                installer.InstallLocation = value;
                OnPropertyChanged(nameof(InstallLocation));
                OnPropertyChanged(nameof(DataFoundMessageVisibility));
            }
        }

        private void OnPropertyChanged(string v)
        {
            throw new NotImplementedException();
        }

        public Visibility DataFoundMessageVisibility =>
            installer.ExistingDataPresent ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorMessage => installer.InstallLocationError;

        public bool CreateDesktopShortcuts
        {
            get => installer.CreateDesktopShortcuts;
            set => installer.CreateDesktopShortcuts = value;
        }

        public bool CreateStartMenuShortcuts
        {
            get => installer.CreateStartMenuShortcuts;
            set => installer.CreateStartMenuShortcuts = value;
        }

        private RelayCommand? _browseInstallLocationCommand;
        public ICommand BrowseInstallLocationCommand => _browseInstallLocationCommand ??= new RelayCommand(BrowseInstallLocation);

        private void BrowseInstallLocation()
        {
            throw new NotImplementedException();
        }

        private RelayCommand? _resetInstallLocationCommand;
        public ICommand ResetInstallLocationCommand => _resetInstallLocationCommand ??= new RelayCommand(ResetInstallLocation);

        private RelayCommand? _openFolderCommand;
        public ICommand OpenFolderCommand => _openFolderCommand ??= new RelayCommand(OpenFolder);

        public InstallViewModel()
        {
            _originalInstallLocation = installer.InstallLocation;
        }

        public bool DoInstall()
        {
            if (!installer.CheckInstallLocation())
            {
                SetCanContinueEvent?.Invoke(this, false);
                OnPropertyChanged(nameof(ErrorMessage));
                return false;
            }

            installer.DoInstall();
            return true;
        }

        private void ResetInstallLocation()
        {
            InstallLocation = _originalInstallLocation;
        }

        private void OpenFolder()
        {
            try
            {
                Process.Start("explorer.exe", Paths.Base);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }
    }
}
