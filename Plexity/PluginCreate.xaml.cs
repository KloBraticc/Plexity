using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Windows.Media;
using Plexity.Models;
using Plexity.Views.Pages;


namespace Plexity
{
    public partial class PluginCreate : Window
    {
        private string _xamlCode = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""Test Plugin"" Height=""200"" Width=""300"">
  <Grid>
    <Button Name=""MyButton"" Content=""Click me!"" Width=""100"" Height=""30""/>
  </Grid>
</Window>";

        private string _csCode = @"using System.Windows;
using System.Windows.Controls;

public class TestPluginLogic
{
    public TestPluginLogic(Window window)
    {
        var button = (Button)((Grid)window.Content).FindName(""MyButton"");
        if (button != null)
        {
            button.Click += (s, e) => MessageBox.Show(""Button clicked from plugin!"");
        }
    }
}";


        // This Suggestions thingy is taken from a Github I didnt make it But I did Modify it a TUN k..
private readonly List<string> _suggestions = new()
{
    // XAML Controls & Panels
    "Button", "TextBlock", "TextBox", "PasswordBox", "CheckBox", "RadioButton",
    "ComboBox", "ListBox", "ListView", "GridView", "TreeView", "TreeViewItem",
    "Menu", "MenuItem", "ToolBar", "StatusBar", "ScrollViewer", "Slider",
    "ProgressBar", "TabControl", "TabItem", "StackPanel", "WrapPanel",
    "DockPanel", "UniformGrid", "GridSplitter", "Grid", "Canvas", "Border",
    "Viewbox", "ScrollBar", "Image", "MediaElement", "InkCanvas",
    "Popup", "ToolTip", "UserControl", "Page", "Window", "Frame",
    "ContentControl", "ItemsControl", "DataGrid", "Expander", "GroupBox",
    "Calendar", "DatePicker", "TimePicker", "FlipView", "Hub", "Pivot",
    "MapControl", "WebView", "SplitView", "NavigationView", "PersonPicture",
    "MediaPlayerElement", "RichEditBox", "Flyout", "TeachingTip", "CommandBar",

    // Common Properties
    "Name", "x:Name", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
    "Margin", "Padding", "HorizontalAlignment", "VerticalAlignment", "Visibility",
    "Background", "Foreground", "BorderBrush", "BorderThickness", "CornerRadius",
    "FontSize", "FontFamily", "FontWeight", "FontStyle", "FontStretch",
    "TextAlignment", "TextWrapping", "TextDecorations", "LineHeight",
    "Opacity", "IsEnabled", "IsHitTestVisible", "IsReadOnly", "IsChecked",
    "IsSelected", "SelectedIndex", "SelectedItem", "TabIndex", "ToolTip",
    "Cursor", "FocusVisualStyle", "Clip", "RenderTransform", "RenderTransformOrigin",
    "FlowDirection", "UseLayoutRounding", "SnapsToDevicePixels", "Language",
    "InputScope", "CharacterSpacing", "IsTabStop", "TextTrimming", "Header",

    // Layout-specific Attached Properties
    "Grid.Row", "Grid.Column", "Grid.RowSpan", "Grid.ColumnSpan",
    "DockPanel.Dock", "Canvas.Left", "Canvas.Top", "Canvas.Right", "Canvas.Bottom",
    "ScrollViewer.HorizontalScrollBarVisibility", "ScrollViewer.VerticalScrollBarVisibility",
    "Panel.ZIndex", "Grid.IsSharedSizeScope", "Canvas.ZIndex",
    "KeyboardNavigation.TabNavigation", "KeyboardNavigation.ControlTabNavigation",
    "VirtualizingStackPanel.IsVirtualizing", "ScrollViewer.CanContentScroll",
    "ScrollViewer.PanningMode", "Grid.IsSharedSizeScope",

    // Data Binding & Commands
    "DataContext", "Binding", "RelativeSource", "ElementName", "Path",
    "Command", "CommandParameter", "UpdateSourceTrigger", "NotifyOnValidationError",
    "ValidatesOnExceptions", "ValidatesOnDataErrors", "Mode", "Converter",
    "ConverterParameter", "FallbackValue", "TargetNullValue", "x:Bind",
    "x:DataType", "OneWay", "TwoWay", "OneTime", "OneWayToSource",

    // Events
    "Click", "Checked", "Unchecked", "SelectionChanged", "TextChanged",
    "MouseEnter", "MouseLeave", "MouseDown", "MouseUp", "MouseMove",
    "KeyDown", "KeyUp", "GotFocus", "LostFocus", "Loaded", "Unloaded",
    "DragEnter", "DragLeave", "Drop", "Tapped", "DoubleTapped", "RightTapped",
    "PointerEntered", "PointerExited", "PointerPressed", "PointerReleased",
    "ManipulationStarted", "ManipulationDelta", "ManipulationCompleted",
    "ValueChanged", "DropDownOpened", "DropDownClosed",

    // Animation & Visual State
    "Storyboard", "BeginStoryboard", "VisualStateManager", "VisualState",
    "VisualTransition", "Duration", "RepeatBehavior", "AutoReverse",
    "EasingFunction", "DoubleAnimation", "ObjectAnimationUsingKeyFrames",
    "ColorAnimation", "KeyTime", "SplineDoubleKeyFrame", "DiscreteObjectKeyFrame",

    // Special/XAML keywords
    "x:Class", "x:Key", "x:Type", "x:Static", "x:Bind", "x:DataType",
    "x:Uid", "x:FieldModifier", "x:Shared", "xmlns", "xmlns:x",
    "mc:Ignorable", "d:DesignHeight", "d:DesignWidth", "d:DataContext",

    // Styles, Templates, Resources
    "Style", "Template", "ControlTemplate", "DataTemplate", "ItemsPanelTemplate",
    "Triggers", "Setters", "Setter", "Resources", "ResourceDictionary",
    "BasedOn", "StaticResource", "DynamicResource", "ThemeResource", "MergedDictionaries",

    // Triggers & Behaviors
    "EventTrigger", "DataTrigger", "MultiTrigger", "Trigger", "Storyboard",
    "BeginStoryboard", "Interaction.Behaviors", "Behavior", "Action", "InvokeCommandAction",

    // Miscellaneous
    "AutomationProperties.Name", "ToolTipService.ToolTip", "InputBindings",
    "CommandBindings", "NavigationCacheMode", "Tag", "Uid",
    "AutomationProperties.AutomationId", "AutomationProperties.HelpText",

    // C# Keywords & Constructs
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
    "char", "checked", "class", "const", "continue", "decimal", "default",
    "delegate", "do", "double", "else", "enum", "event", "explicit",
    "extern", "false", "finally", "fixed", "float", "for", "foreach",
    "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
    "lock", "long", "namespace", "new", "null", "object", "operator",
    "out", "override", "params", "private", "protected", "public",
    "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
    "stackalloc", "static", "string", "struct", "switch", "this", "throw",
    "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
    "ushort", "using", "virtual", "void", "volatile", "while",

    // Contextual keywords
    "async", "await", "var", "dynamic", "yield", "partial", "nameof",

    // Preprocessor directives
    "#define", "#undef", "#if", "#elif", "#else", "#endif",
    "#line", "#error", "#warning", "#region", "#endregion", "#pragma",

    // Common .NET types & aliases
    "Int32", "Int64", "UInt32", "UInt64", "Single", "Double", "Decimal",
    "Boolean", "String", "Char", "Object", "Void", "DateTime", "Guid",
    "Task", "ValueTask", "Action", "Func", "Predicate", "CancellationToken",
    "Exception", "ArgumentException", "InvalidOperationException",
    "IDisposable", "IEnumerable", "IEnumerator",

    // Common Attributes
    "Serializable", "Obsolete", "DllImport", "StructLayout", "Flags",
    "DebuggerStepThrough", "CompilerGenerated", "Conditional", "Attribute",

    // Other useful modifiers & keywords
    "extern", "unsafe", "fixed"
};

