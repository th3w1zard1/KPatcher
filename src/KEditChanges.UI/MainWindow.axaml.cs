using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using KEditChanges;
using KPatcher.Core.Config;

namespace KEditChanges.UI
{
    public partial class MainWindow : Window
    {
        private ChangesIniDocument _document;
        private readonly ChangesIniService _service = new ChangesIniService();
        private List<ChangesIniSectionNode> _sectionNodes = new List<ChangesIniSectionNode>();
        private FileSystemWatcher _fileWatcher;
        private string _watchedPath;
        private bool _suppressExternalReload;
        private DispatcherTimer _suppressTimer;
        private bool _bindingUi;

        public MainWindow()
        {
            InitializeComponent();
            OpenIniButton.Click += OnOpenIniClick;
            SaveIniButton.Click += OnSaveIniClick;
            Closed += OnWindowClosed;
            WireSettingsDirtyTracking();
        }

        private void WireSettingsDirtyTracking()
        {
            WindowCaptionBox.TextChanged += OnSettingsFieldChanged;
            ConfirmMessageBox.TextChanged += OnSettingsFieldChanged;
            LogLevelCombo.SelectionChanged += OnSettingsFieldChanged;
            InstallerModeCheck.IsCheckedChanged += OnSettingsFieldChanged;
            BackupFilesCheck.IsCheckedChanged += OnSettingsFieldChanged;
            PlaintextLogCheck.IsCheckedChanged += OnSettingsFieldChanged;
        }

        private void OnSettingsFieldChanged(object sender, EventArgs e)
        {
            if (_bindingUi || _document == null)
            {
                return;
            }

            _document.MarkDirty();
            UpdateDirtyIndicator();
        }

        private void UpdateDirtyIndicator()
        {
            if (_document == null)
            {
                return;
            }

            string path = !string.IsNullOrEmpty(_document.SourcePath)
                ? _document.SourcePath
                : IniPathText.Text?.TrimEnd(' ', '*') ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            IniPathText.Text = _document.IsDirty ? path + " *" : path;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            StopWatching();
            if (_suppressTimer != null)
            {
                _suppressTimer.Stop();
                _suppressTimer = null;
            }
        }

