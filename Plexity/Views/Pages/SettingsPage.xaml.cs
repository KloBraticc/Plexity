using System.Windows.Controls;
using System.Windows.Input;
using Plexity.ViewModels;
using Plexity.ViewModels.Pages;
using Plexity.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
            int seconds = App.Settings.Prop.RobloxStartWaitTime / 1000;
            LaunchDelayTextBox.Text = $"{seconds} sec";
           
        }


        private void LaunchDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Extract only digits
            string numericPart = new string(LaunchDelayTextBox.Text.Where(char.IsDigit).ToArray());

            if (int.TryParse(numericPart, out int seconds))
            {
                App.Settings.Prop.RobloxStartWaitTime = seconds * 1000;
            }
            else
            {
                App.Settings.Prop.RobloxStartWaitTime = 1000;
            }
        }

        private void LaunchDelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }




        private void LaunchDelayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string numericPart = new string(LaunchDelayTextBox.Text.Where(char.IsDigit).ToArray());

            if (int.TryParse(numericPart, out int seconds))
            {
                LaunchDelayTextBox.Text = $"{seconds} sec";
            }
        }

    }
}