        private Window _currentPluginWindow = null;
        public PluginCreate()
        {
            InitializeComponent();
            EditorModeComboBox.SelectionChanged += EditorModeComboBox_SelectionChanged;
            LoadLatestPlugin();
            CodeLabel.Text = "Plugin XAML Code:";
            CodeTextBox.Text = _xamlCode;
            CompositionTarget.Rendering += OnRendering;
            UpdateLineNumbers();
        }

        private void LoadLatestPlugin()
        {
            try
            {
                string createdPluginDir = Path.Combine(Paths.Plugins, "CreatedPlugin");
                if (!Directory.Exists(createdPluginDir))
                    return;

                var xamlFiles = Directory.GetFiles(createdPluginDir, "Plugin_*.xaml");
                var csFiles = Directory.GetFiles(createdPluginDir, "Plugin_*.cs");

                if (xamlFiles.Length == 0 || csFiles.Length == 0)
                    return;

                string latestXamlFile = xamlFiles.OrderByDescending(f => f).First();
                string latestCsFile = csFiles.OrderByDescending(f => f).First();

                _xamlCode = File.ReadAllText(latestXamlFile);
                _csCode = File.ReadAllText(latestCsFile);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessage($"Failed to load latest plugin:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (SuggestionPopup.IsOpen)
                UpdateSuggestionPopupPosition();
        }



        private void EditorModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CodeTextBox == null || CodeLabel == null) return;

            if (EditorModeComboBox.SelectedIndex == 0)
            {
                _csCode = CodeTextBox.Text;
                CodeLabel.Text = "Plugin XAML Code:";
                CodeTextBox.Text = _xamlCode;
            }
            else if (EditorModeComboBox.SelectedIndex == 1)
            {
                _xamlCode = CodeTextBox.Text;
                CodeLabel.Text = "Plugin Code-Behind (C#):";
                CodeTextBox.Text = _csCode;
            }

            UpdateLineNumbers();
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            ShowAutocomplete();
            if (SuggestionPopup.IsOpen)
                UpdateSuggestionPopupPosition();
        }


