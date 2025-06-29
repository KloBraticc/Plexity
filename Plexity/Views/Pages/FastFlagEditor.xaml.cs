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



        private bool _showPresets = true;
        private string _searchFilter = string.Empty;
        private string _lastSearch = string.Empty;
        private DateTime _lastSearchTime = DateTime.MinValue;
        private const int _debounceDelay = 70;

        public FastFlagEditor()
        {
            InitializeComponent();
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



        public class FlagHistoryEntry
        {
            public string FlagName { get; set; }
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public DateTime Timestamp { get; set; }
            public override string ToString()
            {
                return $"{Timestamp:HH:mm:ss} - '{FlagName}' changed from '{OldValue}' to '{NewValue}'";
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


        private void FastFinder_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewFlagFinderWindow();
            window.Show();
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