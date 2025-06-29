using System.Linq;
using System.Windows;

namespace Plexity
{
    public static class DialogService
    {
        public static bool ShowConfirm(string message, string caption = "Confirm")
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            var dialog = new ConfirmDialog(message)
            {
                Title = caption,
                Owner = owner
            };

            var result = dialog.ShowDialog();
            return result == true && dialog.IsConfirmed;
        }



        public static void ShowMessage(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            var dialog = new ConfirmDialog(message, isMessageOnly: true)
            {
                Title = caption,
                Owner = owner
            };

            dialog.ShowDialog();
        }

    }
}
