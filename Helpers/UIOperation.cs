using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Plexity.Helpers
{
    public class UIOperation : INotifyPropertyChanged
    {
        private bool _isInProgress;
        private bool _isCompleted;
        private bool _hasError;
        private string _statusMessage;

        public bool IsInProgress
        {
            get => _isInProgress;
            private set
            {
                if (_isInProgress != value)
                {
                    _isInProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            private set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task RunAsync(Func<Task> operation, string inProgressMessage = "Processing...", int completedVisibleMs = 2000)
        {
            if (IsInProgress)
                return;

            try
            {
                // Reset state
                IsCompleted = false;
                HasError = false;
                IsInProgress = true;
                StatusMessage = inProgressMessage;

                // Run operation
                await operation();

                // Show completed
                IsInProgress = false;
                IsCompleted = true;
                StatusMessage = "Completed";

                // Hide completed after delay
                if (completedVisibleMs > 0)
                {
                    await Task.Delay(completedVisibleMs);
                    IsCompleted = false;
                    StatusMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                IsInProgress = false;
                HasError = true;
                StatusMessage = $"Error: {ex.Message}";

                // Log error
                App.Logger.WriteLine(LogLevel.Error, "UIOperation", ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}