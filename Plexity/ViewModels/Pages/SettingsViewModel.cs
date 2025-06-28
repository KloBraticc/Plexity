using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Plexity.Helpers;
using System.Collections.ObjectModel;
using Wpf.Ui.Appearance;
using Wpf.Ui.Abstractions.Controls;
using Plexity.Views.Windows;
using System.Diagnostics;
using Plexity.Models.Persistable;

namespace Plexity.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware, IDisposable
    {
        private bool _isInitialized;
        private bool _suppressThemeAnimation;
        private bool _disposed;

        [ObservableProperty]
        private ApplicationTheme _currentTheme;

        private string _selectedPriority = App.Settings.Prop.RobloxPriority ?? "Normal";

        public ObservableCollection<string> PaneOptions { get; } = new ObservableCollection<string>
        {
            "LeftFluent",
            "LeftMinimal",
            "Top"
        };

        public string SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority != value)
                {
                    _selectedPriority = value;
                    App.Settings.Prop.RobloxPriority = value;
                    OnPropertyChanged(nameof(SelectedPriority));
                }
            }
        }

        public ObservableCollection<string> PriorityOptions { get; } = new()
        {
            "Low", "BelowNormal", "Normal", "AboveNormal", "High"
        };

        public List<ApplicationTheme> ThemeOptions { get; } = new()
        {
            ApplicationTheme.Light, ApplicationTheme.Dark
        };

        public bool DisableAnimations
        {
            get => AppThemeChanger.DisableAnimations;
            set
            {
                if (AppThemeChanger.DisableAnimations != value)
                {
                    AppThemeChanger.DisableAnimations = value;
                    OnPropertyChanged(nameof(DisableAnimations));
                }
            }
        }

        private string _currentPane = App.Settings.Prop.PaneDisplayMode ?? "LeftFluent";

        public string CurrentPane
        {
            get => _currentPane;
            set
            {
                if (SetProperty(ref _currentPane, value))
                {
                    App.Settings.Prop.PaneDisplayMode = value;
                    OnPaneChanged();
                }
            }
        }

        private void OnPaneChanged()
        {
            if (App.Current?.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SetPaneDisplayMode(_currentPane);
            }
            else
            {
                Debug.WriteLine("MainWindow instance not found or not ready.");
            }
        }

        public bool DebugLogging
        {
            get => App.Settings.Prop.DebugLog;
            set
            {
                if (App.Settings.Prop.DebugLog != value)
                {
                    App.Settings.Prop.DebugLog = value;
                    OnPropertyChanged(nameof(DebugLogging));
                }
            }
        }

        public bool KeepPlexityOpen
        {
            get => App.Settings.Prop.KeepPlexityOpen;
            set
            {
                if (App.Settings.Prop.KeepPlexityOpen != value)
                {
                    App.Settings.Prop.KeepPlexityOpen = value;
                    OnPropertyChanged(nameof(KeepPlexityOpen));
                }
            }
        }

        // Add property for UI density options
        public ObservableCollection<UIDensity> DensityOptions { get; } = new ObservableCollection<UIDensity>
        {
            UIDensity.Compact,
            UIDensity.Regular,
            UIDensity.Expanded
        };

        // Add property for selected density
        private UIDensity _selectedDensity = App.Settings.Prop.UIDisplayDensity;
        public UIDensity SelectedDensity
        {
            get => _selectedDensity;
            set
            {
                if (SetProperty(ref _selectedDensity, value))
                {
                    App.Settings.Prop.UIDisplayDensity = value;
                    UIDensityManager.ApplyDensityMode((UIDensityManager.DensityMode)value);
                }
            }
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            await Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            _suppressThemeAnimation = true;

            AppThemeChanger.Initialize(
                initialTheme: App.Settings.Prop.ThemeModes == "Light"
                    ? ApplicationTheme.Light
                    : ApplicationTheme.Dark,
                disableAnimations: DisableAnimations);

            CurrentTheme = AppThemeChanger.CurrentTheme;
            _isInitialized = true;
            _suppressThemeAnimation = false;
        }

        partial void OnCurrentThemeChanged(ApplicationTheme value)
        {
            if (_suppressThemeAnimation)
                return;

            _ = ChangeThemeAsync(value);
        }

        private async Task ChangeThemeAsync(ApplicationTheme newTheme)
        {
            await AppThemeChanger.ChangeThemeAsync(newTheme);

            _currentTheme = AppThemeChanger.CurrentTheme;
            App.Settings.Prop.ThemeModes = _currentTheme == ApplicationTheme.Dark ? "Dark" : "Light";

            OnPropertyChanged(nameof(CurrentTheme));
        }

        public string RobloxPriority
        {
            get => App.Settings.Prop.RobloxPriority;
            set
            {
                if (App.Settings.Prop.RobloxPriority != value)
                {
                    App.Settings.Prop.RobloxPriority = value;
                    OnPropertyChanged(nameof(RobloxPriority));
                }
            }
        }

        public bool ForceRobloxReinstallation
        {
            get => App.Settings.Prop.ForceRobloxReinstallation;
            set => App.Settings.Prop.ForceRobloxReinstallation = value;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            PriorityOptions?.Clear();
            ThemeOptions?.Clear();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
