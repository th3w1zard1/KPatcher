using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KEditChanges;
using KPatcher.Core.Config;

namespace KEditChanges.UI
{
    public partial class MainWindow : Window
    {
        private ChangesIniDocument _document;
        private readonly ChangesIniService _service = new ChangesIniService();
        private List<ChangesIniSectionNode> _sectionNodes = new List<ChangesIniSectionNode>();

        public MainWindow()
        {
            InitializeComponent();
            OpenIniButton.Click += OnOpenIniClick;
            SaveIniButton.Click += OnSaveIniClick;
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
                IniPathText.Text = path;
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
                _service.Save(_document, path, includeHeader: true);
                IniPathText.Text = path;
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

        private void ShowSection(ChangesIniSectionKind kind)
        {
            SectionTitleText.Text = kind.ToString();
            bool isSettings = kind == ChangesIniSectionKind.Settings;
            SettingsPanel.IsVisible = isSettings;
            SectionPlaceholderText.IsVisible = !isSettings;
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
    }
}
