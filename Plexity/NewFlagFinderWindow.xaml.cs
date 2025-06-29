using Microsoft.Win32;
using Plexity.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Plexity
{
    public partial class NewFlagFinderWindow : Window
    {
        private readonly FlagDataService _flagService = new();
        private Dictionary<string, object> _currentDisplayedFlags = new();
        private bool _isLoading = false;
        private Plexity.Services.ValidationResult _lastValidationResult;

        public NewFlagFinderWindow()
        {
            InitializeComponent();
            LoadCachedData();
        }

        private async void LoadCachedData()
        {
            _flagService.LoadCachedFlags();
            await RefreshFlags();
        }

        private async Task RefreshFlags()
        {
            if (_isLoading) return;

            _isLoading = true;
            SetLoadingState(true);

            try
            {
                bool success = await _flagService.FetchAllFlagsAsync();
                if (success)
                {
                    StatusText.Text = $"Successfully fetched flags from all sources. Last updated: {DateTime.Now:HH:mm:ss}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    StatusText.Text = "Failed to fetch flags from some sources. Using cached data.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error fetching flags: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                _isLoading = false;
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            RefreshButton.IsEnabled = !isLoading;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshFlags();
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var input = ValidationInput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                ValidationResults.Text = "Please enter flags to validate.";
                ValidationResults.Foreground = new SolidColorBrush(Colors.Gray);
                return;
            }

            try
            {
                var inputFlags = ParseInputFlags(input);
                _lastValidationResult = _flagService.ValidateFlags(inputFlags);

                DisplayValidationResults(_lastValidationResult);
            }
            catch (Exception ex)
            {
                ValidationResults.Text = $"Error parsing input: {ex.Message}";
                ValidationResults.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void RemoveInvalidButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastValidationResult == null)
            {
                StatusText.Text = "Please validate flags first before removing invalid ones.";
                StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                return;
            }

            if (_lastValidationResult.InvalidFlags.Count == 0)
            {
                StatusText.Text = "No invalid flags to remove.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
                return;
            }

            try
            {
                var cleanedInput = _flagService.RemoveInvalidFlags(ValidationInput.Text, _lastValidationResult);
                ValidationInput.Text = cleanedInput;
                
                StatusText.Text = $"Removed {_lastValidationResult.InvalidFlags.Count} invalid flags from input.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
                
                // Re-validate after removal
                ValidateButton_Click(sender, e);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error removing invalid flags: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private Dictionary<string, object> ParseInputFlags(string input)
        {
            var flags = new Dictionary<string, object>();

            if (input.TrimStart().StartsWith("{"))
            {
                // JSON format
                var jsonDoc = JsonDocument.Parse(input);
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    flags[property.Name] = property.Value.ToString();
                }
            }
            else
            {
                // Line-by-line format
                var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    if (trimmed.Contains('='))
                    {
                        var parts = trimmed.Split('=', 2);
                        flags[parts[0].Trim()] = parts[1].Trim();
                    }
                    else if (trimmed.Contains(':'))
                    {
                        var parts = trimmed.Split(':', 2);
                        flags[parts[0].Trim().Trim('"')] = parts[1].Trim().Trim('"', ',');
                    }
                    else
                    {
                        flags[trimmed] = "true";
                    }
                }
            }

            return flags;
        }

        private void DisplayValidationResults(Plexity.Services.ValidationResult result)
        {
            var sb = new StringBuilder();

            if (result.Duplicates.Any())
            {
                sb.AppendLine("⚠️ DUPLICATES FOUND:");
                foreach (var duplicate in result.Duplicates)
                {
                    sb.AppendLine($"  • {duplicate}");
                }
                sb.AppendLine();
            }

            if (result.ValidFlags.Any())
            {
                sb.AppendLine("✅ VALIDATED FLAGS:");
                foreach (var flag in result.ValidFlags)
                {
                    sb.AppendLine($"  • {flag.Key}: {flag.Value}");
                }
                sb.AppendLine();
            }

            if (result.InvalidFlags.Any())
            {
                sb.AppendLine("❌ UNVALIDATED FLAGS:");
                foreach (var flag in result.InvalidFlags)
                {
                    sb.AppendLine($"  • {flag.Key}: {flag.Value} (not found in fetched data)");
                }
            }

            ValidationResults.Text = sb.ToString();
            ValidationResults.Foreground = result.InvalidFlags.Any() || result.Duplicates.Any() 
                ? new SolidColorBrush(Colors.Orange) 
                : new SolidColorBrush(Colors.Green);
        }

        private void ImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select file to import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var content = File.ReadAllText(openFileDialog.FileName);
                    ValidationInput.Text = content;
                    ValidateButton_Click(sender, e);
                }
                catch (Exception ex)
                {
                    ValidationResults.Text = $"Error reading file: {ex.Message}";
                    ValidationResults.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void ShowLast24HoursButton_Click(object sender, RoutedEventArgs e)
        {
            var recentFlags = _flagService.GetFlagsAddedInLast24Hours();
            _currentDisplayedFlags = recentFlags;
            DisplayFlagsInRecentTab(recentFlags, "Flags added in the last 24 hours");
        }

        private void ShowTrueFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var recentFlags = _flagService.GetFlagsAddedInLast24Hours();
            var trueFlags = _flagService.FilterFlags(recentFlags, true);
            _currentDisplayedFlags = trueFlags;
            DisplayFlagsInRecentTab(trueFlags, "True flags from last 24 hours");
        }

        private void ShowFalseFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var recentFlags = _flagService.GetFlagsAddedInLast24Hours();
            var falseFlags = _flagService.FilterFlags(recentFlags, false);
            _currentDisplayedFlags = falseFlags;
            DisplayFlagsInRecentTab(falseFlags, "False flags from last 24 hours");
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchTerm = SearchInput.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                StatusText.Text = "Please enter a search term.";
                StatusText.Foreground = new SolidColorBrush(Colors.Gray);
                return;
            }

            var searchResults = _flagService.SearchFlags(searchTerm);
            _currentDisplayedFlags = searchResults;
            DisplayFlagsInSearchTab(searchResults, $"Search results for '{searchTerm}'");
        }

        private void DisplayFlagsInRecentTab(Dictionary<string, object> flags, string title)
        {
            if (!flags.Any())
            {
                StatusText.Text = $"{title}: No flags found.";
                FlagDataGrid.ItemsSource = null;
                return;
            }

            var items = new List<DisplayFlag>();

            foreach (var kvp in flags)
            {
                string value = kvp.Value?.ToString() ?? "null";
                string source = string.Empty;
                DateTime? timestamp = null;

                var typeName = kvp.Value?.GetType().Name ?? "null";
                Console.WriteLine($"Processing flag '{kvp.Key}', Value Type: {typeName}");

                if (kvp.Value is FlagDetail detail)
                {
                    value = detail.Value?.ToString() ?? "null";
                    source = detail.Source ?? "";
                    timestamp = detail.Timestamp;
                }
                else if (kvp.Value is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("Value", out var valProp))
                        value = valProp.ToString();
                    if (jsonElement.TryGetProperty("Source", out var sourceProp))
                        source = sourceProp.GetString() ?? "";
                    if (jsonElement.TryGetProperty("Timestamp", out var tsProp) &&
                        DateTime.TryParse(tsProp.GetString(), out var dt))
                        timestamp = dt;
                }
                else
                {
                    // Could add more type checks here if needed
                }

                items.Add(new DisplayFlag
                {
                    Name = kvp.Key,
                    Value = value,
                    Source = source,
                    Timestamp = timestamp
                });
            }

            FlagDataGrid.ItemsSource = null;
            FlagDataGrid.ItemsSource = items;

            StatusText.Text = $"{title} ({flags.Count} flags)";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }


        // Classes for your data grid binding
        public class FlagDetail
        {
            public object Value { get; set; }
            public string Source { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class DisplayFlag
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Source { get; set; }
            public DateTime? Timestamp { get; set; }

            // Optional for friendly display in DataGrid
            public string TimestampDisplay => Timestamp?.ToString("g") ?? "";
        }





        private void DisplayFlagsInSearchTab(Dictionary<string, object> flags, string title)
        {
            if (!flags.Any())
            {
                SearchResults.Text = $"{title}: No flags found.";
                return;
            }


            var formattedOutput = FormatFlagsAsJson(flags);
            SearchResults.Text = $"{title} ({flags.Count} flags):\n\n{formattedOutput}";
        }

        private string FormatFlagsAsJson(Dictionary<string, object> flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            var sortedFlags = flags.OrderBy(kvp => kvp.Key).ToList();
            for (int i = 0; i < sortedFlags.Count; i++)
            {
                var flag = sortedFlags[i];
                var isLast = i == sortedFlags.Count - 1;
                var value = FormatValue(flag.Value);
                
                sb.Append($"  \"{flag.Key}\": {value}");
                if (!isLast) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private string FormatValue(object value)
        {
            return value switch
            {
                bool b => b.ToString().ToLower(),
                string s => $"\"{s}\"",
                int or double => value.ToString(),
                _ => $"\"{value}\""
            };
        }

        private void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadFlags(_currentDisplayedFlags, "all");
        }

        private void DownloadTrueButton_Click(object sender, RoutedEventArgs e)
        {
            var trueFlags = _flagService.FilterFlags(_currentDisplayedFlags, true);
            DownloadFlags(trueFlags, "true");
        }

        private void DownloadFalseButton_Click(object sender, RoutedEventArgs e)
        {
            var falseFlags = _flagService.FilterFlags(_currentDisplayedFlags, false);
            DownloadFlags(falseFlags, "false");
        }

        private void DownloadFlags(Dictionary<string, object> flags, string type)
        {
            if (!flags.Any())
            {
                StatusText.Text = $"No {type} flags to download.";
                StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"flags_{type}_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var jsonOutput = FormatFlagsAsJson(flags);
                    File.WriteAllText(saveFileDialog.FileName, jsonOutput);
                    
                    StatusText.Text = $"Successfully downloaded {flags.Count} {type} flags to {Path.GetFileName(saveFileDialog.FileName)}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error saving file: {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void SearchInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }




        private void ValidationInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                ValidateButton_Click(sender, e);
            }
        }
    }
}