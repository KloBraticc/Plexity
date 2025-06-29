using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Plexity.ViewModels.Pages;

namespace Plexity.Views.Pages
{
    public partial class VersionsPage : Page, INotifyPropertyChanged
    {
        public RobloxVersionsViewModel ViewModel { get; }

        private ObservableCollection<FileSystemItem> _allItems = new();
        public ObservableCollection<FileSystemItem> FilteredItems { get; set; } = new ObservableCollection<FileSystemItem>();

        private readonly string _versionsPath = Paths.Versions;

        public VersionsPage(RobloxVersionsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = this;

            LoadVersionsDirectory();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void LoadVersionsDirectory()
        {
            _allItems.Clear();

            if (!Directory.Exists(_versionsPath))
            {
                DialogService.ShowMessage("Versions folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load folders first
            foreach (var dir in Directory.GetDirectories(_versionsPath))
            {
                var info = new DirectoryInfo(dir);
                _allItems.Add(new FileSystemItem
                {
                    Name = info.Name,
                    IsFolder = true,
                    FullPath = dir,
                    Type = "Folder",
                    Size = FormatSize(GetFolderSize(dir))
                });
            }

            // Then load files
            foreach (var file in Directory.GetFiles(_versionsPath))
            {
                var info = new FileInfo(file);
                _allItems.Add(new FileSystemItem
                {
                    Name = info.Name,
                    IsFolder = false,
                    FullPath = file,
                    Type = info.Extension.TrimStart('.').ToUpperInvariant() + " File",
                    Size = FormatSize(info.Length)
                });
            }

            ApplyFilter();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVersionsDirectory();
        }

        private void ApplyFilter()
        {
            string query = SearchTextBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(query)
                ? _allItems
                : _allItems.Where(i => i.Name.ToLower().Contains(query) || i.Type.ToLower().Contains(query) || i.Size.ToLower().Contains(query));

            FilteredItems.Clear();
            foreach (var item in filtered)
                FilteredItems.Add(item);

            VersionsDataGrid.ItemsSource = FilteredItems;
            TotalItemsTextBlock.Text = $"Items count: {FilteredItems.Count}";
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private long GetFolderSize(string folderPath)
        {
            try
            {
                return Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                                .Sum(f => new FileInfo(f).Length);
            }
            catch
            {
                return 0;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void VersionsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            var editedItem = e.Row.Item as FileSystemItem;
            if (editedItem == null) return;

            var editingElement = e.EditingElement as TextBox;
            if (editingElement == null) return;

            string oldName = editedItem.Name;
            string newName = editingElement.Text.Trim();

            if (string.IsNullOrEmpty(newName) || newName == oldName)
                return;

            string oldFullPath = editedItem.FullPath;
            string newFullPath = Path.Combine(_versionsPath, newName);

            try
            {
                if (editedItem.IsFolder)
                {
                    Directory.Move(oldFullPath, newFullPath);
                }
                else
                {
                    File.Move(oldFullPath, newFullPath);
                }

                // Update properties
                editedItem.Name = newName;
                editedItem.FullPath = newFullPath;

                if (!editedItem.IsFolder)
                {
                    editedItem.Type = Path.GetExtension(newName).TrimStart('.').ToUpperInvariant() + " File";
                    var info = new FileInfo(newFullPath);
                    editedItem.Size = FormatSize(info.Length);
                }
                else
                {
                    editedItem.Type = "Folder";
                    editedItem.Size = FormatSize(GetFolderSize(newFullPath));
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage(
                    $"Failed to rename {oldName}: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                // Reload items to revert changes
                LoadVersionsDirectory();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Adds a new dummy text file
            string baseName = "NewFile";
            int idx = 1;
            string newFilePath;
            do
            {
                newFilePath = Path.Combine(_versionsPath, $"{baseName}{idx}.txt");
                idx++;
            } while (File.Exists(newFilePath) || Directory.Exists(newFilePath));

            try
            {
                File.WriteAllText(newFilePath, "");
                LoadVersionsDirectory();
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Failed to create file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var items = VersionsDataGrid.SelectedItems.Cast<FileSystemItem>().ToList();
            if (items.Count == 0) return;

            bool confirmed = DialogService.ShowConfirm(
                $"Are you sure you want to delete {items.Count} item(s)?",
                "Confirm Delete");

            if (!confirmed) return;  // Only proceed if user clicked Yes

            foreach (var item in items)
            {
                try
                {
                    if (item.IsFolder)
                        Directory.Delete(item.FullPath, true);
                    else
                        File.Delete(item.FullPath);
                }
                catch
                {
                    // Ignore or log errors
                }
            }
            LoadVersionsDirectory();
        }


        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = DialogService.ShowConfirm(
                "Are you sure you want to delete all items?",
                "Confirm Delete All");

            if (!confirmed) return; // Only proceed if user clicked Yes

            foreach (var item in _allItems.ToList())
            {
                try
                {
                    if (item.IsFolder)
                        Directory.Delete(item.FullPath, true);
                    else
                        File.Delete(item.FullPath);
                }
                catch
                {
                    // Ignore or log errors
                }
            }
            LoadVersionsDirectory();
        }


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadVersionsDirectory();
        }

        // Model class for files/folders
        public class FileSystemItem : INotifyPropertyChanged
        {
            private string _name;
            private string _type;
            private string _size;

            public string Name
            {
                get => _name;
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        NotifyPropertyChanged(nameof(Name));
                    }
                }
            }

            public string Type
            {
                get => _type;
                set
                {
                    if (_type != value)
                    {
                        _type = value;
                        NotifyPropertyChanged(nameof(Type));
                    }
                }
            }

            public string Size
            {
                get => _size;
                set
                {
                    if (_size != value)
                    {
                        _size = value;
                        NotifyPropertyChanged(nameof(Size));
                    }
                }
            }

            public bool IsFolder { get; set; }
            public string FullPath { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
