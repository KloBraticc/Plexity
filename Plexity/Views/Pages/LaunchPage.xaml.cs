using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Windows.Navigation;

namespace Plexity.Views.Pages
{
    public partial class LaunchPage : Page, INotifyPropertyChanged
    {
        private DispatcherTimer _loadingTimer;
        private int _dotCount = 0;

        private string _debugLogText;
        private bool _cancelEnabled;

        public LaunchPage()
        {
            InitializeComponent();
            this.DataContext = new LaunchPageViewModel();
            LoadingMessage.Text = App.MessageStatus.Prop.Message;
            DebugLogScrollViewer.Visibility = App.Settings.Prop.DebugLog ? Visibility.Visible : Visibility.Collapsed;
            MainContentPanel.Visibility = App.Settings.Prop.DebugLog ? Visibility.Collapsed : Visibility.Visible;

            _loadingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            _dotCount = (_dotCount + 1) % 4;
            string baseMessage = App.MessageStatus.Prop.Message;
            string baseMessageNoDots = baseMessage.TrimEnd('.');
            LoadingMessage.Text = $"{baseMessageNoDots}{new string('.', _dotCount)}";

            if (baseMessageNoDots.Equals("Starting Roblox", StringComparison.OrdinalIgnoreCase) && App.Settings.Prop.DebugLog)
            {
                DebugLogScrollViewer.Visibility = Visibility.Visible;
                MainContentPanel.Visibility = Visibility.Collapsed;
            }

            if (App.Settings.Prop.DebugLog)
            {
                DebugLogText = App.Logger.AsDocument;
            }
        }

        public string DebugLogText
        {
            get => _debugLogText;
            set
            {
                if (_debugLogText != value)
                {
                    _debugLogText = value;
                    OnPropertyChanged(nameof(DebugLogText));
                    SetDebugLogText(value);
                }
            }
        }

        public bool CancelEnabled
        {
            get => _cancelEnabled;
            set
            {
                if (_cancelEnabled != value)
                {
                    _cancelEnabled = value;
                    OnPropertyChanged(nameof(CancelEnabled));
                }
            }
        }

        private void SetDebugLogText(string text)
        {
            DebugLogTextBlock.Inlines.Clear();

            var urlRegex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled);
            int lastIndex = 0;

            foreach (Match match in urlRegex.Matches(text))
            {
                // Add plain text before the link
                if (match.Index > lastIndex)
                {
                    string before = text.Substring(lastIndex, match.Index - lastIndex);
                    DebugLogTextBlock.Inlines.Add(new Run(before));
                }

                try
                {
                    var hyperlink = new Hyperlink(new Run(match.Value))
                    {
                        NavigateUri = new Uri(match.Value),
                        Foreground = Brushes.DeepSkyBlue,
                        TextDecorations = TextDecorations.Underline.Clone()
                    };

                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Failed to open link: " + ex.Message);
                        }

                        e.Handled = true;
                    };

                    DebugLogTextBlock.Inlines.Add(hyperlink);
                }
                catch (UriFormatException)
                {
                    DebugLogTextBlock.Inlines.Add(new Run(match.Value)); // fallback to plain text
                }

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text after the last match
            if (lastIndex < text.Length)
            {
                DebugLogTextBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }

        public void SetCancelEnabled(bool enabled) => CancelEnabled = enabled;

        public void ShowBootstrapper() { }

        public void CloseBootstrapper() { }

        private void Cancel_Button(object sender, RoutedEventArgs e)
        {
            App.MessageStatus.Prop.Message = "Canceling";
            foreach (var proc in Process.GetProcessesByName("RobloxPlayerBeta"))
            {
                try { proc.Kill(); proc.WaitForExit(); } catch { }
            }

            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
