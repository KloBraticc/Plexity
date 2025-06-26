using Plexity.UI.ViewModels.Settings;
using Plexity.ViewModels;
using Plexity.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class ModsPage : INavigableView<ModsViewModel>
    {
        public ModsViewModel ViewModel { get; }

        public ModsPage(ModsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
        }
    }
}
