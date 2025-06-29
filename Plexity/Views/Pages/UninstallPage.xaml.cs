using Plexity.UI.ViewModels.Installer;
using Plexity.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows;

namespace Plexity.Views.Pages
{
    public partial class UninstallPage
    {
        public bool Confirmed { get; private set; } = false;
        public bool KeepData { get; private set; } = true;

        public UninstallPage()
        {
            InitializeComponent();

            var viewModel = new UninstallViewModel();

            viewModel.ConfirmUninstallRequest += (_, _) =>
            {
                Confirmed = true;
                KeepData = viewModel.KeepData;

                var window = Window.GetWindow(this);
                window?.Close();
            };

            DataContext = viewModel;
        }

        internal void ShowDialog()
        {
            throw new NotImplementedException();
        }
    }
}
