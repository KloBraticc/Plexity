using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Plexity.Models
{
    /// <summary>
    /// Represents a plugin item with metadata and installation status.
    /// </summary>
    public class PluginItem : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private string _iconUrl;
        private ImageSource _icon;
        private bool _isInstalling;
        private string _statusMessage;
        private bool _isInstalled;

        /// <summary>
        /// Plugin name.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// The filename for the plugin JSON or main descriptor.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Plugin description.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// URL of the plugin icon.
        /// </summary>
        public string IconUrl
        {
            get => _iconUrl;
            set => SetProperty(ref _iconUrl, value);
        }

        /// <summary>
        /// Icon image source.
        /// </summary>
        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// True if the plugin is currently installing.
        /// </summary>
        public bool IsInstalling
        {
            get => _isInstalling;
            set => SetProperty(ref _isInstalling, value);
        }

        /// <summary>
        /// Status message shown in UI (e.g. "Installing...", "Installed").
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// True if the plugin is installed and button should be disabled.
        /// </summary>
        public bool IsInstalled
        {
            get => _isInstalled;
            set => SetProperty(ref _isInstalled, value);
        }

        /// <summary>
        /// Path to the plugin's XAML file.
        /// </summary>
        public string XamlPath { get; set; }

        /// <summary>
        /// Path to the plugin's C# file.
        /// </summary>
        public string CsPath { get; set; }

        /// <summary>
        /// Optional custom install action.
        /// </summary>
        public Action InstallAction { get; set; }

        /// <summary>
        /// Property changed event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method to set the property and notify change.
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
