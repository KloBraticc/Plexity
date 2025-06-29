using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Plexity.Views.Pages
{
    public class LaunchPageViewModel : INotifyPropertyChanged
    {
        private double _progressMaximum = 100;
        public double ProgressMaximum
        {
            get => _progressMaximum;
            set => SetProperty(ref _progressMaximum, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                double newValue = Math.Max(0, Math.Min(ProgressMaximum, value));
                if (_progressValue != newValue)
                {
                    _progressValue = newValue;
                    OnPropertyChanged();

                    // Disable navigation while progress is between 0 and max
                    if (_progressValue > 0 && _progressValue < ProgressMaximum)
                        CanNavigate = false;
                    else
                        CanNavigate = true;
                }
            }
        }

        private FlowDirection _progressBarFlowDirection = FlowDirection.LeftToRight;
        public FlowDirection ProgressBarFlowDirection
        {
            get => _progressBarFlowDirection;
            set => SetProperty(ref _progressBarFlowDirection, value);
        }

        private bool _progressIncreasing = true;
        private DateTime _lastRenderTime;

        // Navigation lock property
        private bool _canNavigate = false;
        public bool CanNavigate
        {
            get => _canNavigate;
            set => SetProperty(ref _canNavigate, value);
        }

        public LaunchPageViewModel()
        {
            CancelEnabled = true;
            _lastRenderTime = DateTime.Now;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;

            const double speedPerSecond = 60; // slow speed for smoothness

            double delta = speedPerSecond * elapsed;

            if (_progressIncreasing)
            {
                ProgressValue += delta;
                if (ProgressValue >= ProgressMaximum)
                {
                    ProgressValue = ProgressMaximum;
                    _progressIncreasing = false;
                    ProgressBarFlowDirection = FlowDirection.RightToLeft;
                }
            }
            else
            {
                ProgressValue -= delta;
                if (ProgressValue <= 0)
                {
                    ProgressValue = 0;
                    _progressIncreasing = true;
                    ProgressBarFlowDirection = FlowDirection.LeftToRight;
                }
            }
        }

        private bool _cancelEnabled;
        public bool CancelEnabled
        {
            get => _cancelEnabled;
            set => SetProperty(ref _cancelEnabled, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }



        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
