using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Plexity.Models;

namespace Plexity.ViewModels.Pages
{
    public class PluginsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PluginItem> Plugins { get; } = new();

        private readonly HttpClient _http = new();

        public PluginsViewModel()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("PlexityApp");
        }

        public async Task LoadPluginsAsync()
        {
            Plugins.Clear();

            const string repoApiUrl = "https://api.github.com/repos/KloBraticc/PluginsPagePlexity/contents";
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var json = await _http.GetStringAsync(repoApiUrl);
                var files = JsonSerializer.Deserialize<GitHubContent[]>(json, jsonOptions);
                var pluginFiles = files?.Where(f => f.Name.EndsWith(".json")).ToArray();
                if (pluginFiles == null) return;

                var metadatas = await Task.WhenAll(pluginFiles.Select(f => LoadPluginMetadataAsync(f, jsonOptions)));

                for (int i = 0; i < pluginFiles.Length; i++)
                {
                    var meta = metadatas[i];
                    var file = pluginFiles[i];
                    if (meta != null)
                    {
                        Plugins.Add(new PluginItem
                        {
                            Name = meta.Name,
                            Description = meta.Description,
                            IconUrl = meta.IconUrl,
                            FileName = file.Name
                        });
                    }
                }

                await Task.WhenAll(Plugins.Select(p => LoadIconAsync(p)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Plugin load error: " + ex.Message);
            }
        }

        private async Task<PluginMetadata?> LoadPluginMetadataAsync(GitHubContent file, JsonSerializerOptions options)
        {
            try
            {
                var fileJson = await _http.GetStringAsync(file.DownloadUrl);
                return JsonSerializer.Deserialize<PluginMetadata>(fileJson, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin metadata from {file.Name}: {ex.Message}");
                return null;
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }



        private async Task LoadIconAsync(PluginItem plugin)
        {
            if (string.IsNullOrWhiteSpace(plugin.IconUrl)) return;

            try
            {
                var bytes = await _http.GetByteArrayAsync(plugin.IconUrl);
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                App.Current.Dispatcher.Invoke(() => plugin.Icon = bitmap);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load icon for plugin {plugin.Name}: {ex.Message}");
            }
        }

        public async Task InstallPlugin(PluginItem plugin)
        {
            if (plugin == null || string.IsNullOrWhiteSpace(plugin.FileName))
                return;

            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    plugin.IsInstalling = true;
                    plugin.StatusMessage = "Installing...";
                });

                string pluginFolder = Path.Combine(Paths.Plugins, plugin.Name);
                Directory.CreateDirectory(pluginFolder);

                // Download the entire plugin JSON manifest file
                string downloadUrl = $"https://raw.githubusercontent.com/KloBraticc/PluginsPagePlexity/main/{plugin.FileName}";

                string pluginJson = await _http.GetStringAsync(downloadUrl);

                // Save JSON manifest to file (optional)
                string jsonFilePath = Path.Combine(pluginFolder, plugin.FileName);
                await File.WriteAllTextAsync(jsonFilePath, pluginJson);

                // Deserialize the plugin metadata
                var pluginData = JsonSerializer.Deserialize<PluginMetadata>(pluginJson);

                if (pluginData?.Code == null || pluginData.Code.Count == 0)
                {
                    UpdatePluginStatus(plugin, "No code URLs found in plugin JSON");
                    return;
                }

                foreach (var codeUrl in pluginData.Code)
                {
                    try
                    {
                        var uri = new Uri(codeUrl);
                        string fileName = Path.GetFileName(uri.LocalPath);

                        var fileBytes = await _http.GetByteArrayAsync(codeUrl);

                        string filePath = Path.Combine(pluginFolder, fileName);
                        await File.WriteAllBytesAsync(filePath, fileBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to download or save file from {codeUrl}: {ex.Message}");
                    }
                }

                UpdatePluginStatus(plugin, "Installed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InstallPlugin error: {ex.Message}");
                UpdatePluginStatus(plugin, "Failed");
            }
            finally
            {
                App.Current.Dispatcher.Invoke(() => plugin.IsInstalling = false);
            }
        }


        private async void UpdatePluginStatus(PluginItem plugin, string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                plugin.StatusMessage = message;
            });

            if (message == "Installed!")
            {
                await Task.Delay(3000);
                App.Current.Dispatcher.Invoke(() =>
                {
                    plugin.StatusMessage = string.Empty;
                });
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
