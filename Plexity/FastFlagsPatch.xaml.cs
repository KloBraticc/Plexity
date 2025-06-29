using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Plexity
{
    public partial class FastFlagsPatch : Window
    {
        private Dictionary<string, object> _allKnownFlags = new();
        private Dictionary<string, object> _previousFlags = new();
        private Dictionary<string, DateTime> _flagAddedTimestamps = new();

        private ObservableCollection<FlagInfo> _flagInfoList = new ObservableCollection<FlagInfo>();

        public FastFlagsPatch(string message = null, bool isMessageOnly = false)
        {
            InitializeComponent();

            FlagsDataGrid.ItemsSource = _flagInfoList;

            LoadPreviousFlags();
            UpdateRecentFlags();
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

            if (bool.TryParse(strValue, out bool boolValue))
                return boolValue.ToString().ToLower();

            if (int.TryParse(strValue, out int intValue))
                return intValue;

            if (double.TryParse(strValue, out double doubleValue))
                return doubleValue;

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
                Console.WriteLine($"Warning: Failed to load flag cache: {ex.Message}");
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
                Console.WriteLine($"Warning: Failed to save flag cache: {ex.Message}");
            }
        }

        private void UpdateRecentFlags()
        {
            var newFlags = GetLast24HourFlags();

            if (newFlags.Count == 0)
            {
                _flagInfoList.Clear();
                ValidatorResultText.Text = "No new flags found in the last 24 hours.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                return;
            }

            ShowFlags(newFlags, "Recent Flags");
        }

        private Dictionary<string, object> GetLast24HourFlags()
        {
            var recent24Hours = DateTime.Now.AddHours(-24);
            var recentFlags = new Dictionary<string, object>();

            foreach (var flag in _allKnownFlags)
            {
                if (_flagAddedTimestamps.ContainsKey(flag.Key) &&
                    _flagAddedTimestamps[flag.Key] >= recent24Hours)
                {
                    recentFlags[flag.Key] = flag.Value;
                }
            }

            return recentFlags;
        }

        private void ShowFlags(Dictionary<string, object> flags, string header)
        {
            Dispatcher.Invoke(() =>
            {
                _flagInfoList.Clear();

                var sortedFlags = new List<(string Key, object Value)>();
                foreach (var kvp in flags)
                    sortedFlags.Add((kvp.Key, kvp.Value));
                sortedFlags.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

                int count = Math.Min(sortedFlags.Count, 100);
                for (int i = 0; i < count; i++)
                {
                    var flag = sortedFlags[i];
                    DateTime ts = _flagAddedTimestamps.ContainsKey(flag.Key) ? _flagAddedTimestamps[flag.Key] : DateTime.MinValue;

                    _flagInfoList.Add(new FlagInfo
                    {
                        Key = flag.Key,
                        Value = FormatFlagValue(flag.Value),
                        AddedTimestamp = ts
                    });
                }
            });
        }

        private string FormatFlagValue(object value)
        {
            if (value == null) return "null";
            var str = value.ToString();
            if (str.StartsWith("\"") && str.EndsWith("\"") && str.Length > 1)
                return str.Trim('"');
            return str;
        }

        private void ShowTrueFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var trueFlags = GetLast24HourFlags().Where(f => f.Value.ToString()?.ToLower() == "true")
                                                .ToDictionary(k => k.Key, v => v.Value);

            if (trueFlags.Count == 0)
            {
                _flagInfoList.Clear();
                ValidatorResultText.Text = "No new 'true' flags found in the last 24 hours.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                return;
            }

            ShowFlags(trueFlags, "'true' Flags");
        }

        private void ShowFalseFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var falseFlags = GetLast24HourFlags().Where(f => f.Value.ToString()?.ToLower() == "false")
                                                 .ToDictionary(k => k.Key, v => v.Value);

            if (falseFlags.Count == 0)
            {
                _flagInfoList.Clear();
                ValidatorResultText.Text = "No new 'false' flags found in the last 24 hours.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                return;
            }

            ShowFlags(falseFlags, "'false' Flags");
        }

        private void ShowAllNewFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            var newFlags = GetLast24HourFlags();

            if (newFlags.Count == 0)
            {
                _flagInfoList.Clear();
                ValidatorResultText.Text = "No new flags found in the last 24 hours.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                return;
            }

            ShowFlags(newFlags, "All New Flags");
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

            if ((input.Contains("{") || input.Contains("\"")) && input.Contains(":"))
            {
                ProcessBulkFlags(input);
            }
            else
            {
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
                    ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
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
            var dict = new Dictionary<string, object>();

            input = input.Trim();
            if (input.StartsWith("{") && input.EndsWith("}"))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(input);
                    foreach (var property in jsonDoc.RootElement.EnumerateObject())
                    {
                        dict[property.Name] = property.Value.ToString();
                    }
                }
                catch
                {
                    var lines = input.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim(' ', '"', '\'');
                            var val = parts[1].Trim(' ', '"', '\'');
                            dict[key] = val;
                        }
                    }
                }
            }
            else
            {
                var parts = input.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var p = part.Split('=', 2);
                    if (p.Length == 2)
                    {
                        dict[p[0].Trim()] = p[1].Trim();
                    }
                }
            }

            return dict;
        }

        private void ShowBulkValidationResult(
            List<KeyValuePair<string, object>> validFlags,
            List<string> invalidFlags,
            List<KeyValuePair<string, object>> defaults)
        {
            var sb = new StringBuilder();

            if (validFlags.Count > 0)
            {
                sb.AppendLine("Known Flags:");
                foreach (var flag in validFlags)
                {
                    sb.AppendLine($"- {flag.Key} = {flag.Value}");
                }
            }

            if (invalidFlags.Count > 0)
            {
                sb.AppendLine("\nUnknown Flags (defaults applied):");
                foreach (var flag in invalidFlags)
                {
                    var defVal = defaults.FirstOrDefault(f => f.Key == flag).Value ?? "N/A";
                    sb.AppendLine($"- {flag} = {defVal}");
                }
            }

            ValidatorResultText.Text = sb.ToString();
            ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 0));
        }

        private void ValidateSingleFlag(string flagName)
        {
            if (_allKnownFlags.ContainsKey(flagName))
            {
                var value = _allKnownFlags[flagName];
                ValidatorResultText.Text = $"Flag '{flagName}' exists with value: {FormatFlagValue(value)}";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 0));
            }
            else
            {
                ValidatorResultText.Text = $"Flag '{flagName}' is unknown.";
                ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
        }

        private void RefreshValidatorButton_Click(object sender, RoutedEventArgs e)
        {
            SavePreviousFlags();
            UpdateRecentFlags();

            ValidatorResultText.Text = "Validator refreshed.";
            ValidatorResultText.Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 0));
        }
    }

    public class FlagInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime AddedTimestamp { get; set; }
    }
}
