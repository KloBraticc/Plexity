using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Plexity.Models;
using Plexity.ViewModels.Pages;
using System.Windows.Markup;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Plexity.Views.Pages
{
    public partial class PluginsPage : Page
    {
        private readonly PluginsViewModel _viewModel;
        public static ObservableCollection<PluginItem> PluginsList { get; private set; } = new ObservableCollection<PluginItem>();
        public ObservableCollection<PluginItem> Plugins { get; set; }

        public PluginsPage()
        {
            InitializeComponent();
            _viewModel = new PluginsViewModel();
            DataContext = _viewModel;
            Plugins = PluginsList;
            _ = LoadPluginsAsync();
        }

        private async Task LoadPluginsAsync()
        {
            _viewModel.IsLoading = true;
            await _viewModel.LoadPluginsAsync();
            _viewModel.IsLoading = false;
        }

        private async void RefreshPlugins_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.IsLoading = true;
                await _viewModel.LoadPluginsAsync();
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }



        private void InstallPlugin_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PluginItem plugin)
            {
                _ = _viewModel.InstallPlugin(plugin);
            }
        }

        /// <summary>
        /// Loads plugin window from DLL.
        /// </summary>
        public void LaunchPluginFromDll(string pluginDllPath, string windowClassFullName)
        {
            try
            {
                if (!File.Exists(pluginDllPath))
                {
                    MessageBox.Show($"Plugin DLL not found: {pluginDllPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var assembly = Assembly.LoadFrom(pluginDllPath);

                var windowType = assembly.GetType(windowClassFullName);
                if (windowType == null)
                {
                    MessageBox.Show($"Window class '{windowClassFullName}' not found in plugin DLL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Activator.CreateInstance(windowType) is Window window)
                {
                    window.Show();
                }
                else
                {
                    MessageBox.Show($"Type '{windowClassFullName}' is not a Window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load plugin window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Loads plugin window from raw XAML file + code-behind (.cs).
        /// </summary>
        public void LaunchPluginWindow(string pluginFolder)
        {
            try
            {
                var xamlFile = Directory.GetFiles(pluginFolder, "*.xaml")
                    .FirstOrDefault(f => IsWindowXaml(f));
                if (xamlFile == null)
                {
                    MessageBox.Show("No Window XAML file found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Window window;
                using (var fs = File.OpenRead(xamlFile))
                {
                    window = (Window)XamlReader.Load(fs);
                }

                string csFile = Path.ChangeExtension(xamlFile, ".cs");
                if (!File.Exists(csFile))
                {
                    MessageBox.Show("Plugin code-behind file (.cs) missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var assembly = CompileCodeBehind(csFile);
                if (assembly == null)
                {
                    MessageBox.Show("Compilation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var logicType = assembly.GetTypes()
                    .FirstOrDefault(t =>
                        t.IsClass &&
                        t.GetConstructor(new[] { typeof(Window) }) != null);



                if (logicType == null)
                {
                    MessageBox.Show("Logic class not found in compiled code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Activator.CreateInstance(logicType, window);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching plugin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Assembly CompileCodeBehind(string csFilePath)
        {
            string code = File.ReadAllText(csFilePath);

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            string assemblyName = Path.GetRandomFileName();

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                string errors = string.Join(Environment.NewLine, failures.Select(f => $"{f.Id}: {f.GetMessage()}"));
                MessageBox.Show($"Compilation errors:\n{errors}", "Compilation Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }


        /// <summary>
        /// User clicks "Launch" button for a plugin.
        /// Loads plugin window using XAML + code-behind compilation.
        /// </summary>
        private void LaunchPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PluginItem plugin)
            {
                string pluginFolder = Path.Combine(Paths.Plugins, plugin.Name);

                string xamlFile = Directory.GetFiles(pluginFolder, "*.xaml").FirstOrDefault();
                string csFile = xamlFile != null ? Path.ChangeExtension(xamlFile, ".cs") : null;

                if (xamlFile == null || csFile == null || !File.Exists(csFile))
                {
                    MessageBox.Show("Plugin XAML or code-behind file is missing or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    LaunchPluginWindow(pluginFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load plugin window:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemovePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PluginItem plugin)
            {
                string pluginFolder = Path.Combine(Paths.Plugins, plugin.Name);

                if (!Directory.Exists(pluginFolder))
                {
                    MessageBox.Show($"This Plugin is not Installed.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    return; // Exit early if folder doesn't exist
                }

                var result = MessageBox.Show($"Are you sure you want to remove '{plugin.Name}'?", "Confirm Removal", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.Delete(pluginFolder, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete plugin files:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void CreatePlugins_Click(object sender, RoutedEventArgs e)
        {
            var pluginCreateWindow = new PluginCreate
            {
                Owner = Window.GetWindow(this)
            };
            pluginCreateWindow.ShowDialog();
        }


        /// <summary>
        /// Checks if the XAML file's root element is a Window.
        /// </summary>
        private bool IsWindowXaml(string xamlFilePath)
        {
            try
            {
                using var fs = File.OpenRead(xamlFilePath);
                using var reader = XmlReader.Create(fs);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return string.Equals(reader.Name, "Window", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading XAML file '{xamlFilePath}': {ex.Message}");
            }

            return false;
        }
    }
}
