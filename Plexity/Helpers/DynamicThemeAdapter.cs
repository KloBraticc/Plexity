using System;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using System.Diagnostics;

namespace Plexity.Helpers
{
    public class DynamicThemeAdapter : IDisposable
    {
        private const string PersonalizeRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private readonly RegistryMonitor _registryMonitor;
        private bool _disposed;

        public event EventHandler<ApplicationTheme> ThemeChanged;

        public DynamicThemeAdapter()
        {
            _registryMonitor = new RegistryMonitor(RegistryHive.CurrentUser, PersonalizeRegistryPath);
            _registryMonitor.RegChanged += RegistryMonitor_RegChanged;
            _registryMonitor.Start();
        }

        private void RegistryMonitor_RegChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (App.Settings.Prop.UseDynamicTheme)
                {
                    var newTheme = GetCurrentWindowsTheme();
                    ThemeChanged?.Invoke(this, newTheme);
                    
                    // Update app theme
                    ApplicationThemeManager.Apply(newTheme);
                    
                    // Update settings if needed
                    App.Settings.Prop.ThemeModes = newTheme == ApplicationTheme.Dark ? "Dark" : "Light";
                    
                    // Update accent colors if system accent color is enabled
                    if (App.Settings.Prop.UseSystemAccentColor)
                    {
                        SystemAccentColorHelper.ApplySystemAccentColor(
                            Application.Current, 
                            newTheme == ApplicationTheme.Dark);
                    }
                }
            });
        }

        public ApplicationTheme GetCurrentWindowsTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting Windows theme: {ex.Message}");
                return ApplicationTheme.Light;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _registryMonitor?.Dispose();
                _disposed = true;
            }
        }
    }

    // Registry monitor helper class
    public class RegistryMonitor : IDisposable
    {
        private readonly RegistryHive _hive;
        private readonly string _keyPath;
        private bool _disposed;
        private readonly Timer _timer;

        public event EventHandler RegChanged;

        public RegistryMonitor(RegistryHive hive, string keyPath)
        {
            _hive = hive;
            _keyPath = keyPath;
            _timer = new Timer(CheckRegistry, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _timer.Change(0, 1000); // Check every second
        }

        private void CheckRegistry(object state)
        {
            // This simple approach uses polling - for a more efficient solution,
            // you could use the Windows API RegisterForSettingChange
            RegChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}