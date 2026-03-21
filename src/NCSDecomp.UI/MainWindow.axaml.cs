// Copyright 2021-2025 NCSDecomp / KPatcher

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.UI
{
    public partial class MainWindow : Window
    {
        private const int TabHighlighted = 0;
        private const int TabAst = 3;

        private NcsDecompSettings _settings;
        private string _ncsPath;
        /// <summary>Last NCS bytes used for a decompile attempt (parse tree is built lazily from this).</summary>
        private byte[] _bytesForAst;
        private int _decompileGeneration;
        private int _astTreeBuiltForGeneration = -1;

        public MainWindow()
        {
            InitializeComponent();
            _settings = NcsDecompSettings.Load(NcsDecompSettings.GetDefaultAppBaseDirectory(), true);
            GameCombo.SelectedIndex = FileDecompilerOptions.IsK2Selected ? 1 : 0;

            OpenNcsButton.Click += OnOpenNcsClick;
            DecompileButton.Click += OnDecompileClick;
            SaveNssButton.Click += OnSaveNssClick;
            RoundTripButton.Click += OnRoundTripClick;
            GameCombo.SelectionChanged += OnGameChanged;
        }

        private void OnMainTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabs == null || NssOutputPlain == null)
                return;
            // When switching to Highlighted, refresh from plain text so edits apply.
            if (MainTabs.SelectedIndex == TabHighlighted)
                RefreshHighlightedView();
            else if (MainTabs.SelectedIndex == TabAst)
                TryPopulateAstTree();
        }

        private void RefreshHighlightedView()
        {
            if (NssHighlighted == null || NssOutputPlain == null)
                return;

            string text = NssOutputPlain.Text ?? string.Empty;
            NssHighlighted.Inlines.Clear();
            foreach (NwScriptHighlightedSegment seg in NwScriptSyntaxHighlighter.Segment(text))
            {
                var run = new Run(seg.Text);
                IBrush brush = BrushForHighlight(seg.Kind);
                if (brush != null)
                    run.Foreground = brush;
                NssHighlighted.Inlines.Add(run);
            }
        }

        private void RefreshBytecodeView(string tokenStream)
        {
            if (NssBytecodeView == null)
                return;

            tokenStream = tokenStream ?? string.Empty;
            NssBytecodeView.Inlines.Clear();
            foreach (NcsBytecodeHighlightedSegment seg in NcsBytecodeSyntaxHighlighter.Segment(tokenStream))
            {
                var run = new Run(seg.Text);
                IBrush brush = BrushForBytecode(seg.Kind);
                if (brush != null)
                    run.Foreground = brush;
                if (seg.Kind == NcsBytecodeHighlightKind.Instruction
                    || seg.Kind == NcsBytecodeHighlightKind.Function
                    || seg.Kind == NcsBytecodeHighlightKind.TypeIndicator)
                    run.FontWeight = FontWeight.Bold;
                NssBytecodeView.Inlines.Add(run);
            }
        }

        private void ClearBytecodeView()
        {
            if (NssBytecodeView == null)
                return;
            NssBytecodeView.Inlines.Clear();
        }

        private static TreeViewItem ToAstTreeItem(AstOutlineNode node)
        {
            if (node == null)
                return new TreeViewItem { Header = "(null)" };

            var item = new TreeViewItem { Header = node.Label };
            foreach (AstOutlineNode child in node.Children)
                item.Items.Add(ToAstTreeItem(child));
            return item;
        }

        private void TryPopulateAstTree()
        {
            if (AstTree == null)
                return;

            if (_bytesForAst == null || _bytesForAst.Length == 0)
            {
                AstTree.Items.Clear();
                AstTree.Items.Add(new TreeViewItem { Header = "Decompile an .ncs file to load the parse tree." });
                return;
            }

            if (_astTreeBuiltForGeneration == _decompileGeneration)
                return;

            AstTree.Items.Clear();
            try
            {
                bool k2 = GameCombo != null && GameCombo.SelectedIndex == 1;
                ActionsData actions = ActionsData.LoadForGame(k2, _settings.K1NwscriptPath, _settings.K2NwscriptPath);
                Start ast = NcsParsePipeline.ParseAst(_bytesForAst, actions);
                AstOutlineNode outline = NcsAstOutline.Build(ast);
                AstTree.Items.Add(ToAstTreeItem(outline));
                _astTreeBuiltForGeneration = _decompileGeneration;
            }
            catch (Exception ex)
            {
                AstTree.Items.Add(new TreeViewItem { Header = "AST error: " + ex.Message });
            }
        }

        private static IBrush BrushForBytecode(NcsBytecodeHighlightKind kind)
        {
            switch (kind)
            {
                case NcsBytecodeHighlightKind.Instruction:
                    return new SolidColorBrush(Color.FromRgb(0, 0, 255));
                case NcsBytecodeHighlightKind.Address:
                    return new SolidColorBrush(Color.FromRgb(128, 128, 128));
                case NcsBytecodeHighlightKind.HexValue:
                    return new SolidColorBrush(Color.FromRgb(0, 128, 0));
                case NcsBytecodeHighlightKind.Function:
                    return new SolidColorBrush(Color.FromRgb(128, 0, 128));
                case NcsBytecodeHighlightKind.TypeIndicator:
                    return new SolidColorBrush(Color.FromRgb(255, 140, 0));
                default:
                    return null;
            }
        }

        private static IBrush BrushForHighlight(NwScriptHighlightKind kind)
        {
            switch (kind)
            {
                case NwScriptHighlightKind.Keyword:
                    return new SolidColorBrush(Color.FromRgb(0, 0, 255));
                case NwScriptHighlightKind.Type:
                    return new SolidColorBrush(Color.FromRgb(128, 0, 128));
                case NwScriptHighlightKind.String:
                    return new SolidColorBrush(Color.FromRgb(0, 128, 0));
                case NwScriptHighlightKind.Comment:
                    return new SolidColorBrush(Color.FromRgb(128, 128, 128));
                case NwScriptHighlightKind.Number:
                    return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                case NwScriptHighlightKind.Function:
                    return new SolidColorBrush(Color.FromRgb(0, 128, 128));
                default:
                    return null;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnGameChanged(object sender, SelectionChangedEventArgs e)
        {
            FileDecompilerOptions.IsK2Selected = GameCombo.SelectedIndex == 1;
            _settings.CaptureFromRuntime();
        }

        private async void OnOpenNcsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                IStorageFolder start = null;
                if (!string.IsNullOrEmpty(_settings.OpenDirectory) && Directory.Exists(_settings.OpenDirectory))
                {
                    start = await StorageProvider.TryGetFolderFromPathAsync(_settings.OpenDirectory);
                }

                IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open NCS",
                    AllowMultiple = false,
                    SuggestedStartLocation = start,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("NCS bytecode") { Patterns = new[] { "*.ncs" } },
                        FilePickerFileTypes.All
                    }
                });

                if (files == null || files.Count == 0)
                    return;
                string local = files[0].TryGetLocalPath();
                if (string.IsNullOrEmpty(local))
                {
                    StatusText.Text = "Could not resolve local path for selected file.";
                    return;
                }

                _ncsPath = local;
                NcsPathText.Text = local;
                ClearBytecodeView();
                _bytesForAst = null;
                _astTreeBuiltForGeneration = -1;
                if (AstTree != null)
                    AstTree.Items.Clear();
                StatusText.Text = "Loaded: " + Path.GetFileName(local);
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
            }
        }

        private void OnDecompileClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_ncsPath) || !File.Exists(_ncsPath))
            {
                StatusText.Text = "Open a valid .ncs file first.";
                return;
            }

            byte[] bytes = null;
            try
            {
                FileDecompilerOptions.IsK2Selected = GameCombo.SelectedIndex == 1;
                _settings.CaptureFromRuntime();
                bool k2 = FileDecompilerOptions.IsK2Selected;
                ActionsData actions = ActionsData.LoadForGame(k2, _settings.K1NwscriptPath, _settings.K2NwscriptPath);
                var decompiler = new FileDecompiler(actions);
                bytes = File.ReadAllBytes(_ncsPath);
                _bytesForAst = bytes;
                _decompileGeneration++;
                _astTreeBuiltForGeneration = -1;
                if (MainTabs != null && MainTabs.SelectedIndex == TabAst)
                    TryPopulateAstTree();

                string tokenStream = NcsParsePipeline.DecodeToTokenStream(bytes, actions);
                RefreshBytecodeView(tokenStream);

                try
                {
                    string nss = decompiler.DecompileToNss(bytes);
                    NssOutputPlain.Text = nss;
                    RefreshHighlightedView();
                    StatusText.Text = "Decompiled " + bytes.Length + " bytes → " + nss.Length + " characters.";
                }
                catch (Exception exNss)
                {
                    StatusText.Text = "Decoder OK; NSS error: " + exNss.Message;
                    NssOutputPlain.Text = string.Empty;
                    RefreshHighlightedView();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
                NssOutputPlain.Text = string.Empty;
                RefreshHighlightedView();
                ClearBytecodeView();
                if (bytes == null)
                {
                    _bytesForAst = null;
                    _astTreeBuiltForGeneration = -1;
                }
            }
        }

        private void OnRoundTripClick(object sender, RoutedEventArgs e)
        {
            if (_bytesForAst == null || _bytesForAst.Length == 0)
            {
                StatusText.Text = "Decompile an .ncs first (round-trip needs original bytecode).";
                return;
            }

            string nss = NssOutputPlain.Text ?? string.Empty;
            if (nss.Trim().Length == 0)
            {
                StatusText.Text = "No NSS in Plain text — decompile or paste a script first.";
                return;
            }

            try
            {
                _settings.CaptureFromRuntime();
                bool k2 = GameCombo.SelectedIndex == 1;
                ManagedRoundTripCompareResult r = RoundTripUtil.CompareManagedRecompileToOriginalDecoderText(
                    _bytesForAst,
                    nss,
                    k2,
                    _settings.K1NwscriptPath,
                    _settings.K2NwscriptPath);

                string msg = r.Summary;
                if (msg.Length > 600)
                    msg = msg.Substring(0, 597) + "…";
                StatusText.Text = (r.DecoderOutputsMatch ? "OK: " : r.CompileSucceeded ? "Mismatch: " : "") + msg;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Round-trip check failed: " + ex.Message;
            }
        }

        private async void OnSaveNssClick(object sender, RoutedEventArgs e)
        {
            string text = NssOutputPlain.Text ?? string.Empty;
            if (text.Length == 0)
            {
                StatusText.Text = "Nothing to save.";
                return;
            }

            try
            {
                IStorageFolder start = null;
                if (!string.IsNullOrEmpty(_settings.OutputDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(_settings.OutputDirectory);
                        start = await StorageProvider.TryGetFolderFromPathAsync(_settings.OutputDirectory);
                    }
                    catch
                    {
                        start = null;
                    }
                }

                string ext = string.IsNullOrWhiteSpace(_settings.FileExtension) ? ".nss" : _settings.FileExtension;
                if (!ext.StartsWith(".", StringComparison.Ordinal))
                    ext = "." + ext;

                string suggestName = "out" + ext;
                if (!string.IsNullOrEmpty(_ncsPath))
                {
                    suggestName = Path.GetFileNameWithoutExtension(_ncsPath) + ext;
                    if (!string.IsNullOrEmpty(_settings.FilenamePrefix))
                        suggestName = _settings.FilenamePrefix + suggestName;
                    if (!string.IsNullOrEmpty(_settings.FilenameSuffix))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(suggestName);
                        suggestName = baseName + _settings.FilenameSuffix + ext;
                    }
                }

                IStorageFile file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save NSS",
                    SuggestedStartLocation = start,
                    SuggestedFileName = suggestName,
                    DefaultExtension = ext.TrimStart('.'),
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("NSS script") { Patterns = new[] { "*.nss" } }
                    }
                });

                if (file == null)
                    return;

                string path = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    StatusText.Text = "Could not resolve save path.";
                    return;
                }

                Encoding enc;
                try
                {
                    enc = string.IsNullOrWhiteSpace(_settings.EncodingName)
                        ? new UTF8Encoding(false)
                        : Encoding.GetEncoding(_settings.EncodingName);
                }
                catch
                {
                    enc = new UTF8Encoding(false);
                }

                File.WriteAllText(path, text, enc);
                StatusText.Text = "Saved: " + path;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Save failed: " + ex.Message;
            }
        }
    }
}
