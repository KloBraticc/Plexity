using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Media;  // For VisualTreeHelper
using Wpf.Ui.Appearance;
using Plexity.Helpers;

namespace Plexity
{
    public partial class FirstTimeShow : Window, INotifyPropertyChanged
    {
        private bool _isInitialized = false;
        private bool _suppressThemeAnimation = false;

        private ApplicationTheme _currentTheme;
        private string _selectedPriority = App.Settings.Prop.RobloxPriority ?? "Normal";

        public FirstTimeShow()
        {
            InitializeComponent();

            // Subscribe to all Hyperlink.RequestNavigate events inside this window
            SubscribeHyperlinks(this);

            InitializeViewModel();
            DataContext = this;

            this.Closed += FirstTimeShow_Closed;
        }

        private void FirstTimeShow_Closed(object? sender, EventArgs e)
        {
            // Unsubscribe event handlers to prevent leaks
            UnsubscribeHyperlinks(this);
            this.Closed -= FirstTimeShow_Closed;
        }

        private void SubscribeHyperlinks(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Documents.Hyperlink hyperlink)
                {
                    hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                }
                else
                {
                    SubscribeHyperlinks(child);
                }
            }
        }

        private void UnsubscribeHyperlinks(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Documents.Hyperlink hyperlink)
                {
                    hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
                }
                else
                {
                    UnsubscribeHyperlinks(child);
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                using Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo(e.Uri.AbsoluteUri)
                    {
                        UseShellExecute = true
                    }
                };
                proc.Start();
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Failed to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ApplicationTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    App.Settings.Prop.ThemeModes = value == ApplicationTheme.Dark ? "Dark" : "Light";
                    OnPropertyChanged(nameof(CurrentTheme));

                    if (!_suppressThemeAnimation)
                    {
                        _ = ChangeThemeAsync(value).ConfigureAwait(false);
                    }
                }
            }
        }

        public List<ApplicationTheme> ThemeOptions2 { get; } = new() { ApplicationTheme.Light, ApplicationTheme.Dark };

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

        private void InitializeViewModel()
        {
            _suppressThemeAnimation = true;

            AppThemeChanger.Initialize(
                initialTheme: App.Settings.Prop.ThemeModes == "Light"
                    ? ApplicationTheme.Light
                    : ApplicationTheme.Dark,
                disableAnimations: false);

            CurrentTheme = AppThemeChanger.CurrentTheme;

            _isInitialized = true;
            _suppressThemeAnimation = false;
        }

        private async Task ChangeThemeAsync(ApplicationTheme newTheme)
        {
            try
            {
                await AppThemeChanger.ChangeThemeAsync(newTheme).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Theme change failed: {ex}");
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _currentTheme = AppThemeChanger.CurrentTheme;
                OnPropertyChanged(nameof(CurrentTheme));
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string modsFolder = Paths.Mods;

            if (Directory.Exists(modsFolder))
            {
                try
                {
                    using Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo("explorer.exe", modsFolder)
                        {
                            UseShellExecute = false
                        }
                    };
                    proc.Start();
                }
                catch (Exception ex)
                {
                    DialogService.ShowMessage($"Failed to open Mods folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                DialogService.ShowMessage("Mods folder not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
