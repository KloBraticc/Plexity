using System.Diagnostics;
using System.Windows.Navigation;
using Plexity.ViewModels.Pages;
using System.Windows.Controls;

namespace Plexity.Views.Pages
{
    public partial class AboutPage : Page
    {
        public AboutViewModel ViewModel { get; }

        public AboutPage()
        {
            ViewModel = new AboutViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
