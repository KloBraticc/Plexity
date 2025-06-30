using System.Windows.Controls;
using System.Windows.Input;
using Plexity.ViewModels;
using Plexity.ViewModels.Pages;
using Plexity.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class TweaksPage : INavigableView<TweaksViewModel>
    {
        public TweaksViewModel ViewModel { get; }

        public TweaksPage(TweaksViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
        }
    }
}
