using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Windows;
using Plexity.ViewModels.Windows;
using Plexity.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Plexity.Helpers;
using System.Threading;
using System.Threading.Tasks;
using Plexity.Services;

namespace Plexity.Views.Windows
{
    public partial class MainWindow : INavigationWindow, INotifyPropertyChanged
    {
        private readonly MainWindowViewModel _viewModel = new();
        public MainWindowViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;
        private readonly DiscordService _discordService;
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;

        // Flag to lock navigation after launch button clicked
        private bool isNavigationLocked = false;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = _viewModel;
            DataContext = this;

            _navigationService = navigationService;
            _discordService = new DiscordService();

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            viewModel.DoInstall();
            InitializeWindowState();
            SetPageService(navigationViewPageProvider);
            _navigationService.SetNavigationControl(RootNavigation);

            if (App.Settings.Prop.UseModernWindowStyling)
            {
                WindowsVersionHelper.ApplyRoundedCorners(this);
            }

            // Check Discord server membership after window is loaded
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Small delay to ensure UI is fully loaded
            await Task.Delay(1000);
            
            // Check Discord server membership
            await CheckDiscordMembershipAsync();
        }

        private async Task CheckDiscordMembershipAsync()
        {
            const string LOG_IDENT = "MainWindow::CheckDiscordMembership";
            
            try
            {
                App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "Starting Discord server membership check...");

                // First validate the invite link
                var isValidInvite = await _discordService.ValidateInviteLinkAsync();
                if (!isValidInvite)
                {
                    App.Logger?.WriteLine(LogLevel.Warning, LOG_IDENT, "Invalid Discord invite link, skipping check"); return;
                }

                // Check if user is in server
                var isInServer = await _discordService.IsUserInServerAsync();
                
                if (!isInServer)
                {
                    App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "User not in Discord server, initiating join process");

                    // Show notification about Discord requirement
                    ShowNotification("Discord server check in progress...", 3000);
                    
                    // Auto-join the Discord server
                    var joinResult = await _discordService.CheckAndJoinDiscordServerAsync();
                    
                    if (joinResult)
                    {
                        ShowNotification("Please join our Discord server to continue!", 5000);
                    }
                    else
                    {
                        ShowNotification("Failed to open Discord invite. Please join manually.", 5000);
                    }
                }
                else
                {
                    App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "User is already in Discord server");
                    ShowNotification("Welcome back! You're already in our Discord server.", 2000);
                }
            }
            catch (Exception ex)
            {
                App.Logger?.WriteLine(LogLevel.Error, LOG_IDENT, $"Error during Discord membership check: {ex.Message}");
                ShowNotification("Discord check failed. You may need to join manually.", 4000);
            }
        }

        #region INavigationWindow implementation

        // Returns the navigation view control
        public INavigationView GetNavigation() => RootNavigation;

        // Navigates to a page, respecting navigation lock
        public bool Navigate(Type pageType)
        {
            if (isNavigationLocked)
                return false;

            return RootNavigation.Navigate(pageType);
        }

        // Sets the page provider service on the navigation control
        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        // Shows the window
        public void ShowWindow() => Show();

        // Closes the window
        public void CloseWindow() => Close();

        // Optional: implement if your interface requires it, otherwise can be empty
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            // You can leave empty if not used
        }

        #endregion

        private void SaveWindowState()
        {
            _state.Width = Width;
            _state.Height = Height;
            _state.Top = Top;
            _state.Left = Left;

            App.State.Save();
        }

        public void SetPaneDisplayMode(string mode)
        {
            NavigationViewPaneDisplayMode displayMode;

            switch (mode)
            {
                case "LeftFluent":
                    displayMode = NavigationViewPaneDisplayMode.LeftFluent;
                    break;
                case "LeftMinimal":
                    displayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                    break;
                case "Top":
                    displayMode = NavigationViewPaneDisplayMode.Top;
                    break;
                default:
                    displayMode = NavigationViewPaneDisplayMode.LeftFluent;
                    mode = "LeftFluent";
                    break;
            }

            RootNavigation.PaneDisplayMode = displayMode;
            App.Settings.Prop.PaneDisplayMode = mode;
            App.Settings.Save();

            RootNavigation.Visibility = Visibility.Collapsed;
            RootNavigation.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            RootNavigation.Visibility = Visibility.Visible;
        }

        private void InitializeWindowState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth || _state.Top > SystemParameters.VirtualScreenHeight)
            {
                _state.Left = 0;
                _state.Top = 0;
            }

            if (_state.Width > 0) Width = _state.Width;
            if (_state.Height > 0) Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = _state.Left;
                Top = _state.Top;
            }
        }

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowState();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            App.Terminate();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private CancellationTokenSource? _notificationCts;

        public async void ShowNotification(string message, int durationMs = 1800)
        {
            _notificationCts?.Cancel();
            _notificationCts = new CancellationTokenSource();
            var token = _notificationCts.Token;

            NotificationText.Text = message;

            if (NotificationPanel.Visibility != Visibility.Visible)
                NotificationPanel.Visibility = Visibility.Visible;

            NotificationPanel.BeginAnimation(UIElement.OpacityProperty, null);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            var easingOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            var easingIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            var fadeIn = new DoubleAnimation
            {
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = easingOut
            };

            var scaleIn = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = easingOut
            };

            NotificationPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);

            try
            {
                await Task.Delay(durationMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = easingIn
            };

            var scaleOut = new DoubleAnimation
            {
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = easingIn
            };

            fadeOut.Completed += (s, e) =>
            {
                if (!token.IsCancellationRequested)
                    NotificationPanel.Visibility = Visibility.Collapsed;
            };

            NotificationPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            NotificationScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }

        // Fixed: Removed async as it's not needed
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _navigationService.Navigate(typeof(LaunchPage));
                ShowNotification("Plexity Saved and Launched Roblox!");
                ViewModel.Save();
                ViewModel.SaveAndLaunchSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Loading failed: {ex}");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ShowNotification("Plexity Settings Saved!");
            ViewModel.Save();
        }

        #region Commands for StatusBar Buttons

        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);
        public ICommand CloseWindowCommand => new RelayCommand(CloseApplication);

        private void SaveAndLaunchSettings()
        {
            try
            {
                ViewModel.SaveAndLaunchSettings();
                ShowNotification("Settings saved and launching...");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to save and launch: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                ViewModel.Save();
                ShowNotification("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to save settings: {ex.Message}");
            }
        }

        private void CloseApplication()
        {
            Close();
        }

        #endregion

        #region Notification System

        private void ShowErrorMessage(string message)
        {
            DialogService.ShowConfirm(message, "Error");
        }

        #endregion

        #region INotifyPropertyChanged

        // Fixed: Made nullable to match interface
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Fixed: Made nullable to match interface
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Fixed: Made parameter nullable to match interface
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        // Fixed: Made parameter nullable to match interface
        public void Execute(object? parameter) => _execute();
    }
}
