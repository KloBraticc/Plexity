using Plexity.ViewModels.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>, IDisposable
    {
        public DashboardViewModel ViewModel { get; }

        private bool _disposed = false;

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel; // Changed from 'this' to 'ViewModel' for consistency

            InitializeComponent();

            // Set welcome text with current Windows username
            string userName = Environment.UserName;
            WelcomeText.Text = $"Welcome, {userName}";

            // Subscribe to TextChanged event for placeholder visibility toggle
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchPlaceholder.Visibility = Visibility.Visible; // Initially visible

            // Initialize placeholder visibility on load
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Subscribe to Unloaded event to clean up resources
            Unloaded += Page_Unloaded;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update placeholder visibility based on text content
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Create a custom TextBox control or use WPF-UI components with built-in placeholder support
        // instead of manually managing visibility

        // Alternatively, implement it via attached behavior to keep the view cleaner
        public static class TextBoxPlaceholderBehavior
        {
            public static readonly DependencyProperty PlaceholderTextProperty =
                DependencyProperty.RegisterAttached(
                    "PlaceholderText",
                    typeof(string),
                    typeof(TextBoxPlaceholderBehavior),
                    new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

            public static string GetPlaceholderText(DependencyObject obj)
            {
                return (string)obj.GetValue(PlaceholderTextProperty);
            }

            public static void SetPlaceholderText(DependencyObject obj, string value)
            {
                obj.SetValue(PlaceholderTextProperty, value);
            }

            private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is TextBox textBox)
                {
                    textBox.Loaded += (s, args) =>
                    {
                        // Find or create a TextBlock for the placeholder
                        var parent = textBox.Parent as Panel;
                        if (parent != null)
                        {
                            // Create placeholder TextBlock if needed
                            var placeholderName = $"{textBox.Name}Placeholder";
                            var placeholder = parent.FindName(placeholderName) as TextBlock;

                            if (placeholder != null)
                            {
                                placeholder.Text = (string)e.NewValue;

                                // Set initial visibility
                                placeholder.Visibility = string.IsNullOrEmpty(textBox.Text)
                                    ? Visibility.Visible
                                    : Visibility.Collapsed;

                                // Subscribe to TextChanged event
                                textBox.TextChanged += (sender, eventArgs) =>
                                {
                                    placeholder.Visibility = string.IsNullOrEmpty(textBox.Text)
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;
                                };
                            }
                        }
                    };
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    SearchBox.TextChanged -= SearchBox_TextChanged;

                    // Unsubscribe from any ViewModel events if applicable
                }

                _disposed = true;
            }
        }

        // Optional: Add page unload event handler to clean up resources
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
    }
}