        private void ShowAutocomplete()
        {
            string text = CodeTextBox.Text;
            int caretIndex = CodeTextBox.CaretIndex;
            string currentWord = GetCurrentWord(text, caretIndex);

            if (string.IsNullOrWhiteSpace(currentWord) || currentWord.Length < 2)
            {
                SuggestionPopup.IsOpen = false;
                return;
            }

            var matches = _suggestions
                .Where(s => s.StartsWith(currentWord, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (matches.Any())
            {
                SuggestionList.ItemsSource = matches;
                SuggestionList.SelectedIndex = 0;
                SuggestionPopup.IsOpen = true;
                UpdateSuggestionPopupPosition();
            }
            else
            {
                SuggestionPopup.IsOpen = false;
            }
        }

        private string GetCurrentWord(string text, int index)
        {
            int start = index - 1;
            while (start >= 0 && !char.IsWhiteSpace(text[start]) && text[start] != '\n')
                start--;
            start++;
            int length = index - start;
            return (start >= 0 && length > 0 && text.Length >= start + length)
                ? text.Substring(start, length)
                : "";
        }

        private void InsertSuggestion(string suggestion)
        {
            int caretIndex = CodeTextBox.CaretIndex;
            string text = CodeTextBox.Text;

            int start = caretIndex - 1;
            while (start >= 0 && !char.IsWhiteSpace(text[start]) && text[start] != '\n')
                start--;
            start++;

            CodeTextBox.Text = text.Substring(0, start) + suggestion + text.Substring(caretIndex);
            CodeTextBox.CaretIndex = start + suggestion.Length;
            SuggestionPopup.IsOpen = false;
        }

        private void CodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (SuggestionPopup.IsOpen)
            {
                if (e.Key == Key.Down)
                {
                    SuggestionList.Focus();
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    if (SuggestionList.SelectedItem is string suggestion)
                    {
                        InsertSuggestion(suggestion);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    SuggestionPopup.IsOpen = false;
                    e.Handled = true;
                }
                else
                {
                    UpdateSuggestionPopupPosition();
                }
            }
        }


        private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionList.SelectedItem is string suggestion)
                InsertSuggestion(suggestion);
        }

