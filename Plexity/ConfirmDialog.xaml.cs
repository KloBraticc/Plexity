using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Plexity
{
    public partial class ConfirmDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        private bool _isMessageOnly;

        // New constructor overload for message-only dialog
        public ConfirmDialog(string message, bool isMessageOnly = false)
        {
            InitializeComponent();
            MessageText.Text = message;
            _isMessageOnly = isMessageOnly;

            if (_isMessageOnly)
            {
                // Hide Yes/No buttons, show OK button
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                Title = "Message";
            }
            else
            {
                // Confirm mode: show Yes/No buttons, hide OK button
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                OkButton.Visibility = Visibility.Collapsed;
                Title = "Confirm";
            }
        }

        public void Yes_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        public void No_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        public void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
