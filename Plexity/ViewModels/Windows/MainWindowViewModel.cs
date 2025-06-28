using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plexity.Enums;
using Wpf.Ui.Controls;

namespace Plexity.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // Commands
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);
        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

        // Events
        public event EventHandler<bool>? SetCanContinueEvent;
        public event EventHandler? RequestSaveLaunchNoticeEvent;
        public EventHandler? RequestSaveNoticeEvent;
        public event EventHandler? RequestCloseWindowEvent;
        public event EventHandler<string>? PageRequest;

        // Installer logic
        private readonly Installer installer = new();
        private readonly string _originalInstallLocation;

        // Constructor
        public MainWindowViewModel()
        {
            _originalInstallLocation = installer.InstallLocation;

            UpdateIconsToModern();
        }

        // Observable properties
        [ObservableProperty]
        private string _applicationTitle = "Plexity";

        [ObservableProperty]
        private bool _isNavigationEnabled = true;

        [ObservableProperty]
        private ObservableCollection<NavigationViewItem> _menuItems =
        [
            new NavigationViewItem
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home20 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem
            {
                Content = "Deploy",
                Icon = new SymbolIcon { Symbol = SymbolRegular.PlaySettings20 },
                TargetPageType = typeof(Views.Pages.DeploymentPage)
            },
            new NavigationViewItem
            {
                Content = "Mods",
                Icon = new SymbolIcon { Symbol = SymbolRegular.BoxToolbox20 },
                TargetPageType = typeof(Views.Pages.ModsPage)
            },
            new NavigationViewItem
            {
                Content = "Editor",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Edit20 },
                TargetPageType = typeof(Views.Pages.FastFlagEditor)
            },
            new NavigationViewItem
            {
                Content = "FFlags",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Flag20 },
                TargetPageType = typeof(Views.Pages.FastFlagsPage)
            },
            new NavigationViewItem
            {
                Content = "Files",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Folder20 },
                TargetPageType = typeof(Views.Pages.VersionsPage)
            },
            new NavigationViewItem
            {
                Content = "Plugins",
                Icon = new SymbolIcon { Symbol = SymbolRegular.BoxToolbox20 },
                TargetPageType = typeof(Views.Pages.PluginsPage)
            },
            new NavigationViewItem
            {
                Content = "Tweaks",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Lightbulb20 },
                TargetPageType = typeof(Views.Pages.TweaksPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<NavigationViewItem> _footerMenuItems =
        [
            new NavigationViewItem
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings20 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            },
            new NavigationViewItem
            {
                Content = "About",
                Icon = new SymbolIcon { Symbol = SymbolRegular.QuestionCircle20 },
                TargetPageType = typeof(Views.Pages.AboutPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };

        // Install location logic
        public string InstallLocation
        {
            get => installer.InstallLocation;
            set
            {
                installer.InstallLocation = value;

                if (!string.IsNullOrEmpty(installer.InstallLocationError))
                {
                    installer.InstallLocationError = "";
                    SetCanContinueEvent?.Invoke(this, true);
                }

                OnPropertyChanged(nameof(InstallLocation));
                OnPropertyChanged(nameof(DataFoundMessageVisibility));
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }


        private void SaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            App.Settings.Save();
            App.State.Save();
            App.FastFlags.Save();

            foreach (var pair in App.PendingSettingTasks)
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            App.PendingSettingTasks.Clear();

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }

        public string ErrorMessage => installer.InstallLocationError;

        public Visibility DataFoundMessageVisibility =>
            installer.ExistingDataPresent ? Visibility.Visible : Visibility.Collapsed;

        // Installation process
        public bool DoInstall()
        {
            if (installer.ExistingDataPresent)
            {
                Debug.WriteLine("Installation skipped: Already installed.");
                return true;
            }

            if (!installer.CheckInstallLocation())
            {
                SetCanContinueEvent?.Invoke(this, false);
                OnPropertyChanged(nameof(ErrorMessage));
                return false;
            }

            try
            {
                installer.DoInstall();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation failed: {ex.Message}");
                OnPropertyChanged(nameof(ErrorMessage));
                SetCanContinueEvent?.Invoke(this, false);
                return false;
            }
        }

        // Command handlers
        private void CloseWindow()
        {
            RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            SaveSettings();
        }

        public void SaveAndLaunchSettings()
        {
            // Disable all navigation
            IsNavigationEnabled = false;

            // Notify any views if needed
            RequestSaveLaunchNoticeEvent?.Invoke(this, EventArgs.Empty);

            // Launch the Roblox client
            LaunchHandler.LaunchRoblox(LaunchMode.Player);
        }

        // Add a method to update icons based on settings
        private void UpdateIconsToModern()
        {
            if (!App.Settings.Prop.UseModernIcons)
                return;
                
            try
            {
                // Update menu items with modern icons
                foreach (var item in MenuItems)
                {
                    object? iconResource = null;
                    switch (item.Content.ToString())
                    {
                        case "Home":
                            iconResource = Application.Current.Resources["ModernHomeIcon"];
                            break;
                        case "Deploy":
                            iconResource = Application.Current.Resources["ModernDeployIcon"];
                            break;
                        case "Mods":
                            iconResource = Application.Current.Resources["ModernModsIcon"];
                            break;
                        case "Editor":
                            iconResource = Application.Current.Resources["ModernEditorIcon"];
                            break;
                        case "FFlags":
                            iconResource = Application.Current.Resources["ModernFlagsIcon"];
                            break;
                        case "Files":
                            iconResource = Application.Current.Resources["ModernFilesIcon"];
                            break;
                        case "Plugins":
                            iconResource = Application.Current.Resources["ModernPluginsIcon"];
                            break;
                        case "Tweaks":
                            iconResource = Application.Current.Resources["ModernTweaksIcon"];
                            break;
                    }
                    
                    if (iconResource is SymbolIcon symbolIcon)
                    {
                        item.Icon = symbolIcon;
                    }
                }
                
                // Update footer items with modern icons
                foreach (var item in FooterMenuItems)
                {
                    object? iconResource = null;
                    switch (item.Content.ToString())
                    {
                        case "Settings":
                            iconResource = Application.Current.Resources["ModernSettingsIcon"];
                            break;
                        case "About":
                            iconResource = Application.Current.Resources["ModernAboutIcon"];
                            break;
                    }
                    
                    if (iconResource is SymbolIcon symbolIcon)
                    {
                        item.Icon = symbolIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to update icons: {ex.Message}");
            }
        }
    }
}
