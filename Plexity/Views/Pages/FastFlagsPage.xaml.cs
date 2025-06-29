using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Plexity.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class FastFlagsPage : INavigableView<DashboardViewModel>, IDisposable
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
            this.Loaded += Page_Loaded;
            this.Unloaded += FastFlagsPage_Unloaded;
        }

        private void SetupViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.RequestPageReloadEvent -= OnRequestPageReload;
            }

            _viewModel = new FastFlagsViewModel();
            _viewModel.RequestPageReloadEvent += OnRequestPageReload;

            DataContext = _viewModel;
        }

        private void OnRequestPageReload(object sender, EventArgs e)
        {
            SetupViewModel();
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

        private void FastFlagsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestPageReloadEvent -= OnRequestPageReload;
            }
            this.Loaded -= Page_Loaded;
            this.Unloaded -= FastFlagsPage_Unloaded;
        }

        private void ValidateInt32(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            e.Handled = !Regex.IsMatch(newText, @"^-?\d*$");
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            e.Handled = !Regex.IsMatch(newText, @"^\d*$");
        }

        public void Dispose()
        {
            FastFlagsPage_Unloaded(this, null);
        }
    }
}
