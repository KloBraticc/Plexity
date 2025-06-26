using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Plexity.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class FastFlagsPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private bool _initialLoad = false;

        private FastFlagsViewModel _viewModel;

        public FastFlagsPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
            InitializeComponent();
            SetupViewModel();
        }

        private void SetupViewModel()
        {
            _viewModel = new FastFlagsViewModel();
            _viewModel.RequestPageReloadEvent += (_, _) => SetupViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_initialLoad)
            {
                _initialLoad = true;
                return;
            }

            SetupViewModel();
        }

        private void ValidateInt32(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text != "-" && !int.TryParse(e.Text, out _);
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !uint.TryParse(e.Text, out _);
        }
    }
}
