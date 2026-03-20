using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Namespaces;
using KPatcher.Core.Patcher;
using KPatcher.Core.Reader;
using KPatcher.Core.Uninstall;
using KPatcher.UI.Resources;
using KPatcher.UI.Update;
using KPatcher.UI.Views.Dialogs;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace KPatcher.UI.ViewModels
{

    public class MainWindowViewModel : ViewModelBase
    {
        private readonly PatchLogger _logger = new PatchLogger();
        private readonly RobustLogger _robustLogger;
        private readonly StringBuilder _logTextBuilder = new StringBuilder();

        /// <summary>
        /// Observable list of log entries for colored display in the View.
        /// </summary>
        public ObservableCollection<FormattedLogEntry> LogEntries { get; } = new ObservableCollection<FormattedLogEntry>();

        /// <summary>
        /// Gets all log entries for formatting. Used by View to format log display.
        /// </summary>
        public IReadOnlyList<FormattedLogEntry> GetLogEntries() => LogEntries;

        private string _logText = string.Empty;
        private bool _isTaskRunning;
        private int _progressValue;
        private int _progressMaximum = 100;
        [CanBeNull]
        private string _selectedNamespace;
        [CanBeNull]
        private string _selectedGamePath;
        private string _modPath = string.Empty;
        private Avalonia.Media.IBrush _logTextColor = Avalonia.Media.Brushes.White;
        private bool _isRtfContent = false;
        private string _rtfContent = string.Empty;
        private bool _isModSelectionVisible = true;
        private bool _isShowingConfigurationSummary;
        private string _configurationSummaryText = string.Empty;
        // Cached info.rtf content when user toggles to configuration summary (restore when toggling back; no disk read).
        private string _cachedInfoRtfContent = string.Empty;
        private bool _cachedInfoIsRtf = true;

        /// <summary>
        /// Whether the mod selection UI (combobox, Browse button, ? button) is visible.
        /// Only set to false at startup when the app was run from (or next to) a tslpatchdata folder
        /// with namespaces.ini/changes.ini/changes.yaml (conditionally-loaded UI). When true, the
        /// game installation combobox appears below; when false, the game combobox is in its place.
        /// </summary>
        public bool IsModSelectionVisible
        {
            get => _isModSelectionVisible;
            set => SetProperty(ref _isModSelectionVisible, value);
        }

        /// <summary>
        /// Whether the current version is an alpha/pre-release version.
        /// Used to conditionally show alpha warnings and disclaimers.
        /// </summary>
        public bool IsAlphaVersion => Core.IsAlphaVersionOrLowerThanV1_0_0(Core.VersionLabel);

        /// <summary>
        /// Window title: "KOTORPatcher". Includes alpha disclaimer only if version is alpha.
        /// </summary>
        public string WindowTitle
        {
            get
            {
                string title = UIResources.MainWindowTitle;
                if (IsAlphaVersion)
                {
                    title += UIResources.WindowTitleAlphaSuffix;
                }
                return title;
            }
        }

        /// <summary>Start patching button label (TSLPatcher .rsrc 0x004a2ba8).</summary>
        public string StartPatchingButtonText => TSLPatcherMessages.StartPatchingButton;

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        /// <summary>
        /// Color for the log text - changes based on log type for consistency with Python
        /// </summary>
        public Avalonia.Media.IBrush LogTextColor
        {
            get => _logTextColor;
            set => SetProperty(ref _logTextColor, value);
        }

        /// <summary>
        /// Whether we're displaying RTF content (true) or plain text logs (false)
        /// </summary>
        public bool IsRtfContent
        {
            get => _isRtfContent;
            set
            {
                if (SetProperty(ref _isRtfContent, value))
                {
                    OnPropertyChanged(nameof(IsNotRtfContent));
                    Console.WriteLine($"[RTF] IsRtfContent set to: {value}");
                }
            }
        }

        /// <summary>
        /// Inverse of IsRtfContent for XAML binding
        /// </summary>
        public bool IsNotRtfContent => !_isRtfContent;

        /// <summary>
        /// True when the main area shows the configuration summary (dry-run report); false when showing info.rtf/content.
        /// </summary>
        public bool IsShowingConfigurationSummary
        {
            get => _isShowingConfigurationSummary;
            private set
            {
                if (SetProperty(ref _isShowingConfigurationSummary, value))
                {
                    OnPropertyChanged(nameof(DisplayedPlainText));
                }
            }
        }

        /// <summary>
        /// Plain text shown in the main area: configuration summary when toggled, otherwise the log.
        /// </summary>
        public string DisplayedPlainText => IsShowingConfigurationSummary ? _configurationSummaryText : LogText;

        /// <summary>
        /// RTF content to display in RichTextBox
        /// </summary>
        public string RtfContent
        {
            get => _rtfContent;
            set
            {
                if (SetProperty(ref _rtfContent, value))
                {
                    Console.WriteLine($"[RTF] RtfContent set, length: {value?.Length ?? 0}");
                }
            }
        }

        public bool IsTaskRunning
        {
            get => _isTaskRunning;
            set
            {
                if (SetProperty(ref _isTaskRunning, value))
                {
                    OnIsTaskRunningChanged(value);
                }
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            set => SetProperty(ref _progressMaximum, value);
        }

        [CanBeNull]
        public string SelectedNamespace
        {
            get => _selectedNamespace;
            set
            {
                if (SetProperty(ref _selectedNamespace, value))
                {
                    OnSelectedNamespaceChanged(value);
                    OnPropertyChanged(nameof(CanShowConfigurationSummary));
                }
            }
        }

        [CanBeNull]
        public string SelectedGamePath
        {
            get => _selectedGamePath;
            set
            {
                if (SetProperty(ref _selectedGamePath, value))
                {
                    OnSelectedGamePathChanged(value);
                }
            }
        }

        public string ModPath
        {
            get => _modPath;
            set
            {
                if (SetProperty(ref _modPath, value))
                {
                    // Update RobustLogger to write to installlog.txt when ModPath is set
                    if (!string.IsNullOrEmpty(value))
                    {
                        string logFilePath = Core.GetLogFilePath(value);
                        _robustLogger.SetLogFilePath(logFilePath);
                    }
                }
            }
        }

        private List<PatcherNamespace> _loadedNamespaces = new List<PatcherNamespace>();
        // Can be null if no cancellation token source created
        private CancellationTokenSource _cancellationTokenSource;
        // Can be null if no config reader loaded
        private ConfigReader _currentConfigReader;
        private LogLevel _logLevel = LogLevel.Warnings;
        private readonly bool _oneShot = false;

        public ObservableCollection<string> Namespaces { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> GamePaths { get; } = new ObservableCollection<string>();

        public bool CanInstall => !IsTaskRunning &&
                                  !string.IsNullOrEmpty(SelectedNamespace) &&
                                  !string.IsNullOrEmpty(SelectedGamePath);

        /// <summary>True when configuration summary can be shown (mod and config loaded).</summary>
        public bool CanShowConfigurationSummary => !string.IsNullOrEmpty(SelectedNamespace) &&
                                                    _currentConfigReader?.Config != null;

        public ICommand BrowseModCommand { get; }
        public ICommand BrowseGamePathCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ValidateIniCommand { get; }
        public ICommand UninstallModCommand { get; }
        public ICommand FixPermissionsCommand { get; }
        public ICommand FixCaseSensitivityCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand ShowNamespaceInfoCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public ICommand ToggleConfigurationSummaryCommand { get; }

        public MainWindowViewModel()
        {
            // Initialize RobustLogger for KPatcher errors/exceptions/warnings/info
            // Will set log file path when mod is loaded
            _robustLogger = new RobustLogger();

            // Initialize commands
            BrowseModCommand = new AsyncRelayCommand(BrowseMod);
            BrowseGamePathCommand = new AsyncRelayCommand(BrowseGamePath);
            InstallCommand = new AsyncRelayCommand(Install);
            ExitCommand = new RelayCommand(Exit);
            ValidateIniCommand = new AsyncRelayCommand(ValidateIni);
            UninstallModCommand = new AsyncRelayCommand(UninstallMod);
            FixPermissionsCommand = new AsyncRelayCommand(FixPermissions);
            FixCaseSensitivityCommand = new AsyncRelayCommand(FixCaseSensitivity);
            OpenUrlCommand = new RelayCommand<string>(OpenUrl);
            CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdates);
            ShowNamespaceInfoCommand = new AsyncRelayCommand(ShowNamespaceInfo);
            ShowHelpCommand = new RelayCommand(ShowHelp);
            SetLanguageCommand = new RelayCommand<string>(SetLanguage);
            ToggleConfigurationSummaryCommand = new AsyncRelayCommand(ToggleConfigurationSummary);

            // Subscribe to logger events
            _logger.VerboseLogged += OnLogEntry;
            _logger.NoteLogged += OnLogEntry;
            _logger.WarningLogged += OnLogEntry;
            _logger.ErrorLogged += OnLogEntry;

            // Initialize with welcome message
            AddLogEntry(UIResources.WelcomeToKPatcher, LogType.Note);
            AddLogEntry(UIResources.SelectModAndKotorToBegin, LogType.Note);

            // Try to detect KOTOR installations on UI thread so second combobox binding sees the update
            Avalonia.Threading.Dispatcher.UIThread.Post(DetectGamePaths, Avalonia.Threading.DispatcherPriority.Loaded);

            // Auto-open mod from tslpatchdata (if present), or reload mod after language change
            _ = InitializeModAsync();
        }

        private async Task InitializeModAsync()
        {
            await TryAutoOpenMod();
            if (!string.IsNullOrEmpty(Core.LastLoadedModPathForLanguageChange))
            {
                string path = Core.LastLoadedModPathForLanguageChange;
                Core.LastLoadedModPathForLanguageChange = null;
                await LoadModFromPath(path);
            }
        }

        private void OnLogEntry([CanBeNull] object sender, PatchLog log)
        {
            WriteLogEntry(log);
        }

        private void WriteLogEntry(PatchLog log)
        {
            // Write ALL logs to file regardless of log level
            // log_file.write(f"{log.formatted_message}\n") happens BEFORE filtering
            try
            {
                string logFilePath = Core.GetLogFilePath(ModPath);
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    string directory = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.AppendAllText(logFilePath, log.FormattedMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                _robustLogger.Error($"Failed to write log file: {ex.Message}");
                Debug.WriteLine($"Failed to write log file: {ex.Message}");
            }

            // Filter by log level for UI display only
            LogType minLevel = GetLogTypeForLevel(_logLevel);
            if ((int)log.LogType < (int)minLevel)
            {
                return;
            }

            // Add to UI with formatting
            AddLogEntry(log.FormattedMessage, log.LogType);
        }

        private LogType GetLogTypeForLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Errors:
                    return LogType.Warning;
                case LogLevel.General:
                    return LogType.Warning;
                case LogLevel.Full:
                    return LogType.Verbose;
                case LogLevel.Warnings:
                    return LogType.Note;
                case LogLevel.Nothing:
                    return LogType.Warning;
                default:
                    return LogType.Warning;
            }
        }

        /// <summary>
        /// Adds a log entry with formatting. Matches Python's write_log behavior.
        /// The View's FormatLogText() method uses the FormattedLogEntry list to render
        /// colored, styled text matching Python's tkinter tag_configure implementation.
        /// </summary>
        public void AddLogEntry(string message, LogType logType = LogType.Note)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Store formatted log entry for styled rendering in View
                var entry = new FormattedLogEntry(message, logType);
                LogEntries.Add(entry);

                // Keep plain text for scroll-to-end and any consumers
                _logTextBuilder.AppendLine(message);
                LogText = _logTextBuilder.ToString();
            });
        }

        /// <summary>
        /// Helper method to parse log type from message prefix like [ERROR], [WARNING], etc.
        /// </summary>
        private LogType ParseLogTypeFromMessage(string message)
        {
            if (message.StartsWith("[ERROR]", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("[CRITICAL]", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Error;
            }
            if (message.StartsWith("[WARNING]", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Warning;
            }
            if (message.StartsWith("[DEBUG]", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("[VERBOSE]", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Verbose;
            }
            return LogType.Note; // Default to INFO/NOTE
        }

        private void LogExceptionToDebugConsole(Exception ex, string context = "")
        {
            string contextPrefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
            string message = $"{contextPrefix}EXCEPTION: {ex.GetType().Name}: {ex.Message}";

            // Write using RobustLogger (which writes to installlog.txt)
            _robustLogger.Exception(message, ex);

            Debug.WriteLine(message);
            Debug.WriteLine($"{contextPrefix}STACK TRACE:");
            Debug.WriteLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"{contextPrefix}INNER EXCEPTION: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                Debug.WriteLine($"{contextPrefix}INNER STACK TRACE:");
                Debug.WriteLine(ex.InnerException.StackTrace);
            }

            // Also write to console for good measure
            Console.Error.WriteLine(message);
            Console.Error.WriteLine($"{contextPrefix}STACK TRACE:");
            Console.Error.WriteLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"{contextPrefix}INNER EXCEPTION: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                Console.Error.WriteLine($"{contextPrefix}INNER STACK TRACE:");
                Console.Error.WriteLine(ex.InnerException.StackTrace);
            }
        }

        private async Task BrowseMod()
        {
            // Can be null if window not available
            Window window = GetMainWindow();
            if (window is null)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = UIResources.SelectModDirectory,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string path = folders[0].Path.LocalPath;
                await LoadModFromPath(path);
            }
        }

        private async Task BrowseGamePath()
        {
            Window window = GetMainWindow();
            if (window is null)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = UIResources.SelectKotorDirectory,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string path = folders[0].Path.LocalPath;
                if (!GamePaths.Contains(path))
                {
                    GamePaths.Add(path);
                }
                SelectedGamePath = path;
            }
        }

        /// <summary>
        /// Toggles the main area between configuration summary (dry-run report) and cached info.rtf/content.
        /// Caches info content when first switching to summary so we don't reload from disk when switching back.
        /// </summary>
        private async Task ToggleConfigurationSummary()
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (IsShowingConfigurationSummary)
                {
                    // Switch back to info: restore cached content (no disk read)
                    IsShowingConfigurationSummary = false;
                    RtfContent = _cachedInfoRtfContent ?? string.Empty;
                    IsRtfContent = _cachedInfoIsRtf;
                    OnPropertyChanged(nameof(DisplayedPlainText));
                    return;
                }

                // Switch to configuration summary: cache current info, then show summary
                PatcherNamespace selectedNs = _loadedNamespaces?.FirstOrDefault(ns => ns.Name == SelectedNamespace);
                if (selectedNs is null || _currentConfigReader?.Config == null)
                {
                    return;
                }

                _cachedInfoRtfContent = RtfContent ?? string.Empty;
                _cachedInfoIsRtf = IsRtfContent;

                string changesFileName = selectedNs.ChangesFilePath();
                string infoFileName = selectedNs.RtfFilePath();
                _configurationSummaryText = Core.BuildConfigurationSummary(changesFileName, infoFileName, _currentConfigReader.Config);
                IsRtfContent = false;
                IsShowingConfigurationSummary = true;
                OnPropertyChanged(nameof(DisplayedPlainText));
            });
        }

        private async Task Install()
        {
            if (!PreInstallValidate())
            {
                return;
            }

            IsTaskRunning = true;
            ProgressValue = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            ClearLogText();

            try
            {
                // Can be null if namespace not found
                PatcherNamespace selectedNs = _loadedNamespaces.FirstOrDefault(ns => ns.Name == SelectedNamespace);
                if (selectedNs is null)
                {
                    throw new InvalidOperationException(UIResources.SelectedNamespaceNotFound);
                }

                // Use Core.InstallMod which handles path resolution correctly (matches Python)
                // installer = ModInstaller(namespace_mod_path, game_path, ini_file_path, logger)
                // where namespace_mod_path = ini_file_path.parent (parent of the ini file)
                string tslPatchDataPath = Path.Combine(ModPath, "tslpatchdata");
                string iniFilePath = Path.Combine(tslPatchDataPath, selectedNs.ChangesFilePath());

                if (!File.Exists(iniFilePath))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, UIResources.ChangesIniFileNotFoundFormat, iniFilePath));
                }

                // namespace_mod_path: CaseAwarePath = ini_file_path.parent
                // The modPath for ModInstaller should be the parent of the ini file, not the mod root
                string namespaceModPath = Path.GetDirectoryName(iniFilePath) ?? tslPatchDataPath;

                var installer = new ModInstaller(namespaceModPath, SelectedGamePath, iniFilePath, _logger)
                {
                    TslPatchDataPath = tslPatchDataPath
                };

                // TSLPatcher-style confirmation before starting (always shown unless one-shot)
                if (!_oneShot)
                {
                    MsBox.Avalonia.Base.IMsBox<ButtonResult> confirmBox = MessageBoxManager.GetMessageBoxStandard(
                        UIResources.StartPatchingButtonLabel,
                        TSLPatcherMessages.StartPatchingConfirmation,
                        ButtonEnum.YesNo,
                        Icon.Question);
                    ButtonResult result = await confirmBox.ShowAsync();
                    if (result != ButtonResult.Yes)
                    {
                        IsTaskRunning = false;
                        return;
                    }
                }

                // Mod-specific confirmation message from changes.ini (if any)
                string confirmMsg = Core.GetConfirmMessage(installer);
                if (!string.IsNullOrEmpty(confirmMsg) && !_oneShot)
                {
                    MsBox.Avalonia.Base.IMsBox<ButtonResult> confirmBox = MessageBoxManager.GetMessageBoxStandard(
                        UIResources.ThisModRequiresConfirmation,
                        confirmMsg,
                        ButtonEnum.OkCancel,
                        Icon.Question);
                    ButtonResult result = await confirmBox.ShowAsync();
                    if (result != ButtonResult.Ok)
                    {
                        IsTaskRunning = false;
                        return;
                    }
                }

                AddLogEntry(UIResources.StartingInstallation, LogType.Note);

                // Calculate total patches for progress
                int totalPatches = Core.CalculateTotalPatches(installer);
                ProgressMaximum = totalPatches;

                DateTime installStartTime = DateTime.UtcNow;

                await Task.Run(() =>
                {
                    installer.Install(
                        _cancellationTokenSource.Token,
                        progress =>
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                ProgressValue = progress;
                            });
                        });
                }, _cancellationTokenSource.Token);

                TimeSpan installTime = DateTime.UtcNow - installStartTime;
                int numErrors = _logger.Errors.Count();
                int numWarnings = _logger.Warnings.Count();
                int numPatches = installer.Config().PatchCount();

                string timeStr = Core.FormatInstallTime(installTime);
                _logger.AddNote(
                    string.Format(CultureInfo.CurrentCulture, UIResources.InstallationCompleteWithErrorsAndWarningsFormat, numErrors, numWarnings, Environment.NewLine, timeStr, numPatches));

                ProgressValue = ProgressMaximum;

                // Show TSLPatcher-style completion message (four variants)
                string completionBody;
                Icon completionIcon;
                string completionTitle;
                if (numErrors > 0 && numWarnings > 0)
                {
                    completionTitle = UIResources.PatcherFinishedTitle;
                    completionBody = string.Format(CultureInfo.CurrentCulture, TSLPatcherMessages.PatcherFinishedWithErrorsAndWarnings, numErrors, numWarnings);
                    completionIcon = Icon.Error;
                }
                else if (numErrors > 0)
                {
                    completionTitle = UIResources.PatcherFinishedTitle;
                    completionBody = string.Format(CultureInfo.CurrentCulture, TSLPatcherMessages.PatcherFinishedWithErrors, numErrors);
                    completionIcon = Icon.Error;
                }
                else if (numWarnings > 0)
                {
                    completionTitle = UIResources.PatcherFinishedTitle;
                    completionBody = string.Format(CultureInfo.CurrentCulture, TSLPatcherMessages.PatcherFinishedWithWarnings, numWarnings);
                    completionIcon = Icon.Warning;
                }
                else
                {
                    completionTitle = UIResources.PatcherFinishedTitle;
                    completionBody = TSLPatcherMessages.PatcherFinished;
                    completionIcon = Icon.Success;
                }
                MsBox.Avalonia.Base.IMsBox<ButtonResult> completionBox = MessageBoxManager.GetMessageBoxStandard(
                    completionTitle,
                    completionBody,
                    ButtonEnum.Ok,
                    completionIcon);
                await completionBox.ShowAsync();
            }
            catch (OperationCanceledException ex)
            {
                LogExceptionToDebugConsole(ex, "Install (Cancelled)");
                AddLogEntry(UIResources.InstallationCancelled, LogType.Warning);
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "Install");
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.InstallationFailedFormat, ex.Message), LogType.Error);
                string installErrorMsg = string.Format(CultureInfo.CurrentCulture, UIResources.UnexpectedErrorDuringInstallationFormat, Environment.NewLine, ex.Message);
                MsBox.Avalonia.Base.IMsBox<ButtonResult> errorBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.Error,
                    installErrorMsg,
                    ButtonEnum.Ok,
                    Icon.Error);
                await errorBox.ShowAsync();
            }
            finally
            {
                IsTaskRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void ClearLogText()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _logTextBuilder.Clear();
                LogEntries.Clear();
                LogText = string.Empty;
                RtfContent = string.Empty;
                IsRtfContent = false;
            });
        }

        private void Exit()
        {
            Window window = GetMainWindow();
            window?.Close();
        }

        private void SetLanguage(string twoLetterCode)
        {
            App.RequestLanguageChange(twoLetterCode);
        }

        private async Task ValidateIni()
        {
            if (!PreInstallValidate())
            {
                return;
            }

            IsTaskRunning = true;
            ClearLogText();

            await Task.Run(() =>
            {
                try
                {
                    // Can be null if namespace not found
                    PatcherNamespace selectedNs = _loadedNamespaces.FirstOrDefault(ns => ns.Name == SelectedNamespace);
                    if (selectedNs is null)
                    {
                        throw new InvalidOperationException(UIResources.SelectedNamespaceNotFound);
                    }

                    Core.ValidateConfig(ModPath, _loadedNamespaces, SelectedNamespace, _logger);
                }
                catch (Exception ex)
                {
                    LogExceptionToDebugConsole(ex, "ValidateIni");
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.ValidationFailedFormat, ex.Message), LogType.Error);
                }
                finally
                {
                    IsTaskRunning = false;
                    _logger.AddNote(UIResources.ConfigReaderTestComplete);
                }
            });
        }

        private async Task UninstallMod()
        {
            if (!ValidateModPathAndGamePath())
            {
                return;
            }

            IsTaskRunning = true;

            try
            {
                string backupRoot = ModPath;
                while (!Directory.Exists(Path.Combine(backupRoot, "tslpatchdata")) && !string.IsNullOrEmpty(Path.GetDirectoryName(backupRoot)))
                {
                    backupRoot = Path.GetDirectoryName(backupRoot) ?? backupRoot;
                }

                string backupsLocation = Path.Combine(backupRoot, "backup");

                if (!Directory.Exists(backupsLocation))
                {
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.NoBackupDirectoryFoundFormat, backupsLocation), LogType.Error);
                    return;
                }

                var uninstaller = new ModUninstaller(new CaseAwarePath(backupsLocation), new CaseAwarePath(SelectedGamePath), _logger);

                await Task.Run(() =>
                {
                    uninstaller.UninstallSelectedMod(
                        showErrorDialog: (title, msg) =>
                        {
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(title, msg, ButtonEnum.Ok, Icon.Error);
                                await box.ShowAsync();
                            }).Wait();
                        },
                        showYesNoDialog: (title, msg) =>
                        {
                            bool result = false;
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(title, msg, ButtonEnum.YesNo, Icon.Question);
                                ButtonResult res = await box.ShowAsync();
                                result = res == ButtonResult.Yes;
                            }).Wait();
                            return result;
                        },
                        showYesNoCancelDialog: (title, msg) =>
                        {
                            bool? result = null;
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(title, msg, ButtonEnum.YesNoCancel, Icon.Question);
                                ButtonResult res = await box.ShowAsync();
                                if (res == ButtonResult.Yes)
                                {
                                    result = true;
                                }
                                else if (res == ButtonResult.No)
                                {
                                    result = false;
                                }
                                else
                                {
                                    result = null;
                                }
                            }).Wait();
                            return result;
                        },
                        ui: Core.CreateModUninstallerUiStrings()
                    );
                });

                AddLogEntry(UIResources.UninstallProcessFinished);
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "UninstallMod");
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.UninstallFailedFormat, ex.Message), LogType.Error);
            }
            finally
            {
                IsTaskRunning = false;
            }
        }

        private async Task FixPermissions()
        {
            Window window = GetMainWindow();
            if (window is null)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = UIResources.SelectDirectoryFixPermissions,
                AllowMultiple = false
            });

            if (folders.Count == 0)
            {
                return;
            }

            string directory = folders[0].Path.LocalPath;

            MsBox.Avalonia.Base.IMsBox<ButtonResult> confirmBox = MessageBoxManager.GetMessageBoxStandard(
                UIResources.WarningTitle,
                UIResources.WarningNotAToy,
                ButtonEnum.YesNo,
                Icon.Warning);
            ButtonResult result = await confirmBox.ShowAsync();
            if (result != ButtonResult.Yes)
            {
                return;
            }

            IsTaskRunning = true;
            ClearLogText();
            AddLogEntry(UIResources.PleaseWaitMayTakeAWhile);

            await Task.Run(() =>
            {
                try
                {
                    SystemHelpers.FixPermissions(directory, msg => AddLogEntry(msg));

                    int numFiles = 0;
                    int numFolders = 0;
                    if (Directory.Exists(directory))
                    {
                        numFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
                        numFolders = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories).Length;
                    }

                    string extraMsg = string.Format(CultureInfo.CurrentCulture, UIResources.FilesAndFoldersProcessedFormat, numFiles, numFolders);
                    AddLogEntry(extraMsg);

                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        MsBox.Avalonia.Base.IMsBox<ButtonResult> successBox = MessageBoxManager.GetMessageBoxStandard(
                            UIResources.SuccessfullyAcquiredPermission,
                            string.Format(CultureInfo.CurrentCulture, UIResources.OperationSuccessfulFormat, extraMsg),
                            ButtonEnum.Ok,
                            Icon.Success);
                        await successBox.ShowAsync();
                    });
                }
                catch (Exception ex)
                {
                    LogExceptionToDebugConsole(ex, "FixPermissions");
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.FailedToFixPermissionsFormat, ex.Message), LogType.Error);
                    string permErrorMsg = string.Format(CultureInfo.CurrentCulture, UIResources.PermissionsDeniedCheckLogsFormat, Environment.NewLine, ex.Message);
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        MsBox.Avalonia.Base.IMsBox<ButtonResult> errorBox = MessageBoxManager.GetMessageBoxStandard(
                            UIResources.CouldNotAcquirePermission,
                            permErrorMsg,
                            ButtonEnum.Ok,
                            Icon.Error);
                        await errorBox.ShowAsync();
                    });
                }
                finally
                {
                    IsTaskRunning = false;
                    _logger.AddNote(UIResources.PermissionsFixerTaskCompleted);
                }
            });
        }

        private async Task FixCaseSensitivity()
        {
            Window window = GetMainWindow();
            if (window is null)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = UIResources.SelectDirectoryFixCaseSensitivity,
                AllowMultiple = false
            });

            if (folders.Count == 0)
            {
                return;
            }

            string directory = folders[0].Path.LocalPath;

            IsTaskRunning = true;
            ClearLogText();
            AddLogEntry(UIResources.PleaseWaitMayTakeAWhile);

            await Task.Run(() =>
            {
                try
                {
                    bool madeChange = false;
                    SystemHelpers.FixCaseSensitivityRecursive(directory, msg =>
                    {
                        AddLogEntry(msg);
                        madeChange = true;
                    });

                    if (!madeChange)
                    {
                        AddLogEntry(UIResources.NothingToChangeCase);
                    }
                    AddLogEntry(UIResources.IOSCaseRenameTaskCompleted);
                }
                catch (Exception ex)
                {
                    LogExceptionToDebugConsole(ex, "FixCaseSensitivity");
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.FailedToFixCaseSensitivityFormat, ex.Message), LogType.Error);
                }
                finally
                {
                    IsTaskRunning = false;
                }
            });
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "OpenUrl");
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.FailedToOpenUrlFormat, ex.Message), LogType.Error);
            }
        }

        private async Task CheckUpdates()
        {
            try
            {
                bool useBetaChannel = false;
                Dictionary<string, object> updateInfo = await Config.GetRemoteKPatcherUpdateInfoAsync(useBetaChannel);
                if (updateInfo.Count == 0)
                {
                    await ShowErrorAsync(UIResources.UpdateError, UIResources.UnableToFetchUpdateInfo);
                    return;
                }

                RemoteUpdateInfo remoteInfo = RemoteUpdateInfoParser.FromDictionary(updateInfo);
                string latestVersion = remoteInfo.GetChannelVersion(useBetaChannel);

                if (Config.RemoteVersionNewer(Config.CurrentVersion, latestVersion))
                {
                    string notes = remoteInfo.GetChannelNotes(useBetaChannel);
                    string message = string.Format(CultureInfo.CurrentCulture, UIResources.UpdateAvailableMessageFormat, latestVersion, Environment.NewLine, notes);
                    string choice = await ShowChoiceDialogAsync(UIResources.UpdateAvailable, message, UIResources.UpdateButton, UIResources.ManualDownloadButton);

                    if (choice == UIResources.UpdateButton)
                    {
                        await RunAutoUpdate(remoteInfo, useBetaChannel);
                    }
                    else if (choice == UIResources.ManualDownloadButton)
                    {
                        OpenUrl(remoteInfo.GetDownloadPage(useBetaChannel));
                    }
                }
                else
                {
                    string message = string.Format(CultureInfo.CurrentCulture, UIResources.NoUpdatesFoundMessageFormat, Core.VersionLabel);
                    string choice = await ShowChoiceDialogAsync(UIResources.NoUpdatesFound, message, UIResources.ReinstallButton);
                    if (choice == UIResources.ReinstallButton)
                    {
                        await RunAutoUpdate(remoteInfo, useBetaChannel);
                    }
                }
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "CheckUpdates");
                await ShowErrorAsync(UIResources.UpdateError, ex.Message);
            }
        }

        private async Task ShowNamespaceInfo()
        {
            if (string.IsNullOrEmpty(SelectedNamespace))
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.NoNamespaceSelected,
                    UIResources.PleaseSelectNamespaceFirst,
                    ButtonEnum.Ok,
                    Icon.Info);
                await infoBox.ShowAsync();
                return;
            }

            string description = Core.GetNamespaceDescription(_loadedNamespaces, SelectedNamespace);
            MsBox.Avalonia.Base.IMsBox<ButtonResult> descBox = MessageBoxManager.GetMessageBoxStandard(
                SelectedNamespace,
                string.IsNullOrEmpty(description) ? UIResources.NoDescriptionAvailable : description,
                ButtonEnum.Ok,
                Icon.Info);
            await descBox.ShowAsync();
        }

        private void ShowHelp()
        {
            Window owner = GetMainWindow();
            var helpWindow = new HelpWindow();
            if (owner != null)
            {
                helpWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
                helpWindow.Show(owner);
            }
            else
            {
                helpWindow.Show();
            }
        }

        private async Task<string> ShowChoiceDialogAsync(string title, string message, params string[] options)
        {
            Window window = GetMainWindow();
            if (window is null)
            {
                return null;
            }

            var dialog = new ChoiceDialog(title, message, options);
            return await dialog.ShowDialog<string>(window);
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                title,
                message,
                ButtonEnum.Ok,
                Icon.Error);
            await box.ShowAsync();
        }

        private async Task RunAutoUpdate(RemoteUpdateInfo info, bool useBetaChannel)
        {
            Window window = GetMainWindow();
            if (window is null)
            {
                AddLogEntry(UIResources.UnableToLocateWindowForUpdater, LogType.Error);
                return;
            }

            var updater = new AutoUpdater();
            await updater.RunAsync();
        }

        /// <summary>
        /// Loads a mod from a directory. Accepts either the mod root (folder containing tslpatchdata)
        /// or the tslpatchdata folder itself; Core.LoadMod normalizes to the same internal mod path.
        /// On success, hides the mod selector so the configuration summary UI is shown (TSLPatcher parity).
        /// </summary>
        private async Task LoadModFromPath(string path)
        {
            try
            {
                Core.ModInfo modInfo = Core.LoadMod(path);
                ModPath = modInfo.ModPath;
                _loadedNamespaces = modInfo.Namespaces;
                _currentConfigReader = modInfo.ConfigReader;

                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.LoadedNamespacesCountFormat, _loadedNamespaces.Count));

                // Update UI on main thread (mod selector visibility is only set on startup by TryAutoOpenMod)
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OnPropertyChanged(nameof(CanShowConfigurationSummary));
                    Namespaces.Clear();
                    foreach (PatcherNamespace ns in _loadedNamespaces)
                    {
                        string displayName = !string.IsNullOrWhiteSpace(ns.Name) ? ns.Name : ns.ChangesFilePath();
                        Namespaces.Add(displayName);
                    }

                    if (Namespaces.Count > 0)
                    {
                        SelectedNamespace = Namespaces[0];
                        OnNamespaceSelected();
                    }
                });
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "LoadModFromPath");
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.FailedToLoadModFormat, ex.Message), LogType.Error);
                MsBox.Avalonia.Base.IMsBox<ButtonResult> errorBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.Error,
                    string.Format(CultureInfo.CurrentCulture, UIResources.CouldNotFindModAtFolderFormat, Environment.NewLine, ex.Message),
                    ButtonEnum.Ok,
                    Icon.Error);
                await errorBox.ShowAsync();
            }
        }

        private void OnNamespaceSelected()
        {
            if (string.IsNullOrEmpty(SelectedNamespace) || string.IsNullOrEmpty(ModPath))
            {
                return;
            }

            try
            {
                Core.NamespaceInfo namespaceInfo = Core.LoadNamespaceConfig(ModPath, _loadedNamespaces, SelectedNamespace, _currentConfigReader);
                _logLevel = namespaceInfo.LogLevel;

                // Update game paths based on namespace (from FindKotorPathsFromDefault in Core)
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (namespaceInfo.GamePaths.Count > 0)
                    {
                        GamePaths.Clear();
                        foreach (string gamePath in namespaceInfo.GamePaths)
                        {
                            if (!GamePaths.Contains(gamePath))
                            {
                                GamePaths.Add(gamePath);
                            }
                        }
                        if (GamePaths.Count > 0 && string.IsNullOrEmpty(SelectedGamePath))
                        {
                            SelectedGamePath = GamePaths[0];
                        }
                    }
                    else
                    {
                        // No game number in mod config or no paths for that game: keep combobox populated from detected paths
                        List<string> detected = Core.GetDetectedKotorPaths();
                        if (detected.Count > 0)
                        {
                            GamePaths.Clear();
                            foreach (string path in detected)
                            {
                                GamePaths.Add(path);
                            }
                            if (string.IsNullOrEmpty(SelectedGamePath))
                            {
                                SelectedGamePath = GamePaths[0];
                            }
                        }
                    }
                });

                // Load and display info.rtf/rte content
                if (!string.IsNullOrEmpty(namespaceInfo.InfoContent))
                {
                    Console.WriteLine($"[RTF] OnNamespaceSelected: InfoContent length={namespaceInfo.InfoContent.Length}, IsRtf={namespaceInfo.IsRtf}");
                    ClearLogText();

                    if (namespaceInfo.IsRtf)
                    {
                        Console.WriteLine("[RTF] Setting IsRtfContent=true and RtfContent on UI thread");
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            IsRtfContent = true;
                            RtfContent = namespaceInfo.InfoContent;
                            Console.WriteLine($"[RTF] Properties set: IsRtfContent={IsRtfContent}, RtfContent length={RtfContent?.Length ?? 0}");
                        }, Avalonia.Threading.DispatcherPriority.Normal);
                    }
                    else
                    {
                        // Plain text fallback (e.g. non-RTF file named info.rtf)
                        Console.WriteLine("[RTF] Detected plain text content");
                        IsRtfContent = false;
                        AddLogEntry(namespaceInfo.InfoContent);
                    }
                }
                else
                {
                    // TSLPatcher parity: show exact error when info.rtf is missing (.rsrc 0x004a2a62)
                    Console.WriteLine("[RTF] InfoContent is empty (info.rtf/rte missing)");
                    IsRtfContent = false;
                    ClearLogText();
                    AddLogEntry(TSLPatcherMessages.UnableToLoadInstructionsTslpatchdata, LogType.Error);
                }
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "OnNamespaceSelected");
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.FailedToLoadNamespaceConfigFormat, ex.Message), LogType.Error);
            }
        }

        private void OnSelectedNamespaceChanged([CanBeNull] string value)
        {
            OnPropertyChanged(nameof(CanInstall));
            if (IsShowingConfigurationSummary)
            {
                IsShowingConfigurationSummary = false;
            }
            if (!string.IsNullOrEmpty(value))
            {
                OnNamespaceSelected();
            }
        }

        private void DetectGamePaths()
        {
            // Same as HoloPatcher: registry + default paths (K1 + TSL), flattened
            List<string> detected = Core.GetDetectedKotorPaths();
            GamePaths.Clear();
            foreach (string path in detected)
            {
                GamePaths.Add(path);
            }

            if (GamePaths.Count > 0)
            {
                SelectedGamePath = GamePaths[0];
                AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.DetectedKotorInstallationsFormat, GamePaths.Count));
            }
        }

        [CanBeNull]
        private static Window GetMainWindow()
        {
            return App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }

        private void OnIsTaskRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanInstall));
        }


        private void OnSelectedGamePathChanged([CanBeNull] string value)
        {
            OnPropertyChanged(nameof(CanInstall));
        }

        private bool PreInstallValidate()
        {
            if (IsTaskRunning)
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.TaskAlreadyRunning,
                    UIResources.WaitForPreviousTask,
                    ButtonEnum.Ok,
                    Icon.Info);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await infoBox.ShowAsync());
                return false;
            }

            if (string.IsNullOrEmpty(ModPath) || !Directory.Exists(ModPath))
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.NoModChosen,
                    UIResources.SelectModDirectoryFirst,
                    ButtonEnum.Ok,
                    Icon.Info);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await infoBox.ShowAsync());
                return false;
            }

            if (string.IsNullOrEmpty(SelectedGamePath))
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.NoGameFolder,
                    TSLPatcherMessages.NoValidGameFolderSelected,
                    ButtonEnum.Ok,
                    Icon.Info);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await infoBox.ShowAsync());
                return false;
            }

            var gamePath = new CaseAwarePath(SelectedGamePath);
            if (!gamePath.IsDirectory())
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.NoGameFolder,
                    TSLPatcherMessages.NoValidGameFolderSelected,
                    ButtonEnum.Ok,
                    Icon.Info);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await infoBox.ShowAsync());
                return false;
            }

            string dialogTlkPath = Path.Combine(SelectedGamePath, "dialog.tlk");
            if (!File.Exists(dialogTlkPath))
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> infoBox = MessageBoxManager.GetMessageBoxStandard(
                    UIResources.InvalidGameFolder,
                    TSLPatcherMessages.InvalidGameFolderDialogTlkNotFound,
                    ButtonEnum.Ok,
                    Icon.Info);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await infoBox.ShowAsync());
                return false;
            }

            return true;
        }

        private bool ValidateGamePathAndNamespace()
        {
            return PreInstallValidate() && !string.IsNullOrEmpty(SelectedNamespace);
        }

        private bool ValidateModPathAndGamePath()
        {
            if (string.IsNullOrEmpty(ModPath) || string.IsNullOrEmpty(SelectedGamePath))
            {
                AddLogEntry(UIResources.PleaseSelectModAndGameDirectory, LogType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to auto-open a mod when the app starts inside or next to a tslpatchdata folder.
        /// Checks: (1) exe directory contains tslpatchdata with config, (2) exe directory is itself
        /// tslpatchdata (has namespaces.ini/changes.ini/changes.yaml), (3) same for current working directory.
        /// Uses the same summary UI (mod selector hidden) as when the user browses to a mod folder.
        /// </summary>
        private async Task TryAutoOpenMod()
        {
            try
            {
                string exeDirectory = string.IsNullOrEmpty(AppContext.BaseDirectory)
                    ? Directory.GetCurrentDirectory()
                    : Path.GetFullPath(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string cwd = Directory.GetCurrentDirectory();

                // Try exe directory first (mod root, or exe running inside tslpatchdata)
                if (Core.IsValidModPath(exeDirectory))
                {
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.AutoOpeningModFromFormat, exeDirectory));
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { IsModSelectionVisible = false; });
                    await LoadModFromPath(exeDirectory);
                    return;
                }

                // Try current working directory if different (e.g. user cd'd into mod folder and ran app)
                if (!string.Equals(exeDirectory, cwd, StringComparison.OrdinalIgnoreCase) && Core.IsValidModPath(cwd))
                {
                    AddLogEntry(string.Format(CultureInfo.CurrentCulture, UIResources.AutoOpeningModFromFormat, cwd));
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { IsModSelectionVisible = false; });
                    await LoadModFromPath(cwd);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogExceptionToDebugConsole(ex, "TryAutoOpenMod");
                Debug.WriteLine($"Failed to auto-open mod: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Represents a formatted log entry with its log type for color/styling.
    /// Uses idiomatic log-level colors: error=red, warning=dark golden yellow, verbose=light blue, note=default.
    /// </summary>
    public class FormattedLogEntry
    {
        public string Message { get; }
        public LogType LogType { get; }
        public string TagName { get; }
        /// <summary>Brush for this log level (idiomatic colors).</summary>
        public IBrush EntryBrush { get; }

        public FormattedLogEntry(string message, LogType logType)
        {
            Message = message;
            LogType = logType;

            switch (logType)
            {
                case LogType.Note:
                    TagName = "INFO";
                    EntryBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#000000")); // default/info black
                    break;
                case LogType.Verbose:
                    TagName = "DEBUG";
                    EntryBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#87CEEB")); // light sky blue
                    break;
                case LogType.Warning:
                    TagName = "WARNING";
                    EntryBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#B8860B")); // dark goldenrod
                    break;
                case LogType.Error:
                    TagName = "ERROR";
                    EntryBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#DC143C")); // crimson red
                    break;
                default:
                    TagName = "INFO";
                    EntryBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#000000"));
                    break;
            }
        }
    }
}
