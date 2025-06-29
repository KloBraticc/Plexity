using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Plexity.ViewModels.Pages
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        private string _version;
        private string _installedDate;

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string InstalledDate
        {
            get => _installedDate;
            set => SetProperty(ref _installedDate, value);
        }

        public AboutViewModel()
        {
            LoadVersion();
            LoadInstalledDate();
        }

        private void LoadVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Version = version?.ToString() ?? "Unknown Version";
            }
            catch
            {
                Version = "Unknown Version";
            }
        }

        private void LoadInstalledDate()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                var date = System.IO.File.GetCreationTime(exePath);
                InstalledDate = date.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                InstalledDate = "Unknown Date";
            }
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
