using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Plexity.ViewModels
{
    public class FlagViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private string _value;
        private bool _isEnabled;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                    // Update in App.FastFlags
                    ApplyFlagValue();
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                    
                    // If disabled, set value to null, otherwise use the stored value
                    Value = IsEnabled ? Value : null;
                }
            }
        }

        public string FlagKey { get; set; }

        private void ApplyFlagValue()
        {
            if (!string.IsNullOrEmpty(FlagKey))
            {
                App.FastFlags.SetPreset(FlagKey, Value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}