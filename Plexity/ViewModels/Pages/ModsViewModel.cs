using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Plexity.AppData;
using Plexity.Models.SettingTasks;
using Plexity.UI.ViewModels.Bootstrapper;
using System.Threading;


namespace Plexity.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel, INotifyPropertyChanged
    {
        private static readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[] { 0x74, 0x74, 0x63, 0x66 } }
        };

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);
        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);
        public ICommand AddCustomCursorModCommand => new RelayCommand(AddCustomCursorMod);
        public ICommand RemoveCustomCursorModCommand => new RelayCommand(RemoveCustomCursorMod);

        public FontModPresetTask TextFontTask { get; } = new();



        public Visibility ChooseCustomFontVisibility =>
            string.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility DeleteCustomFontVisibility =>
            string.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ChooseCustomCursorVisibility
        {
            get
            {
                string targetDir = Path.Combine(Paths.Mods, "content", "textures", "Cursors", "KeyboardMouse");
                string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };
                bool anyExist = cursorNames.Any(name => File.Exists(Path.Combine(targetDir, name)));
                return anyExist ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility DeleteCustomCursorVisibility
        {
            get
            {
                string targetDir = Path.Combine(Paths.Mods, "content", "textures", "Cursors", "KeyboardMouse");
                string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };
                bool anyExist = cursorNames.Any(name => File.Exists(Path.Combine(targetDir, name)));
                return anyExist ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ManageCustomFont()
        {
            if (!string.IsNullOrEmpty(TextFontTask.NewState))
            {
                // Remove the custom font
                TextFontTask.NewState = string.Empty;
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Font files|*.ttf;*.otf;*.ttc",
                    Title = "Select a Custom Font"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string ext = Path.GetExtension(dialog.FileName).TrimStart('.').ToLowerInvariant();

                if (!FontHeaders.TryGetValue(ext, out var expectedHeader))
                {
                    DialogService.ShowMessage("The selected font file is invalid or unsupported.", "Invalid Font", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] fileHeader;
                try
                {
                    fileHeader = File.ReadAllBytes(dialog.FileName).Take(4).ToArray();
                }
                catch (Exception)
                {
                    DialogService.ShowMessage("Failed to read the font file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!expectedHeader.SequenceEqual(fileHeader))
                {
                    DialogService.ShowMessage("The selected font file is invalid or unsupported.", "Invalid Font", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public void AddCustomCursorMod()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PNG Images (*.png)|*.png",
                Title = "Select a PNG Cursor Image"
            };

            if (dialog.ShowDialog() != true)
                return;

            string sourcePath = dialog.FileName;
            string targetDir = Path.Combine(Paths.Mods, "content", "textures", "Cursors", "KeyboardMouse");
            Directory.CreateDirectory(targetDir);

            string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };

            try
            {
                foreach (var name in cursorNames)
                {
                    string destPath = Path.Combine(targetDir, name);
                    File.Copy(sourcePath, destPath, overwrite: true);
                }
            }
            catch (Exception)
            {
                DialogService.ShowMessage("Failed to add Cursor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        public void RemoveCustomCursorMod()
        {
            string targetDir = Path.Combine(Paths.Mods, "content", "textures", "Cursors", "KeyboardMouse");
            string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };

            bool anyDeleted = false;
            foreach (var name in cursorNames)
            {
                string filePath = Path.Combine(targetDir, name);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        anyDeleted = true;
                    }
                    catch (Exception)
                    {
                        DialogService.ShowMessage("Failed to remove cursor file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            if (!anyDeleted)
                DialogService.ShowMessage("No custom cursors found to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                DialogService.ShowMessage("Roblox is not installed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        internal static class PInvoke
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern bool SHObjectProperties(IntPtr hwnd, SHOP_TYPE shopType, string pszObjectName, string pszPropertyPage);
        }
        internal enum SHOP_TYPE : uint
        {
            SHOP_PRINTERNAME = 0x1,
            SHOP_FILEPATH = 0x2,
            SHOP_VOLUMEGUID = 0x4
        }

        internal static class HWND
        {
            public static readonly IntPtr Null = IntPtr.Zero;
        }
    }
}
