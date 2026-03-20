using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvRichTextBox;
using KPatcher.Core.Common;
using KPatcher.UI.Resources;
using KPatcher.UI.Rte;
using KPatcher.UI.ViewModels;
using MsBox.Avalonia;

namespace KPatcher.UI.Views
{

    public partial class MainWindow : Window
    {
        private readonly ScrollViewer _logScrollViewer;
        private RichTextBox _rtfRichTextBox;
        private RichTextBox _logRichTextBox;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);

            // Get reference to log ScrollViewer and RichTextBoxes
            _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            _rtfRichTextBox = this.FindControl<RichTextBox>("RtfRichTextBox");
            _logRichTextBox = this.FindControl<RichTextBox>("LogRichTextBox");

            // Subscribe to data context changes to set up auto-scroll and log formatting
            DataContextChanged += OnDataContextChanged;

            // Set up window centering on startup (matches Python's set_window)
            Opened += OnWindowOpened;
        }

        private async void OnWindowOpened(object sender, EventArgs e)
        {
            // Center window on screen - matches Python's set_window behavior
            if (Screens.Primary != null)
            {
                var screen = Screens.Primary.WorkingArea;
                int x = (int)((screen.Width - Width) / 2);
                int y = (int)((screen.Height - Height) / 2);
                Position = new Avalonia.PixelPoint(x, y);
            }

            // Show alpha/demo warning on startup
            await ShowAlphaWarning();
        }

        private async System.Threading.Tasks.Task ShowAlphaWarning()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                // Only show warning if version is alpha
                if (!viewModel.IsAlphaVersion)
                {
                    return;
                }

                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.AlphaWarningMessageFormat, Core.VersionLabel);
                var messageBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.AlphaWarningTitle,
                    message,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Warning);
                await messageBox.ShowAsync();
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            Console.WriteLine("[RTF] OnDataContextChanged called");
            if (DataContext is MainWindowViewModel viewModel)
            {
                Console.WriteLine("[RTF] Subscribing to PropertyChanged events");
                // Subscribe to property changes to auto-scroll log and load RTF
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // Subscribe to log entries so colored log RichTextBox is updated when entries are added
                viewModel.LogEntries.CollectionChanged += LogEntries_CollectionChanged;

                // Also check if RTF content is already set
                if (viewModel.IsRtfContent && !string.IsNullOrEmpty(viewModel.RtfContent))
                {
                    Console.WriteLine("[RTF] RTF content already set, loading immediately");
                    Dispatcher.UIThread.Post(() =>
                    {
                        LoadRtfContent();
                    }, DispatcherPriority.Normal);
                }
                else if (!viewModel.IsRtfContent)
                {
                    Dispatcher.UIThread.Post(() => RefreshLogContent(), DispatcherPriority.Background);
                }
            }
        }

        private void LogEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => RefreshLogContent(), DispatcherPriority.Background);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine($"[RTF] PropertyChanged: {e.PropertyName}");
            if (e.PropertyName == nameof(MainWindowViewModel.LogText))
            {
                // Scroll log to end so latest entry is visible
                Dispatcher.UIThread.Post(() =>
                {
                    _logScrollViewer?.ScrollToEnd();
                }, DispatcherPriority.Background);
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.RtfContent))
            {
                Console.WriteLine("[RTF] RtfContent property changed, loading RTF");
                // Load RTF content into RichTextBox when it changes - use Normal priority to ensure it happens
                Dispatcher.UIThread.Post(() =>
                {
                    LoadRtfContent();
                }, DispatcherPriority.Normal);
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.IsRtfContent))
            {
                Console.WriteLine($"[RTF] IsRtfContent changed to: {((MainWindowViewModel)sender).IsRtfContent}");
                // If RTF content is enabled and we have content, load it
                if (DataContext is MainWindowViewModel vm && vm.IsRtfContent && !string.IsNullOrEmpty(vm.RtfContent))
                {
                    Console.WriteLine("[RTF] IsRtfContent=true and RtfContent exists, loading RTF");
                    Dispatcher.UIThread.Post(() =>
                    {
                        LoadRtfContent();
                    }, DispatcherPriority.Normal);
                }
                else if (DataContext is MainWindowViewModel vm2 && !vm2.IsRtfContent)
                {
                    Dispatcher.UIThread.Post(() => RefreshLogContent(), DispatcherPriority.Background);
                }
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.DisplayedPlainText) ||
                     e.PropertyName == nameof(MainWindowViewModel.IsShowingConfigurationSummary))
            {
                Dispatcher.UIThread.Post(() => RefreshLogContent(), DispatcherPriority.Background);
            }
        }

        private static string BrushToHex(IBrush brush)
        {
            if (brush is SolidColorBrush scb)
            {
                return scb.Color.ToString();
            }
            return "#000000";
        }

        private void RefreshLogContent()
        {
            if (_logRichTextBox == null)
            {
                _logRichTextBox = this.FindControl<RichTextBox>("LogRichTextBox");
            }
            if (_logRichTextBox == null)
            {
                return;
            }
            var vm = DataContext as MainWindowViewModel;
            if (vm == null)
            {
                return;
            }

            RteDocument doc;
            if (vm.IsShowingConfigurationSummary)
            {
                string text = vm.DisplayedPlainText ?? string.Empty;
                var ranges = new List<(int Start, int End, string ForegroundColor)>();
                if (text.Length > 0)
                {
                    ranges.Add((0, text.Length, "#000000"));
                }
                doc = RteDocumentConverter.FromColoredLines(text, ranges);
            }
            else
            {
                System.Collections.ObjectModel.ObservableCollection<FormattedLogEntry> entries = vm.LogEntries;
                string content = string.Join("\n", entries.Select(e => e.Message));
                int offset = 0;
                var ranges = new List<(int Start, int End, string ForegroundColor)>();
                foreach (FormattedLogEntry entry in entries)
                {
                    int start = offset;
                    int end = offset + entry.Message.Length;
                    ranges.Add((start, end, BrushToHex(entry.EntryBrush)));
                    offset = end + 1;
                }
                doc = RteDocumentConverter.FromColoredLines(content, ranges);
            }

            try
            {
                RteDocumentConverter.ApplyToRichTextBox(_logRichTextBox, doc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log] RefreshLogContent ApplyToRichTextBox failed: {ex.Message}");
            }
        }

        private void LoadRtfContent()
        {
            Console.WriteLine("[RTF] LoadRtfContent() called");

            // Try to get RichTextBox if not already found
            if (_rtfRichTextBox is null)
            {
                Console.WriteLine("[RTF] RichTextBox not cached, searching for control...");
                _rtfRichTextBox = this.FindControl<RichTextBox>("RtfRichTextBox");
                if (_rtfRichTextBox is null)
                {
                    Console.WriteLine("[RTF] ERROR: RtfRichTextBox control not found by name 'RtfRichTextBox'!");
                    Console.WriteLine("[RTF] This might mean the XAML control name doesn't match or the control isn't loaded yet.");
                    return;
                }
                else
                {
                    Console.WriteLine("[RTF] Found RichTextBox by name");
                }
            }

            if (_rtfRichTextBox is null)
            {
                Console.WriteLine("[RTF] ERROR: RtfRichTextBox is still null!");
                return;
            }

            Console.WriteLine($"[RTF] RichTextBox found: IsVisible={_rtfRichTextBox.IsVisible}, IsEnabled={_rtfRichTextBox.IsEnabled}");
            Console.WriteLine($"[RTF] RichTextBox type: {_rtfRichTextBox.GetType().FullName}");
            Console.WriteLine($"[RTF] RichTextBox IsLoaded: {_rtfRichTextBox.IsLoaded}");

            if (!(DataContext is MainWindowViewModel viewModel))
            {
                Console.WriteLine("[RTF] ERROR: DataContext is not MainWindowViewModel!");
                return;
            }

            // Ensure RichTextBox is visible before loading
            if (!_rtfRichTextBox.IsVisible)
            {
                Console.WriteLine("[RTF] WARNING: RichTextBox is not visible, making it visible...");
                _rtfRichTextBox.IsVisible = true;
            }

            // Wait for control to be fully loaded and initialized
            // The RichTextBox needs to be fully loaded before we can access FlowDocument
            if (!_rtfRichTextBox.IsLoaded)
            {
                Console.WriteLine("[RTF] WARNING: RichTextBox is not loaded yet, subscribing to Loaded event...");
                // Subscribe to Loaded event and wait for it
                EventHandler<Avalonia.Interactivity.RoutedEventArgs> loadedHandler = null;
                loadedHandler = (s, e) =>
                {
                    _rtfRichTextBox.Loaded -= loadedHandler;
                    Console.WriteLine("[RTF] RichTextBox Loaded event fired, waiting for initialization...");
                    // Wait a bit more to ensure FlowDocument is fully initialized
                    // Post to dispatcher with a delay priority to ensure all initialization is complete
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Post again with lower priority to ensure initialization completes
                        Dispatcher.UIThread.Post(() =>
                        {
                            LoadRtfContent();
                        }, DispatcherPriority.Background);
                    }, DispatcherPriority.Loaded);
                };
                _rtfRichTextBox.Loaded += loadedHandler;
                return;
            }

            // Additional check: ensure FlowDocument is initialized
            try
            {
                var flowDocProperty = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                if (flowDocProperty != null)
                {
                    object flowDoc = flowDocProperty.GetValue(_rtfRichTextBox);
                    if (flowDoc is null)
                    {
                        Console.WriteLine("[RTF] WARNING: FlowDoc is null, waiting for initialization...");
                        Dispatcher.UIThread.Post(() => LoadRtfContent(), DispatcherPriority.Loaded);
                        return;
                    }
                    Console.WriteLine("[RTF] FlowDoc is initialized");
                }
            }
            catch (Exception initEx)
            {
                Console.WriteLine($"[RTF] Warning: Error checking FlowDoc initialization: {initEx.Message}");
                // Continue anyway
            }

            string rtfContent = viewModel.RtfContent;
            if (string.IsNullOrEmpty(rtfContent))
            {
                Console.WriteLine("[RTF] WARNING: RtfContent is empty");
                return;
            }

            Console.WriteLine($"[RTF] Loading RTF content, length: {rtfContent.Length}");
            Console.WriteLine($"[RTF] RTF preview (first 200 chars): {rtfContent.Substring(0, Math.Min(200, rtfContent.Length))}");

            // Try to preload RtfDomParserAv assembly to avoid FileNotFoundException
            // This is a dependency of Simplecto.Avalonia.RichTextBox that might not be automatically loaded
            try
            {
                Console.WriteLine("[RTF] Attempting to load RtfDomParserAv assembly...");

                // First, try to load it by type name (if already referenced)
                var rtfDomParserType = Type.GetType("RtfDomParserAv.RtfDomParser, RtfDomParserAv");
                if (rtfDomParserType is null)
                {
                    // Try loading from the executing directory
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string assemblyPath = Path.Combine(baseDir, "RtfDomParserAv.dll");
                    if (File.Exists(assemblyPath))
                    {
                        Console.WriteLine($"[RTF] Found RtfDomParserAv.dll at {assemblyPath}, loading...");
                        Assembly.LoadFrom(assemblyPath);
                        Console.WriteLine("[RTF] Successfully loaded RtfDomParserAv.dll from base directory");
                    }
                    else
                    {
                        // Try to find it in the NuGet packages cache
                        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string nugetPackages = Path.Combine(userProfile, ".nuget", "packages");

                        // Search for RtfDomParserAv in NuGet cache
                        if (Directory.Exists(nugetPackages))
                        {
                            string[] rtfDomDirs = Directory.GetDirectories(nugetPackages, "*RtfDomParserAv*", SearchOption.TopDirectoryOnly);
                            foreach (string dir in rtfDomDirs)
                            {
                                string[] libDirs = Directory.GetDirectories(dir, "lib", SearchOption.AllDirectories);
                                foreach (string libDir in libDirs)
                                {
                                    string dllPath = Path.Combine(libDir, "RtfDomParserAv.dll");
                                    if (File.Exists(dllPath))
                                    {
                                        Console.WriteLine($"[RTF] Found RtfDomParserAv.dll in NuGet cache at {dllPath}, loading...");
                                        Assembly.LoadFrom(dllPath);
                                        // Copy to output directory for future use
                                        try
                                        {
                                            File.Copy(dllPath, assemblyPath, true);
                                            Console.WriteLine($"[RTF] Copied RtfDomParserAv.dll to output directory");
                                        }
                                        catch (Exception copyEx)
                                        {
                                            Console.WriteLine($"[RTF] Warning: Could not copy DLL: {copyEx.Message}");
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[RTF] RtfDomParserAv assembly already loaded");
                }
            }
            catch (Exception preloadEx)
            {
                Console.WriteLine($"[RTF] Warning: Could not preload RtfDomParserAv: {preloadEx.GetType().Name}: {preloadEx.Message}");
                // Continue anyway - LoadRtf might still work
            }

            try
            {
                // Method 1: Try loading RTF from a MemoryStream (might avoid RtfDomParserAv dependency issue)
                Console.WriteLine("[RTF] Attempting Method 1: Load RTF from MemoryStream");
                try
                {
                    using (var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtfContent)))
                    {
                        // Try to find a LoadRtf method that accepts Stream
                        MethodInfo loadRtfStreamMethod = _rtfRichTextBox.GetType().GetMethod("LoadRtf", new[] { typeof(Stream) });
                        if (loadRtfStreamMethod != null)
                        {
                            Console.WriteLine("[RTF] Found LoadRtf(Stream) method, attempting to use it");
                            loadRtfStreamMethod.Invoke(_rtfRichTextBox, new object[] { memoryStream });
                            Console.WriteLine("[RTF] RTF loaded successfully using LoadRtf(Stream)");
                            return;
                        }

                        // Try alternative: Load from TextReader
                        var loadRtfTextReaderMethod = _rtfRichTextBox.GetType().GetMethod("LoadRtf", new[] { typeof(TextReader) });
                        if (loadRtfTextReaderMethod != null)
                        {
                            Console.WriteLine("[RTF] Found LoadRtf(TextReader) method, attempting to use it");
                            memoryStream.Position = 0; // Reset stream position
                            using (var textReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8, true, 1024, true))
                            {
                                loadRtfTextReaderMethod.Invoke(_rtfRichTextBox, new object[] { textReader });
                                Console.WriteLine("[RTF] RTF loaded successfully using LoadRtf(TextReader)");
                                return;
                            }
                        }
                    }
                }
                catch (Exception streamEx)
                {
                    Console.WriteLine($"[RTF] Method 1 failed: {streamEx.GetType().Name}: {streamEx.Message}");
                    if (streamEx.InnerException != null)
                    {
                        Console.WriteLine($"[RTF] Method 1 inner exception: {streamEx.InnerException.GetType().Name}: {streamEx.InnerException.Message}");
                    }
                }

                // Method 2: Load RTF directly from string using RichTextBox.LoadRtf(string)
                // This is the preferred method - loads RTF content directly without temp file
                Console.WriteLine("[RTF] Attempting Method 2: Load RTF directly from string");
                try
                {
                    // Ensure FlowDocument is initialized first
                    var flowDocProperty = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                    if (flowDocProperty != null)
                    {
                        object flowDoc = flowDocProperty.GetValue(_rtfRichTextBox);
                        if (flowDoc is null)
                        {
                            Console.WriteLine("[RTF] WARNING: FlowDoc is null, cannot load RTF");
                            throw new InvalidOperationException("FlowDocument is not initialized");
                        }
                        Console.WriteLine("[RTF] FlowDoc is available, proceeding with LoadRtf");

                        // Ensure the document has been properly initialized by checking if Selection exists
                        var selectionProperty = flowDoc.GetType().GetProperty("Selection");
                        if (selectionProperty != null)
                        {
                            object selection = selectionProperty.GetValue(flowDoc);
                            if (selection is null)
                            {
                                Console.WriteLine("[RTF] WARNING: Selection is null, waiting a bit longer...");
                                // Wait a bit more for initialization
                                Dispatcher.UIThread.Post(() => LoadRtfContent(), DispatcherPriority.Background);
                                return;
                            }
                        }
                    }

                    // Use LoadRtf(string) which takes RTF content directly
                    // The LoadRtf method will call InitializeDocument() which may throw if Selection is not initialized
                    // We need to ensure the FlowDocument has a valid Selection object before loading
                    var flowDocProperty2 = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                    if (flowDocProperty2 != null)
                    {
                        object flowDoc = flowDocProperty2.GetValue(_rtfRichTextBox);
                        if (flowDoc != null)
                        {
                            // Ensure Selection is initialized - check if it exists and is not null
                            var selectionProperty = flowDoc.GetType().GetProperty("Selection");
                            if (selectionProperty != null)
                            {
                                object selection = selectionProperty.GetValue(flowDoc);
                                if (selection is null)
                                {
                                    Console.WriteLine("[RTF] WARNING: Selection is null, attempting to create new document first...");
                                    // Try to call NewDocument() to initialize the document properly
                                    var newDocMethod = flowDoc.GetType().GetMethod("NewDocument");
                                    if (newDocMethod != null)
                                    {
                                        newDocMethod.Invoke(flowDoc, null);
                                        Console.WriteLine("[RTF] NewDocument() called to initialize document");
                                    }
                                }
                            }
                        }
                    }

                    // Set minimal padding on FlowDocument for better fit in narrow window
                    PropertyInfo flowDocPropertyForPadding = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                    if (flowDocPropertyForPadding != null)
                    {
                        object flowDoc = flowDocPropertyForPadding.GetValue(_rtfRichTextBox);
                        if (flowDoc != null)
                        {
                            // Set minimal padding (left, top, right, bottom) - default is 0 but RTF might set it
                            var pagePaddingProperty = flowDoc.GetType().GetProperty("PagePadding");
                            if (pagePaddingProperty != null)
                            {
                                // Use minimal padding: 2 pixels on all sides for better fit
                                var minimalPadding = new Avalonia.Thickness(2);
                                pagePaddingProperty.SetValue(flowDoc, minimalPadding);
                                Console.WriteLine("[RTF] Set minimal PagePadding for better fit");
                            }
                        }
                    }

                    // Now try loading RTF - this should work if Selection is initialized
                    _rtfRichTextBox.LoadRtf(rtfContent);
                    Console.WriteLine("[RTF] RTF loaded successfully using LoadRtf(string)");

                    // After loading, ensure padding is still minimal (RTF might override it)
                    if (flowDocPropertyForPadding != null)
                    {
                        object flowDoc = flowDocPropertyForPadding.GetValue(_rtfRichTextBox);
                        if (flowDoc != null)
                        {
                            var pagePaddingProperty = flowDoc.GetType().GetProperty("PagePadding");
                            if (pagePaddingProperty != null)
                            {
                                var minimalPadding = new Avalonia.Thickness(2);
                                pagePaddingProperty.SetValue(flowDoc, minimalPadding);
                            }
                        }
                    }
                    return;
                }
                catch (NullReferenceException nullEx)
                {
                    Console.WriteLine($"[RTF] Method 2 failed with NullReferenceException: {nullEx.Message}");
                    Console.WriteLine($"[RTF] Stack trace: {nullEx.StackTrace}");
                    // This is likely an initialization issue - don't retry, fall back to stripped text
                    throw;
                }
                catch (Exception method2Ex)
                {
                    Console.WriteLine($"[RTF] Method 2 failed: {method2Ex.GetType().Name}: {method2Ex.Message}");
                    if (method2Ex.InnerException != null)
                    {
                        Console.WriteLine($"[RTF] Method 2 inner exception: {method2Ex.InnerException.GetType().Name}: {method2Ex.InnerException.Message}");
                    }
                }

                // Method 3: Write RTF content to a temp file and load from file path
                // Simplecto.Avalonia.RichTextBox also has LoadRtfDoc(string fileName) method
                Console.WriteLine("[RTF] Attempting Method 3: Load RTF from temporary file");
                string tempFile = Path.Combine(Path.GetTempPath(), $"kpatcher_info_{Guid.NewGuid()}.rtf");
                Console.WriteLine($"[RTF] Writing temp file: {tempFile}");
                File.WriteAllText(tempFile, rtfContent, System.Text.Encoding.UTF8);
                Console.WriteLine($"[RTF] Temp file written, size: {new FileInfo(tempFile).Length} bytes");

                // Set minimal padding on FlowDocument before loading
                PropertyInfo flowDocPropertyForPadding3 = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                if (flowDocPropertyForPadding3 != null)
                {
                    object flowDoc = flowDocPropertyForPadding3.GetValue(_rtfRichTextBox);
                    if (flowDoc != null)
                    {
                        var pagePaddingProperty = flowDoc.GetType().GetProperty("PagePadding");
                        if (pagePaddingProperty != null)
                        {
                            var minimalPadding = new Avalonia.Thickness(2);
                            pagePaddingProperty.SetValue(flowDoc, minimalPadding);
                        }
                    }
                }

                // Use LoadRtfDoc method which takes a file path
                _rtfRichTextBox.LoadRtfDoc(tempFile);
                Console.WriteLine("[RTF] LoadRtfDoc completed successfully");

                // Ensure padding stays minimal after loading
                if (flowDocPropertyForPadding3 != null)
                {
                    object flowDoc = flowDocPropertyForPadding3.GetValue(_rtfRichTextBox);
                    if (flowDoc != null)
                    {
                        var pagePaddingProperty = flowDoc.GetType().GetProperty("PagePadding");
                        if (pagePaddingProperty != null)
                        {
                            var minimalPadding = new Avalonia.Thickness(2);
                            pagePaddingProperty.SetValue(flowDoc, minimalPadding);
                        }
                    }
                }

                // Verify content was loaded
                PropertyInfo flowDocPropertyCheck = _rtfRichTextBox.GetType().GetProperty("FlowDoc");
                if (flowDocPropertyCheck != null)
                {
                    object flowDoc = flowDocPropertyCheck.GetValue(_rtfRichTextBox);
                    Console.WriteLine($"[RTF] FlowDoc property value: {(flowDoc != null ? "not null" : "null")}");
                }

                // Clean up temp file after a delay to ensure it's been read
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(1000); // Wait 1 second
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                            Console.WriteLine($"[RTF] Temp file deleted: {tempFile}");
                        }
                    }
                    catch (Exception delEx)
                    {
                        Console.WriteLine($"[RTF] Error deleting temp file: {delEx.Message}");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                // If RTF loading fails, fall back to stripped text
                Console.WriteLine($"[RTF] ERROR: Failed to load RTF: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[RTF] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[RTF] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Console.WriteLine($"[RTF] Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                // Log the full exception details for debugging
                Console.WriteLine($"[RTF] Full exception details: {ex}");

                // Fall back to stripped text (matches Python behavior - Python strips RTF because tkinter can't render it)
                viewModel.IsRtfContent = false;
                try
                {
                    string stripped = RtfStripper.StripRtf(rtfContent);
                    Console.WriteLine($"[RTF] Falling back to stripped text, length: {stripped.Length}");
                    // Set as plain text content - not as a log entry
                    // This matches Python's set_stripped_rtf_text behavior
                    viewModel.ClearLogText();
                    viewModel.AddLogEntry(stripped, KPatcher.Core.Logger.LogType.Note);
                }
                catch (Exception stripEx)
                {
                    Console.WriteLine($"[RTF] ERROR: Failed to strip RTF: {stripEx.Message}");
                    viewModel.IsRtfContent = false;
                    viewModel.AddLogEntry(UIResources.FailedToLoadRtfContent, KPatcher.Core.Logger.LogType.Error);
                }
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Unsubscribe from events
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            base.OnClosing(e);
        }
    }
}