        private void OnOpenIniClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var files = topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open changes.ini",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("TSLPatcher INI") { Patterns = new[] { "*.ini" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*" } }
                }
            }).GetAwaiter().GetResult();

            if (files == null || files.Count == 0)
            {
                return;
            }

            string path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Could not resolve local path for selected file.");
                return;
            }

            try
            {
                _document = _service.Load(path);
                BindDocumentToUi();
                _document.MarkClean();
                IniPathText.Text = path;
                StartWatching(path);
                UpdateDirtyIndicator();
                SetStatus("Loaded " + path + " (" + _document.Config.PatchCount().ToString(CultureInfo.InvariantCulture) + " patches).");
            }
            catch (Exception ex)
            {
                SetStatus("Load failed: " + ex.Message);
            }
        }

        private void OnSaveIniClick(object sender, RoutedEventArgs e)
        {
            if (_document == null)
            {
                SetStatus("Nothing to save — open changes.ini first.");
                return;
            }

            ApplySettingsFromUi();

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            string suggested = string.IsNullOrEmpty(_document.SourcePath) ? "changes.ini" : _document.SourcePath;
            var file = topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save changes.ini",
                SuggestedFileName = System.IO.Path.GetFileName(suggested),
                DefaultExtension = "ini",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("TSLPatcher INI") { Patterns = new[] { "*.ini" } }
                }
            }).GetAwaiter().GetResult();

            if (file == null)
            {
                return;
            }

            string path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Could not resolve save path.");
                return;
            }

            try
            {
                SuppressExternalReloadBriefly();
                _service.Save(_document, path, includeHeader: true);
                IniPathText.Text = path;
                StartWatching(path);
                UpdateDirtyIndicator();
                SetStatus("Saved " + path);
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            SetStatus(ChangeEditReMapping.Info);
        }

        private void OnSectionTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SectionTree.SelectedItem is TreeViewItem item && item.Tag is ChangesIniSectionKind kind)
            {
                ShowSection(kind);
            }
        }

        private void BindDocumentToUi()
        {
            _bindingUi = true;
            try
            {
                PatcherConfig config = _document.Config;
                WindowCaptionBox.Text = config.WindowTitle ?? string.Empty;
                ConfirmMessageBox.Text = config.ConfirmMessage ?? string.Empty;
                LogLevelCombo.SelectedIndex = ClampLogLevelIndex((int)config.LogLevel);
                InstallerModeCheck.IsChecked = config.InstallerMode;
                BackupFilesCheck.IsChecked = config.BackupFiles;
                PlaintextLogCheck.IsChecked = config.PlaintextLog;

                _sectionNodes = ChangesIniSectionCatalog.BuildTree(_document);
                SectionTree.Items.Clear();
                foreach (ChangesIniSectionNode node in _sectionNodes)
                {
                    var treeItem = new TreeViewItem
                    {
                        Header = node.DisplayName + " (" + node.ItemCount.ToString(CultureInfo.InvariantCulture) + ")",
                        Tag = node.Kind
                    };
                    SectionTree.Items.Add(treeItem);
                }

                if (SectionTree.Items.Count > 0)
                {
                    SectionTree.SelectedItem = SectionTree.Items[0];
                }

                ShowSection(ChangesIniSectionKind.Settings);
            }
            finally
            {
                _bindingUi = false;
            }
        }

        private void ShowSection(ChangesIniSectionKind kind)
        {
            SectionTitleText.Text = kind.ToString();
            bool isSettings = kind == ChangesIniSectionKind.Settings;
            SettingsPanel.IsVisible = isSettings;
            SectionEntriesList.IsVisible = !isSettings;
            if (_document == null || isSettings)
            {
                SectionPlaceholderText.IsVisible = false;
                SectionEntriesList.Items.Clear();
                return;
            }

            List<string> entries = ChangesIniSectionFormatter.FormatEntries(kind, _document.Config);
            SectionEntriesList.Items.Clear();
            foreach (string line in entries)
            {
                SectionEntriesList.Items.Add(line);
            }

            SectionPlaceholderText.IsVisible = entries.Count == 0;
        }

        private void OnReloadIniClick(object sender, RoutedEventArgs e)
        {
            ReloadFromDisk(false);
        }

        private void OnReloadDiscardIniClick(object sender, RoutedEventArgs e)
        {
            ReloadFromDisk(true);
        }

        private void ReloadFromDisk(bool discardLocalEdits)
        {
            if (_document == null || string.IsNullOrEmpty(_watchedPath))
            {
                SetStatus("Nothing to reload — open changes.ini first.");
                return;
            }

            if (_document.IsDirty && !discardLocalEdits)
            {
                SetStatus("Reload skipped — save or use Reload (discard unsaved edits).");
                return;
            }

            try
            {
                _document = _service.Load(_watchedPath);
                BindDocumentToUi();
                _document.MarkClean();
                UpdateDirtyIndicator();
                SetStatus(discardLocalEdits
                    ? "Reloaded (discarded local edits) " + _watchedPath
                    : "Reloaded " + _watchedPath);
            }
            catch (Exception ex)
            {
                SetStatus("Reload failed: " + ex.Message);
            }
        }

        private void ApplySettingsFromUi()
        {
            if (_document == null)
            {
                return;
            }

            PatcherConfig config = _document.Config;
            config.WindowTitle = WindowCaptionBox.Text ?? string.Empty;
            config.ConfirmMessage = ConfirmMessageBox.Text ?? string.Empty;
            config.LogLevel = (LogLevel)ClampLogLevelIndex(LogLevelCombo.SelectedIndex);
            config.InstallerMode = InstallerModeCheck.IsChecked == true;
            config.BackupFiles = BackupFilesCheck.IsChecked == true;
            config.PlaintextLog = PlaintextLogCheck.IsChecked == true;
            _document.MarkDirty();
        }

        private static int ClampLogLevelIndex(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            if (index > 3)
            {
                return 3;
            }

            return index;
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message ?? string.Empty;
        }

        private void StartWatching(string path)
        {
            StopWatching();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                return;
            }

            _watchedPath = fullPath;
            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _fileWatcher.Changed += OnWatchedFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private void StopWatching()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Changed -= OnWatchedFileChanged;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }

            _watchedPath = null;
        }

        private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_suppressExternalReload)
            {
                return;
            }

            Dispatcher.UIThread.Post(TryReloadFromExternalChange);
        }

        private void TryReloadFromExternalChange()
        {
            if (_suppressExternalReload || string.IsNullOrEmpty(_watchedPath))
            {
                return;
            }

            if (_document == null)
            {
                return;
            }

            if (_document.IsDirty)
            {
                SetStatus("External change to " + _watchedPath + " (reload skipped — unsaved edits).");
                return;
            }

            try
            {
                _document = _service.Load(_watchedPath);
                BindDocumentToUi();
                _document.MarkClean();
                UpdateDirtyIndicator();
                SetStatus("Reloaded external changes from " + _watchedPath);
            }
            catch (Exception ex)
            {
                SetStatus("External reload failed: " + ex.Message);
            }
        }

        private void SuppressExternalReloadBriefly()
        {
            _suppressExternalReload = true;
            if (_suppressTimer == null)
            {
                _suppressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
                _suppressTimer.Tick += OnSuppressTimerTick;
            }

            _suppressTimer.Stop();
            _suppressTimer.Start();
        }

        private void OnSuppressTimerTick(object sender, EventArgs e)
        {
            _suppressExternalReload = false;
            if (_suppressTimer != null)
            {
                _suppressTimer.Stop();
            }
        }
    }
}