        private void SuggestionList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (SuggestionList.SelectedItem is string suggestion)
                {
                    InsertSuggestion(suggestion);
                    CodeTextBox.Focus();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                SuggestionPopup.IsOpen = false;
                CodeTextBox.Focus();
                e.Handled = true;
            }
        }
        private void UpdateSuggestionPopupPosition()
        {
            if (!SuggestionPopup.IsOpen || CodeTextBox == null) return;

            int caretIndex = CodeTextBox.CaretIndex;
            Rect charRect = CodeTextBox.GetRectFromCharacterIndex(caretIndex, true);

            if (charRect == Rect.Empty || charRect.Height == 0)
            {
                charRect = new Rect(0, CodeTextBox.ActualHeight, 0, 0);
            }

            SuggestionPopup.PlacementTarget = CodeTextBox;
            SuggestionPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            SuggestionPopup.HorizontalOffset = charRect.X + 5;
            SuggestionPopup.VerticalOffset = charRect.Bottom + 5;
        }

        private string GetDetailedErrorMessage(Exception ex)
        {
            var message = $"Exception Type: {ex.GetType().Name}\nMessage: {ex.Message}\n";

            if (!string.IsNullOrEmpty(ex.StackTrace))
                message += $"Stack Trace:\n{ex.StackTrace}\n";

            if (ex.InnerException != null)
            {
                message += "\nInner Exception:\n" + GetDetailedErrorMessage(ex.InnerException);
            }

            return message;
        }



