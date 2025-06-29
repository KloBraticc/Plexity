using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Plexity
{
    public partial class AddFlagWindow : Window
    {
        // Forbidden flags not allowed in import
        private readonly string[] forbiddenFlags = { "DFFlagNoMinimumSwimVelocity" };

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public AddFlagWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !UInt32.TryParse(e.Text, out _);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON/Text Files (*.json;*.txt;*.md)|*.json;*.txt;*.md|All Files (*.*)|*.*",
                Title = "Import Flags File"
            };

            if (dialog.ShowDialog() != true)
                return;

            string fileContent;
            try
            {
                fileContent = await File.ReadAllTextAsync(dialog.FileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Failed to read file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                DialogService.ShowMessage("The selected file is empty.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Try parse as JSON even if .txt file
            if (!TryParseAndValidateJson(fileContent, out string formattedJson))
                return;

            JsonTextBox.Text = formattedJson;
            JsonTextBox.ScrollToHome(); // scroll to top
        }

        private bool TryParseAndValidateJson(string rawContent, out string formattedJson)
        {
            formattedJson = string.Empty;

            try
            {
                var parsedJson = JToken.Parse(rawContent);

                if (CheckTokenForForbiddenFlags(parsedJson))
                {
                    DialogService.ShowMessage("The imported file contains forbidden flags and cannot be imported.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                formattedJson = parsedJson.ToString(Formatting.Indented);
                return true;
            }
            catch (JsonReaderException jex)
            {
                DialogService.ShowMessage($"Invalid JSON format:\n{jex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Unexpected error:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool CheckTokenForForbiddenFlags(JToken token)
        {
            if (token == null) return false;

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                    {
                        if (IsForbiddenFlag(prop.Name) || IsForbiddenFlag(prop.Value.ToString()))
                            return true;

                        if (CheckTokenForForbiddenFlags(prop.Value))
                            return true;
                    }
                    break;

                case JTokenType.Array:
                    foreach (var item in token.Children())
                        if (CheckTokenForForbiddenFlags(item))
                            return true;
                    break;

                default:
                    if (IsForbiddenFlag(token.ToString()))
                        return true;
                    break;
            }

            return false;
        }

        private void JsonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OkButton.IsEnabled = !string.IsNullOrWhiteSpace(JsonTextBox.Text);
            if (JsonTextBox.Text.Length > 50000)
            {
                DialogService.ShowMessage("The file is very large and may have performance issuse.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool IsForbiddenFlag(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return forbiddenFlags.Any(flag =>
                value.IndexOf(flag, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
