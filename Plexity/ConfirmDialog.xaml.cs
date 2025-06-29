using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Plexity
{
    public partial class ConfirmDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        private readonly bool _isMessageOnly;

        public ConfirmDialog(string message, bool isMessageOnly = false)
        {
            InitializeComponent();

            Closed += (s, e) =>
            {
                MessageText.Text = null;
            };

            MessageText.Text = message;
            _isMessageOnly = isMessageOnly;

            if (_isMessageOnly)
            {
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                Title = "Message";
            }
            else
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                OkButton.Visibility = Visibility.Collapsed;
                Title = "Confirm";
            }
        }


        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Treat OK as confirmed in message-only mode
            if (_isMessageOnly)
            {
                IsConfirmed = true;
            }
            DialogResult = true;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        /// <summary>
        /// Static helper to show the dialog modally.
        /// Returns true if user confirmed (Yes or OK).
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="isMessageOnly">True to show message-only dialog with OK button</param>
        /// <returns></returns>
        public static bool Show(string message, bool isMessageOnly = false)
        {
            var dialog = new ConfirmDialog(message, isMessageOnly)
            {
                Owner = Application.Current?.MainWindow // set owner for modal behavior and centering
            };

            dialog.ShowDialog();

            return dialog.IsConfirmed;
        }
    }
}