        private void UpdateLineNumbers()
        {
            if (LineNumbersTextBlock == null || CodeTextBox == null) return;
            int lineCount = CodeTextBox.LineCount > 0 ? CodeTextBox.LineCount : 1;
            LineNumbersTextBlock.Text = string.Join("\n", Enumerable.Range(1, lineCount));
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void RunPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (EditorModeComboBox.SelectedIndex == 0)
                _xamlCode = CodeTextBox.Text;
            else
                _csCode = CodeTextBox.Text;

            if (string.IsNullOrWhiteSpace(_xamlCode) || string.IsNullOrWhiteSpace(_csCode))
            {
                DialogService.ShowMessage("Both XAML and C# code must be provided before running the plugin.",
                                "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Close existing plugin window if open to avoid memory leak
                if (_currentPluginWindow != null)
                {
                    _currentPluginWindow.Close();
                    _currentPluginWindow = null;
                }

                var loader = new DynamicPluginLoaderFromStrings();
                Window pluginWindow = loader.LoadPluginWindowFromStrings(_xamlCode, _csCode);

                _currentPluginWindow = pluginWindow;
                pluginWindow.Closed += (s, args) => _currentPluginWindow = null;  // Clear reference when closed

                pluginWindow.Show();
            }
            catch (Exception ex)
            {
                string detailedError = GetDetailedErrorMessage(ex);
                DialogService.ShowMessage($"Failed to run plugin due to an error:\n\n{detailedError}\n\n" +
                                "Please check your XAML syntax and C# code for errors.",
                                "Plugin Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            EditorModeComboBox.SelectedIndex = 0;
            _xamlCode = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""Test Plugin"" Height=""200"" Width=""300"">
  <Grid>
    <Button Name=""MyButton"" Content=""Click me!"" Width=""100"" Height=""30""/>
  </Grid>
</Window>";

            _csCode = @"using System.Windows;
using System.Windows.Controls;

public class TestPluginLogic
{
    public TestPluginLogic(Window window)
    {
        var button = (Button)((Grid)window.Content).FindName(""MyButton"");
        if (button != null)
        {
            button.Click += (s, e) => MessageBox.Show(""Button clicked from plugin!"");
        }
    }
}";

            CodeLabel.Text = "Plugin XAML Code:";
            CodeTextBox.Text = _xamlCode;
            UpdateLineNumbers();
            SuggestionPopup.IsOpen = false;
        }



        private void SavePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (EditorModeComboBox.SelectedIndex == 0)
                _xamlCode = CodeTextBox.Text;
            else
                _csCode = CodeTextBox.Text;

            try
            {
                string createdPluginDir = Path.Combine(Paths.Plugins, "CreatedPlugin");

                if (!Directory.Exists(createdPluginDir))
                    Directory.CreateDirectory(createdPluginDir);

                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string xamlFilePath = Path.Combine(createdPluginDir, $"Plugin_{timeStamp}.xaml");
                string csFilePath = Path.Combine(createdPluginDir, $"Plugin_{timeStamp}.cs");

                File.WriteAllText(xamlFilePath, _xamlCode);
                File.WriteAllText(csFilePath, _csCode);

                // Cleanup old plugin files, keep only last 8 versions
                var xamlFiles = Directory.GetFiles(createdPluginDir, "Plugin_*.xaml").OrderByDescending(f => f).ToList();
                var csFiles = Directory.GetFiles(createdPluginDir, "Plugin_*.cs").OrderByDescending(f => f).ToList();

                foreach (var oldFile in xamlFiles.Skip(8))
                    File.Delete(oldFile);

                foreach (var oldFile in csFiles.Skip(8))
                    File.Delete(oldFile);

                DialogService.ShowMessage("Plugin saved successfully.", "Save Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                DialogService.ShowMessage("Permission denied. Please ensure you have write access to the plugin directory.",
                                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                DialogService.ShowMessage($"File IO error while saving plugin:\n{ioEx.Message}\n" +
                                "Check if the files are locked or in use by another process.",
                                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string detailedError = GetDetailedErrorMessage(ex);
                DialogService.ShowMessage($"Unexpected error while saving plugin:\n\n{detailedError}",
                                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportPlugin_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XAML files (*.xaml)|*.xaml|C# files (*.cs)|*.cs|All files (*.*)|*.*",
                Multiselect = true,
                Title = "Import Plugin Files"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    try
                    {
                        string content = File.ReadAllText(filename);
                        if (filename.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                        {
                            _xamlCode = content;
                            if (EditorModeComboBox.SelectedIndex == 0)
                                CodeTextBox.Text = _xamlCode;
                        }
                        else if (filename.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            _csCode = content;
                            if (EditorModeComboBox.SelectedIndex == 1)
                                CodeTextBox.Text = _csCode;
                        }
                        else
                        {
                            DialogService.ShowMessage($"File '{filename}' is not a recognized plugin file type (.xaml or .cs).",
                                            "Import Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        DialogService.ShowMessage($"File not found: '{filename}'. It may have been moved or deleted.",
                                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        DialogService.ShowMessage($"Access denied when reading file '{filename}'. Please check permissions.",
                                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        string detailedError = GetDetailedErrorMessage(ex);
                        DialogService.ShowMessage($"Failed to import file '{filename}':\n\n{detailedError}",
                                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void ExportPlugin_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "XAML file (*.xaml)|*.xaml|C# file (*.cs)|*.cs",
                Title = "Export Plugin Code",
                FileName = EditorModeComboBox.SelectedIndex == 0 ? "Plugin.xaml" : "Plugin.cs"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string contentToSave = EditorModeComboBox.SelectedIndex == 0 ? _xamlCode : _csCode;
                    File.WriteAllText(saveFileDialog.FileName, contentToSave);
                    DialogService.ShowMessage($"File saved successfully:\n{saveFileDialog.FileName}", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DialogService.ShowMessage($"Failed to export file:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            this.Close();
        }
    }
}
