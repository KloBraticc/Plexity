using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using Plexity.Models;
using Plexity.UI;
using Plexity;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Wpf.Ui;
using System.Net.Http;

namespace Plexity.Views.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagEditor.xaml
    /// </summary>
    public partial class FastFlagEditor
    {
        private readonly ObservableCollection<FastFlag> _fastFlagList = new();
        private readonly ObservableCollection<FlagHistoryEntry> _flagHistory = new();
        private Dictionary<string, DateTime> flagTimeAdded = new Dictionary<string, DateTime>();

        // FFlag Finder related fields
        private readonly Dictionary<string, object> _allKnownFlags = new();
        private readonly Dictionary<string, object> _previousFlags = new();
        private readonly Dictionary<string, DateTime> _flagAddedTimestamps = new();
        private DateTime _lastFlagRefresh = DateTime.MinValue;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(24);
        private readonly List<string> _flagUrls = new()
        {
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCClientBootstrapper.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCStudioApp.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-Client-Tracker/refs/heads/roblox/FVariables.txt",
            "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/PCDesktopClient.json",
            "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/FVariablesV2.json",
            "https://clientsettings.roblox.com/v2/settings/application/PCDesktopClient"
        };

        private bool _showPresets = true;
        private string _searchFilter = string.Empty;
        private string _lastSearch = string.Empty;
        private DateTime _lastSearchTime = DateTime.MinValue;
        private const int _debounceDelay = 70;

        public FastFlagEditor()
        {
            InitializeComponent();
            LoadPreviousFlags();
            _ = RefreshFlagDataIfNeeded();
        }

        public class FlagHistoryEntry
        {
            public string FlagName { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public DateTime Timestamp { get; set; }
            public override string ToString()
            {
                return $"{Timestamp:HH:mm:ss} - '{FlagName}' changed from '{OldValue}' to '{NewValue}'";
            }
        }

        private async Task RefreshFlagDataIfNeeded()
        {
            if (DateTime.Now - _lastFlagRefresh > _refreshInterval)
            {
                await RefreshFlagData();
            }
        }

        private async Task RefreshFlagData()
        {
            const string LOG_IDENT = "FastFlagEditor::RefreshFlagData";
            
            try
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Refreshing flag data from remote sources...");
                
                // Store previous flags for comparison
                _previousFlags.Clear();
                foreach (var flag in _allKnownFlags)
                {
                    _previousFlags[flag.Key] = flag.Value;
                }

                _allKnownFlags.Clear();

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                foreach (string url in _flagUrls)
                {
                    try
                    {
                        App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Fetching flags from: {url}");
                        
                        var response = await httpClient.GetStringAsync(url);
                        
                        if (url.Contains("FVariables.txt"))
                        {
                            // Handle FVariables.txt format
                            ParseFVariablesText(response);
                        }
                        else
                        {
                            // Handle JSON format
                            var flags = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                            if (flags != null)
                            {
                                foreach (var flag in flags)
                                {
                                    if (!_allKnownFlags.ContainsKey(flag.Key))
                                    {
                                        _allKnownFlags[flag.Key] = NormalizeValue(flag.Value);
                                        
                                        // Mark new flags with current timestamp
                                        if (!_previousFlags.ContainsKey(flag.Key))
                                        {
                                            _flagAddedTimestamps[flag.Key] = DateTime.Now;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LogLevel.Warning, LOG_IDENT, $"Failed to fetch from {url}: {ex.Message}");
                    }
                }

                _lastFlagRefresh = DateTime.Now;
                SavePreviousFlags();
                UpdateRecentFlags();
                
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Flag data refresh completed. Total flags: {_allKnownFlags.Count}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Error, LOG_IDENT, $"Error refreshing flag data: {ex.Message}");
            }
        }

        private void ParseFVariablesText(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var flagName = parts[0];
                    var flagValue = parts.Length > 1 ? parts[1] : "true";
                    
                    if (!_allKnownFlags.ContainsKey(flagName))
                    {
                        _allKnownFlags[flagName] = NormalizeValue(flagValue);
                        
                        // Mark new flags with current timestamp
                        if (!_previousFlags.ContainsKey(flagName))
                        {
                            _flagAddedTimestamps[flagName] = DateTime.Now;
                        }
                    }
                }
            }
        }

        private object NormalizeValue(object value)
        {
            if (value == null) return "null";
            
            var strValue = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(strValue)) return "\"\"";

            // Check if it's a boolean
            if (bool.TryParse(strValue, out bool boolValue))
                return boolValue.ToString().ToLower();

            // Check if it's a number
            if (int.TryParse(strValue, out int intValue))
                return intValue;

            if (double.TryParse(strValue, out double doubleValue))
                return doubleValue;

            // Default to string
            return $"\"{strValue}\"";
        }

        private void LoadPreviousFlags()
        {
            try
            {
                var flagCachePath = Path.Combine(Paths.Base, "FlagCache.json");
                var timestampCachePath = Path.Combine(Paths.Base, "FlagTimestamps.json");
                
                if (File.Exists(flagCachePath))
                {
                    var content = File.ReadAllText(flagCachePath);
                    var cache = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (cache != null)
                    {
                        foreach (var flag in cache)
                        {
                            _previousFlags[flag.Key] = flag.Value;
                            _allKnownFlags[flag.Key] = flag.Value;
                        }
                    }
                }

                if (File.Exists(timestampCachePath))
                {
                    var content = File.ReadAllText(timestampCachePath);
                    var timestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(content);
                    if (timestamps != null)
                    {
                        foreach (var timestamp in timestamps)
                        {
                            _flagAddedTimestamps[timestamp.Key] = timestamp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Warning, "FastFlagEditor::LoadPreviousFlags", $"Failed to load flag cache: {ex.Message}");
            }
        }

        private void SavePreviousFlags()
        {
            try
            {
                var flagCachePath = Path.Combine(Paths.Base, "FlagCache.json");
                var timestampCachePath = Path.Combine(Paths.Base, "FlagTimestamps.json");
                
                var content = JsonSerializer.Serialize(_allKnownFlags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(flagCachePath, content);

                var timestampContent = JsonSerializer.Serialize(_flagAddedTimestamps, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(timestampCachePath, timestampContent);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Warning, "FastFlagEditor::SavePreviousFlags", $"Failed to save flag cache: {ex.Message}");
            }
        }

        private void UpdateRecentFlags()
        {
            var newFlags = GetLast24HourFlags();

            if (newFlags.Count == 0)
            {
                RecentFlagsText.Text = "No new flags found in the last 24 hours.";
                return;
            }

            ShowAllNewFlags(newFlags);
        }

        private List<KeyValuePair<string, object>> GetLast24HourFlags()
        {
            var recent24Hours = DateTime.Now.AddHours(-24);
            var recentFlags = new List<KeyValuePair<string, object>>();

            foreach (var flag in _allKnownFlags)
            {
                if (_flagAddedTimestamps.ContainsKey(flag.Key) && 
                    _flagAddedTimestamps[flag.Key] >= recent24Hours)
                {
                    recentFlags.Add(flag);
                }
            }

            return recentFlags;
        }

        private void ShowAllNewFlags(List<KeyValuePair<string, object>> newFlags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            
            for (int i = 0; i < Math.Min(newFlags.Count, 50); i++) // Increased to 50
            {
                var flag = newFlags[i];
                var comma = i < Math.Min(newFlags.Count, 50) - 1 ? "," : "";
                sb.AppendLine($"  \"{flag.Key}\": {flag.Value}{comma}");
            }
            
            sb.AppendLine("}");
            
            if (newFlags.Count > 50)
            {
                sb.AppendLine($"\n... and {newFlags.Count - 50} more flags");
            }

            RecentFlagsText.Text = sb.ToString();
        }

        private void ShowTrueFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var newFlags = GetLast24HourFlags();
            var trueFlags = newFlags.Where(f => f.Value.ToString()?.ToLower() == "true").ToList();
            
            if (trueFlags.Count == 0)
            {
                RecentFlagsText.Text = "No new 'true' flags found in the last 24 hours.";
                return;
            }

            ShowFilteredFlags(trueFlags, "true");
        }

        private void ShowFalseFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var newFlags = GetLast24HourFlags();
            var falseFlags = newFlags.Where(f => f.Value.ToString()?.ToLower() == "false").ToList();
            
            if (falseFlags.Count == 0)
            {
                RecentFlagsText.Text = "No new 'false' flags found in the last 24 hours.";
                return;
            }

            ShowFilteredFlags(falseFlags, "false");
        }

        private void ShowAllNewFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var newFlags = GetLast24HourFlags();
            ShowAllNewFlags(newFlags);
        }

        private void ShowFilteredFlags(List<KeyValuePair<string, object>> flags, string filterType)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            
            for (int i = 0; i < Math.Min(flags.Count, 50); i++)
            {
                var flag = flags[i];
                var comma = i < Math.Min(flags.Count, 50) - 1 ? "," : "";
                sb.AppendLine($"  \"{flag.Key}\": {flag.Value}{comma}");
            }
            
            sb.AppendLine("}");
            
            if (flags.Count > 50)
            {
                sb.AppendLine($"\n... and {flags.Count - 50} more '{filterType}' flags");
            }

            RecentFlagsText.Text = sb.ToString();
        }

        private void ValidatorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = ValidatorTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                ValidatorResultText.Text = "Enter flag(s) to validate or import";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                return;
            }

            // Check if it's a bulk import (JSON-like format)
            if (input.Contains("{") || input.Contains("\"") && input.Contains(":"))
            {
                ProcessBulkFlags(input);
            }
            else
            {
                // Single flag validation
                var flagName = input.Trim('"', '\'', ' ', '\t', '\r', '\n');
                ValidateSingleFlag(flagName);
            }
        }

        private void ProcessBulkFlags(string input)
        {
            try
            {
                var flags = ParseBulkFlags(input);
                if (flags.Count == 0)
                {
                    ValidatorResultText.Text = "No valid flags found in input";
                    ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange
                    return;
                }

                var validFlags = new List<KeyValuePair<string, object>>();
                var invalidFlags = new List<string>();
                var flagsWithDefaults = new List<KeyValuePair<string, object>>();

                foreach (var flag in flags)
                {
                    if (_allKnownFlags.ContainsKey(flag.Key))
                    {
                        validFlags.Add(new KeyValuePair<string, object>(flag.Key, _allKnownFlags[flag.Key]));
                    }
                    else
                    {
                        invalidFlags.Add(flag.Key);
                        // Use provided value as default
                        flagsWithDefaults.Add(flag);
                    }
                }

                ShowBulkValidationResult(validFlags, invalidFlags, flagsWithDefaults);
            }
            catch (Exception ex)
            {
                ValidatorResultText.Text = $"Error parsing flags: {ex.Message}";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
        }

        private Dictionary<string, object> ParseBulkFlags(string input)
        {
            var flags = new Dictionary<string, object>();
            
            // Try to parse as JSON first
            try
            {
                input = input.Trim();
                if (!input.StartsWith("{")) input = "{" + input;
                if (!input.EndsWith("}")) input = input + "}";

                var jsonFlags = JsonSerializer.Deserialize<Dictionary<string, object>>(input);
                if (jsonFlags != null)
                {
                    foreach (var flag in jsonFlags)
                    {
                        flags[flag.Key] = NormalizeValue(flag.Value);
                    }
                    return flags;
                }
            }
            catch { }

            // Parse line by line format
            var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim().Trim(',', '"');
                if (string.IsNullOrEmpty(trimmed)) continue;

                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var flagName = trimmed.Substring(0, colonIndex).Trim().Trim('"');
                    var flagValue = trimmed.Substring(colonIndex + 1).Trim().Trim('"', ',');
                    
                    if (!string.IsNullOrEmpty(flagName))
                    {
                        flags[flagName] = NormalizeValue(flagValue);
                    }
                }
            }

            return flags;
        }

        private void ShowBulkValidationResult(List<KeyValuePair<string, object>> validFlags, 
                                            List<string> invalidFlags, 
                                            List<KeyValuePair<string, object>> flagsWithDefaults)
        {
            var result = new StringBuilder();
            
            if (validFlags.Count > 0)
            {
                result.AppendLine($"✓ {validFlags.Count} Valid Flags:");
                result.AppendLine("{");
                for (int i = 0; i < Math.Min(validFlags.Count, 10); i++)
                {
                    var flag = validFlags[i];
                    var comma = i < Math.Min(validFlags.Count, 10) - 1 ? "," : "";
                    result.AppendLine($"  \"{flag.Key}\": {flag.Value}{comma}");
                }
                if (validFlags.Count > 10)
                    result.AppendLine($"  ... and {validFlags.Count - 10} more");
                result.AppendLine("}");
                result.AppendLine();
            }

            if (flagsWithDefaults.Count > 0)
            {
                result.AppendLine($"⚠ {flagsWithDefaults.Count} Flags with Default Values:");
                result.AppendLine("{");
                for (int i = 0; i < Math.Min(flagsWithDefaults.Count, 10); i++)
                {
                    var flag = flagsWithDefaults[i];
                    var comma = i < Math.Min(flagsWithDefaults.Count, 10) - 1 ? "," : "";
                    result.AppendLine($"  \"{flag.Key}\": {flag.Value}{comma}");
                }
                if (flagsWithDefaults.Count > 10)
                    result.AppendLine($"  ... and {flagsWithDefaults.Count - 10} more");
                result.AppendLine("}");
                result.AppendLine();
            }

            if (invalidFlags.Count > 0)
            {
                result.AppendLine($"✗ {invalidFlags.Count} Invalid Flags:");
                result.AppendLine(string.Join(", ", invalidFlags.Take(10)));
                if (invalidFlags.Count > 10)
                    result.AppendLine($"... and {invalidFlags.Count - 10} more");
            }

            ValidatorResultText.Text = result.ToString();
            ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private void ValidateSingleFlag(string flagName)
        {
            if (_allKnownFlags.ContainsKey(flagName))
            {
                var value = _allKnownFlags[flagName];
                ValidatorResultText.Text = $"✓ Valid - Value: {value}";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            }
            else
            {
                ValidatorResultText.Text = "✗ Invalid flag";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
        }

        private async void RefreshValidatorButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshValidatorButton.IsEnabled = false;
            RefreshValidatorButton.Content = "Refreshing...";
            
            try
            {
                await RefreshFlagData();
                ValidatorResultText.Text = $"Data refreshed! {_allKnownFlags.Count} flags loaded.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            }
            catch (Exception ex)
            {
                ValidatorResultText.Text = $"Refresh failed: {ex.Message}";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
            finally
            {
                RefreshValidatorButton.IsEnabled = true;
                RefreshValidatorButton.Content = "Refresh Data";
            }
        }

        private void AddToHistory(string flagName, string? newValue)
        {
            string? oldValue = App.FastFlags.GetValue(flagName);

            var historyEntry = new FlagHistoryEntry
            {
                FlagName = flagName,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = DateTime.Now
            };

            _flagHistory.Add(historyEntry);
        }

        private void ReloadList()
        {
            _fastFlagList.Clear();

            var presetFlags = ClientAppSettings.PresetFlags.Values;

            foreach (var pair in App.FastFlags.Prop.OrderBy(x => x.Key))
            {
                if (!_showPresets && presetFlags.Contains(pair.Key))
                    continue;

                if (!pair.Key.Contains(_searchFilter ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = new FastFlag
                {
                    Name = pair.Key,
                    Value = pair.Value?.ToString() ?? string.Empty
                };

                _fastFlagList.Add(entry);
            }

            if (DataGrid.ItemsSource == null)
                DataGrid.ItemsSource = _fastFlagList;

            UpdateTotalFlagsCount();
        }

        private void UpdateTotalFlagsCount()
        {
            TotalFlagsTextBlock.Text = $"Flags added: {_fastFlagList.Count}";
        }

        private void ClearSearch(bool refresh = true)
        {
            SearchTextBox.Text = "";
            _searchFilter = "";

            if (refresh)
                ReloadList();
        }

        private void AddSingle(string name, string value)
        {
            FastFlag? entry;

            if (App.FastFlags.GetValue(name) is null)
            {
                entry = new FastFlag
                {
                    Name = name,
                    Value = value
                };

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
            }
            else
            {

                bool refresh = false;


                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == name);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);
            UpdateTotalFlagsCount();
        }

        private void ImportJSON(string json)
        {
            Dictionary<string, object>? list = null;

            json = json.Trim();

            // autocorrect where possible
            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');

                if (lastIndex == -1)
                    json += '}';
                else
                    json = json.Substring(0, lastIndex + 1);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                list = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception)
            {
                ShowAddDialog();
                return;
            }

            var conflictingFlags = App.FastFlags.Prop.Where(x => list.ContainsKey(x.Key)).Select(x => x.Key);
            bool overwriteConflicting = false;

            if (conflictingFlags.Any())
            {
                int count = conflictingFlags.Count();

                string message = string.Format(
                    "Strings.Menu_FastFlagEditor_ConflictingImport",
                    count,
                    string.Join(", ", conflictingFlags.Take(25))
                );

                if (count > 25)
                    message += "...";

            }

            foreach (var pair in list)
            {
                if (App.FastFlags.Prop.ContainsKey(pair.Key) && !overwriteConflicting)
                    continue;

                if (pair.Value is null)
                    continue;

                var val = pair.Value.ToString();

                if (val is null)
                    continue;

                App.FastFlags.SetValue(pair.Key, val);
            }

            ClearSearch();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => ReloadList();

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.DataContext is not FastFlag entry)
                return;

            if (e.EditingElement is not TextBox textbox)
                return;

            string newText = textbox.Text;

            switch (e.Column.Header)
            {
                case "Name":
                    string oldName = entry.Name;
                    string newName = newText;

                    if (newName == oldName)
                        return;

                    if (App.FastFlags.GetValue(newName) is not null)
                    {
                        e.Cancel = true;
                        textbox.Text = oldName;
                        return;
                    }

                    // Move timestamp to new name if exists
                    if (flagTimeAdded.ContainsKey(oldName))
                    {
                        flagTimeAdded[newName] = flagTimeAdded[oldName];
                        flagTimeAdded.Remove(oldName);
                    }

                    // Record deletion of old flag
                    AddToHistory(oldName, null);

                    // Rename the flag
                    App.FastFlags.SetValue(oldName, null);
                    App.FastFlags.SetValue(newName, entry.Value);

                    // Record addition of new flag
                    AddToHistory(newName, entry.Value);

                    if (!newName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        ClearSearch();

                    entry.Name = newName;
                    break;

                case "Value":
                    string oldValue = entry.Value;
                    string newValue = newText;

                    if (string.IsNullOrEmpty(oldValue) && !string.IsNullOrEmpty(newValue))
                    {
                        // New flag entry
                        flagTimeAdded[entry.Name] = DateTime.Now;  // record time added
                        AddToHistory(entry.Name, newValue);
                    }
                    else if (oldValue != newValue)
                    {
                        // Update time added on change (optional)
                        flagTimeAdded[entry.Name] = DateTime.Now;
                        AddToHistory(entry.Name, newValue);
                    }

                    App.FastFlags.SetValue(entry.Name, newValue);
                    break;
            }

            UpdateTotalFlagsCount();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(FastFlagsPage));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var tempList = new List<FastFlag>();

            foreach (FastFlag entry in DataGrid.SelectedItems)
                tempList.Add(entry);

            foreach (FastFlag entry in tempList)
            {
                _fastFlagList.Remove(entry);
                App.FastFlags.SetValue(entry.Name, null);
            }

            UpdateTotalFlagsCount();
        }

        private void ExportJSONButton_Click(object sender, RoutedEventArgs e)
        {
            var flags = App.FastFlags.Prop;

            var groupedFlags = flags
                .GroupBy(kvp =>
                {
                    var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                    return match.Success ? match.Value : "Other";
                })
                .OrderBy(g => g.Key);

            var formattedJson = new StringBuilder();
            formattedJson.AppendLine("{");

            int totalItems = flags.Count;
            int writtenItems = 0;
            int groupIndex = 0;

            foreach (var group in groupedFlags)
            {
                if (groupIndex > 0)
                    formattedJson.AppendLine();

                var sortedGroup = group
                    .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                foreach (var kvp in sortedGroup)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                groupIndex++;
            }

            formattedJson.AppendLine("}");

            SaveJSONToFile(formattedJson.ToString());
        }

        private void CopyJSONButton_Click1(object sender, RoutedEventArgs e)
        {
            string json = JsonSerializer.Serialize(App.FastFlags.Prop, new JsonSerializerOptions { WriteIndented = true });
            Clipboard.SetText(json);
        }

        private void CopyJSONButton_Click2(object sender, RoutedEventArgs e)
        {
            var flags = App.FastFlags.Prop;

            var groupedFlags = flags
                .GroupBy(kvp =>
                {
                    var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                    return match.Success ? match.Value : "Other";
                })
                .OrderBy(g => g.Key);

            var formattedJson = new StringBuilder();
            formattedJson.AppendLine("{");

            int totalItems = flags.Count;
            int writtenItems = 0;
            int groupIndex = 0;

            foreach (var group in groupedFlags)
            {
                if (groupIndex > 0)
                    formattedJson.AppendLine();

                var sortedGroup = group
                    .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                foreach (var kvp in sortedGroup)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                groupIndex++;
            }

            formattedJson.AppendLine("}");

            Clipboard.SetText(formattedJson.ToString());
        }

        private void SaveJSONToFile(string json)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt",
                Title = "Save JSON or TXT File",
                FileName = "Plexity.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {

                    var filePath = saveFileDialog.FileName;
                    if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                    {
                        filePath += ".json";
                    }

                    File.WriteAllText(filePath, json);
                    
                }
                catch (IOException)
                {
                    
                }
                catch (UnauthorizedAccessException)
                {
                    
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private void ShowDeleteAllFlagsConfirmation()
        {
            if (!HasFlagsToDelete())
            {
                ShowInfoMessage("There are no flags to delete.");
                return;
            }

            var dialog = new ConfirmDialog("Are you sure you want to delete all flags?");
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();

            if (dialog.IsConfirmed)
            {
                DeleteAllFlags();
                ReloadUI();
            }
        }

        private void ShowAddDialog()
        {
            var dialog = new AddFlagWindow();
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.FlagTabs.SelectedIndex == 0)
                AddSingle(dialog.FlagNameTextBox.Text.Trim(), dialog.FlagValueTextBox.Text);
            else if (dialog.FlagTabs.SelectedIndex == 1)
                ImportJSON(dialog.JsonTextBox.Text);
        }

        private bool HasFlagsToDelete()
        {
            return _fastFlagList.Any() || App.FastFlags.Prop.Any();
        }

        private void DeleteAllFlags()
        {

            _fastFlagList.Clear();


            foreach (var key in App.FastFlags.Prop.Keys.ToList())
            {
                App.FastFlags.SetValue(key, null);
            }
        }

        private void ReloadUI()
        {
            ReloadList();
        }

        private void ShowInfoMessage(string message)
        {
        }

        private void HandleError(Exception ex)
        {
            // Display and log the error message
            LogError(ex); // Logging error in a centralized method
        }

        private void LogError(Exception ex)
        {
            // Detailed logging for developers
            Console.WriteLine(ex.ToString());
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) => ShowAddDialog();
        private void DeleteAllButton_Click(object sender, RoutedEventArgs e) => ShowDeleteAllFlagsConfirmation();

        private CancellationTokenSource? _searchCancellationTokenSource;

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            string newSearch = textbox.Text.Trim();

            if (newSearch == _lastSearch && (DateTime.Now - _lastSearchTime).TotalMilliseconds < _debounceDelay)
                return;

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            _searchFilter = newSearch;
            _lastSearch = newSearch;
            _lastSearchTime = DateTime.Now;

            try
            {
                await Task.Delay(_debounceDelay, _searchCancellationTokenSource.Token);

                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                Dispatcher.Invoke(() =>
                {
                    ReloadList();
                });
            }
            catch (TaskCanceledException)
            {
            }
        }

    }
}