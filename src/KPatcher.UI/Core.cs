using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.GFF;
using KPatcher.Core.Mods.NCS;
using KPatcher.Core.Mods.NSS;
using KPatcher.Core.Mods.SSF;
using KPatcher.Core.Mods.TwoDA;
using KPatcher.Core.Namespaces;
using KPatcher.Core.Patcher;
using KPatcher.Core.Reader;
using KPatcher.Core.Uninstall;
using KPatcher.UI.Resources;
namespace KPatcher.UI
{

    /// <summary>
    /// Core functionality for KPatcher.
    /// </summary>
    public static class Core
    {
        public const string VersionLabel = "v0.1.0";

        /// <summary>
        /// Returns true if the version is considered alpha/pre-release or below 1.0.0.
        /// Recognized as such when: the string contains "alpha" (case-insensitive); the numeric
        /// part parses to a version with major &lt; 1 (e.g. 0.1.0); or the string contains 'a'/'A'
        /// immediately followed by a digit (e.g. 1.0.0a1, 1.0.0-a1). A leading 'v' is ignored.
        /// </summary>
        public static bool IsAlphaVersionOrLowerThanV1_0_0(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            string normalized = version.TrimStart('v', 'V');

            if (normalized.Contains("alpha", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int end = 0;
            while (end < normalized.Length && (char.IsDigit(normalized[end]) || normalized[end] == '.'))
            {
                end++;
            }
            if (end > 0 && Version.TryParse(normalized.AsSpan(0, end), out Version v) && v.Major < 1)
            {
                return true;
            }

            for (int i = 0; i < normalized.Length - 1; i++)
            {
                if (char.ToLowerInvariant(normalized[i]) == 'a' && char.IsDigit(normalized[i + 1]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Exit codes for the application.
        /// </summary>
        public enum ExitCode
        {
            Success = 0,
            UnknownStartupError = 1,
            NumberOfArgs = 2,
            NamespacesIniNotFound = 3,
            NamespaceIndexOutOfRange = 4,
            ChangesIniNotFound = 5,
            AbortInstallUnsafe = 6,
            ExceptionDuringInstall = 7,
            InstallCompletedWithErrors = 8,
            Crash = 9,
            CloseForUpdateProcess = 10
        }

        /// <summary>
        /// Information about a loaded mod.
        /// </summary>
        public class ModInfo
        {
            public string ModPath { get; set; } = string.Empty;
            public List<PatcherNamespace> Namespaces { get; set; } = new List<PatcherNamespace>();
            public ConfigReader ConfigReader { get; set; }
        }

        /// <summary>
        /// Information about a selected namespace.
        /// </summary>
        public class NamespaceInfo
        {
            public ConfigReader ConfigReader { get; set; }
            public LogLevel LogLevel { get; set; }
            public int? GameNumber { get; set; }
            public List<string> GamePaths { get; set; } = new List<string>();
            [CanBeNull]
            public string InfoContent { get; set; }
            /// <summary>
            /// Whether InfoContent is RTF from info.rtf (always true when content is present).
            /// </summary>
            public bool IsRtf { get; set; }
        }

        /// <summary>
        /// Result of a mod installation.
        /// </summary>
        public class InstallResult
        {
            public TimeSpan InstallTime { get; set; }
            public int NumErrors { get; set; }
            public int NumWarnings { get; set; }
            public int NumPatches { get; set; }
        }

        /// <summary>
        /// Returns true if the directory is a valid mod entry point: either a mod root (contains tslpatchdata with config)
        /// or the tslpatchdata folder itself (contains namespaces.ini, changes.ini, or changes.yaml).
        /// Used for startup auto-open and for normalizing browse selection.
        /// </summary>
        public static bool IsValidModPath(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            string childTsl = Path.Combine(directoryPath, "tslpatchdata");
            if (Directory.Exists(childTsl) && HasTslPatchDataConfig(childTsl))
            {
                return true;
            }

            return HasTslPatchDataConfig(directoryPath);
        }

        private static bool HasTslPatchDataConfig(string dir)
        {
            return File.Exists(Path.Combine(dir, "namespaces.ini"))
                   || File.Exists(Path.Combine(dir, "namespaces.yaml"))
                   || File.Exists(Path.Combine(dir, "changes.ini"))
                   || File.Exists(Path.Combine(dir, "changes.yaml"));
        }

        /// <summary>
        /// Two-letter language code used when resolving localized config files (e.g. changes.de.ini).
        /// Set by RequestLanguageChange so the new window reloads the mod with the new language.
        /// </summary>
        public static string ConfigLanguageCode => LanguageSettings.GetConfigLanguageCode();

        /// <summary>
        /// When the user changes UI language, we create a new window; set this to the previous VM's ModPath
        /// so the new VM can reload the mod and pick up the language-specific config file.
        /// </summary>
        public static string LastLoadedModPathForLanguageChange { get; set; }

        /// <summary>
        /// Resolves a config file with optional language suffix: tries &lt;base&gt;.&lt;lang&gt;.ini, &lt;base&gt;.&lt;lang&gt;.yaml, then &lt;base&gt;.ini, &lt;base&gt;.yaml.
        /// Used for namespaces.ini and changes.ini/yaml (and any base name from namespaces.ini). See docs for localized config.
        /// </summary>
        /// <param name="directory">Directory (e.g. tslpatchdata path) to look in.</param>
        /// <param name="baseName">Base filename without extension (e.g. "namespaces", "changes").</param>
        /// <param name="lang">Two-letter language code (e.g. "de", "en").</param>
        /// <param name="tryYaml">If true, also try .yaml variants (namespaces and changes both use true).</param>
        /// <returns>Full path and chosen filename, or (null, null) if none exist.</returns>
        public static (string FullPath, string FileName) ResolveLocalizedConfigFile(CaseAwarePath directory, string baseName, string lang, bool tryYaml)
        {
            return LocalizedConfigResolver.Resolve(directory, baseName, lang, tryYaml);
        }

        /// <summary>
        /// Loads a mod from a directory (mod root or tslpatchdata path; both are normalized to mod root).
        /// Resolves localized config files when the UI language is non-English: e.g. namespaces.de.ini, changes.fr.yaml.
        /// </summary>
        public static ModInfo LoadMod(string directoryPath)
        {
            // tslpatchdata_path = CaseAwarePath(directory_path, "tslpatchdata")
            var tslPatchDataPath = new CaseAwarePath(directoryPath, "tslpatchdata");
            // if not tslpatchdata_path.is_dir() and tslpatchdata_path.parent.name.lower() == "tslpatchdata":
            //         tslpatchdata_path = tslpatchdata_path.parent
            if (!tslPatchDataPath.IsDirectory())
            {
                string parentPath = Path.GetDirectoryName(tslPatchDataPath.GetResolvedPath()) ?? "";
                string parentName = Path.GetFileName(parentPath) ?? "";
                if (parentName.ToLowerInvariant() == "tslpatchdata")
                {
                    tslPatchDataPath = new CaseAwarePath(parentPath);
                }
            }

            // mod_path = str(tslpatchdata_path.parent)
            string modPath = tslPatchDataPath.DirectoryName;
            string lang = ConfigLanguageCode;

            List<PatcherNamespace> namespaces;
            ConfigReader configReader = null;

            // Try localized namespaces first: namespaces.<lang>.ini, then namespaces.ini
            var (namespacesPath, namespacesFileName) = ResolveLocalizedConfigFile(tslPatchDataPath, "namespaces", lang, tryYaml: false);
            if (namespacesPath != null)
            {
                namespaces = NamespaceReader.FromFilePath(namespacesPath);
            }
            else
            {
                // Try localized changes: changes.<lang>.ini, changes.<lang>.yaml, changes.ini, changes.yaml
                var (changesPath, changesFileName) = ResolveLocalizedConfigFile(tslPatchDataPath, "changes", lang, tryYaml: true);
                if (changesPath != null)
                {
                    configReader = ConfigReader.FromFilePath(changesPath, tslPatchDataPath: tslPatchDataPath.GetResolvedPath());
                    namespaces = new List<PatcherNamespace>
                    {
                        new PatcherNamespace(changesFileName, "info.rtf")
                        {
                            Name = "Default",
                            Description = UIResources.DefaultInstallation
                        }
                    };
                }
                else
                {
                    throw new FileNotFoundException($"No namespaces.ini, namespaces.yaml, changes.ini, or changes.yaml (or localized variants) found in {tslPatchDataPath}");
                }
            }

            return new ModInfo
            {
                ModPath = modPath,
                Namespaces = namespaces,
                ConfigReader = configReader
            };
        }

        /// <summary>
        /// Loads configuration for a specific namespace.
        /// Resolves localized changes file when UI language is set: e.g. changes.de.ini for that namespace's base name.
        /// </summary>
        public static NamespaceInfo LoadNamespaceConfig(
            string modPath,
            List<PatcherNamespace> namespaces,
            string selectedNamespaceName,
            [CanBeNull] ConfigReader configReader = null)
        {
            PatcherNamespace namespaceOption = namespaces.FirstOrDefault(x => x.Name == selectedNamespaceName);
            if (namespaceOption is null)
            {
                throw new ArgumentException($"Namespace '{selectedNamespaceName}' not found in namespaces list");
            }

            string tslPatchDataPathResolved = new CaseAwarePath(modPath, "tslpatchdata").GetResolvedPath();
            // Use the directory that contains the changes file (supports IniName like "subfolder/changes.ini" with path in filename)
            string fullChangesPath = Path.Combine(tslPatchDataPathResolved, namespaceOption.ChangesFilePath());
            string namespaceDir = Path.GetDirectoryName(fullChangesPath) ?? tslPatchDataPathResolved;
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(namespaceOption.IniFilename) ?? "changes");
            var namespaceDirPath = new CaseAwarePath(namespaceDir);
            string lang = ConfigLanguageCode;

            string resolvedChangesPath;
            var (localizedPath, _) = ResolveLocalizedConfigFile(namespaceDirPath, baseName, lang, tryYaml: true);
            if (localizedPath != null)
            {
                resolvedChangesPath = localizedPath;
            }
            else
            {
                var changesIniPath = new CaseAwarePath(modPath, "tslpatchdata", namespaceOption.ChangesFilePath());
                resolvedChangesPath = changesIniPath.GetResolvedPath();
            }

            ConfigReader reader = configReader ?? ConfigReader.FromFilePath(resolvedChangesPath, tslPatchDataPath: tslPatchDataPathResolved);
            if (configReader is null)
            {
                reader.Load(reader.Config); // Load() populates the Config
                // When loading from INI (not namespace list), create equivalent .yaml alongside
                if (resolvedChangesPath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    reader.WriteEquivalentYaml(Path.ChangeExtension(resolvedChangesPath, ".yaml"));
                }
            }

            int? gameNumber = reader.Config.GameNumber;
            // Can be null if game number not set
            Game? game = gameNumber.HasValue ? (Game?)gameNumber.Value : null;

            var gamePaths = new List<string>();
            Dictionary<Game, List<string>> detectedPaths = FindKotorPathsFromDefault();
            if (game.HasValue)
            {
                // GameNumber set: show paths for that game (and K1 paths when game is TSL)
                if (detectedPaths.TryGetValue(game.Value, out List<string> paths))
                {
                    gamePaths.AddRange(paths);
                }
                if (game.Value == Game.TSL && detectedPaths.TryGetValue(Game.K1, out List<string> k1Paths))
                {
                    gamePaths.AddRange(k1Paths);
                }
            }
            else
            {
                // No GameNumber in ini: show all detected paths (both K1 and TSL) found on disk
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (List<string> paths in detectedPaths.Values)
                {
                    foreach (string p in paths)
                    {
                        if (seen.Add(p))
                        {
                            gamePaths.Add(p);
                        }
                    }
                }
                gamePaths = gamePaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            }

            // Load info.rtf only (exclusively RTF; no .rte / RTE JSON support)
            var infoRtfPath = new CaseAwarePath(modPath, "tslpatchdata", namespaceOption.RtfFilePath());
            string rtfPathStr = infoRtfPath.GetResolvedPath();

            string infoContent = null;
            bool isRtf = false;
            if (infoRtfPath.IsFile())
            {
                byte[] data = File.ReadAllBytes(infoRtfPath.GetResolvedPath());
                infoContent = DecodeBytesWithFallbacks(data);
                isRtf = true;
            }

            return new NamespaceInfo
            {
                ConfigReader = reader,
                LogLevel = reader.Config.LogLevel,
                GameNumber = gameNumber,
                GamePaths = gamePaths,
                InfoContent = infoContent,
                IsRtf = isRtf
            };
        }

        /// <summary>
        /// Validates a KOTOR game directory.
        /// </summary>
        public static string ValidateGameDirectory(string directoryPath)
        {
            var directory = new CaseAwarePath(directoryPath);
            if (!directory.IsDirectory())
            {
                throw new ArgumentException($"Invalid KOTOR directory: {directoryPath}");
            }
            return directory.GetResolvedPath();
        }

        /// <summary>
        /// Validates that mod and game paths are ready for installation.
        /// </summary>
        public static bool ValidateInstallPaths(string modPath, string gamePath)
        {
            return !string.IsNullOrEmpty(modPath) &&
                   new CaseAwarePath(modPath).IsDirectory() &&
                   !string.IsNullOrEmpty(gamePath) &&
                   new CaseAwarePath(gamePath).IsDirectory();
        }

        /// <summary>
        /// Gets the description for a namespace by name.
        /// </summary>
        public static string GetNamespaceDescription(List<PatcherNamespace> namespaces, string selectedNamespaceName)
        {
            // Can be null if namespace not found
            PatcherNamespace namespaceOption = namespaces.FirstOrDefault(x => x.Name == selectedNamespaceName);
            return namespaceOption?.Description ?? "";
        }

        /// <summary>
        /// Calculates total number of patches for progress calculation.
        /// </summary>
        public static int CalculateTotalPatches(ModInstaller installer)
        {
            PatcherConfig config = installer.Config();
            // Count TLK patches manually since GetTlkPatches is private
            int tlkPatches = config.PatchesTLK.Modifiers.Count;
            return config.InstallList.Count +
                   tlkPatches +
                   config.Patches2DA.Count +
                   config.PatchesGFF.Count +
                   config.PatchesNSS.Count +
                   config.PatchesNCS.Count +
                   config.PatchesSSF.Count;
        }

        /// <summary>
        /// Gets confirmation message if mod requires it.
        /// </summary>
        [CanBeNull]
        public static string GetConfirmMessage(ModInstaller installer)
        {
            string msg = installer.Config().ConfirmMessage?.Trim() ?? "";
            return !string.IsNullOrEmpty(msg) && msg != "N/A" ? msg : null;
        }

        /// <summary>
        /// Builds the TSLPatcher-style configuration summary (dry-run report) from the current config.
        /// Format matches TSLPatcher CONFIGURATION SUMMARY exactly.
        /// </summary>
        public static string BuildConfigurationSummary(
            string changesFileName,
            string infoFileName,
            PatcherConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================================");
            sb.AppendLine("TSLPatcher - CONFIGURATION SUMMARY");
            sb.AppendLine("==================================");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Settings:");
            sb.AppendLine("---------");
            sb.Append("Config file: ");
            sb.AppendLine(changesFileName);
            sb.Append("Information file: ");
            sb.AppendLine(infoFileName);
            sb.AppendLine("Install location: User selected.");
            sb.AppendLine("Make backups: Before modifying/overwriting existing files.");
            sb.Append("Log level: ");
            sb.Append((int)config.LogLevel);
            sb.Append(" - ");
            sb.AppendLine(LogLevelSummary(config.LogLevel));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("dialog tlk appending:");
            sb.AppendLine("---------------------");
            int tlkAppendCount = config.PatchesTLK?.Modifiers?.Count(m => !m.IsReplacement) ?? 0;
            sb.Append("New entries: ");
            sb.AppendLine(tlkAppendCount.ToString());
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("2DA file changes:");
            sb.AppendLine("-----------------");
            if (config.Patches2DA == null || config.Patches2DA.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (Modifications2DA m in config.Patches2DA)
                {
                    int newRows = m.Modifiers.Count(x => x is AddRow2DA || x is CopyRow2DA);
                    int modifiedRows = m.Modifiers.Count(x => x is ChangeRow2DA);
                    int newColumns = m.Modifiers.Count(x => x is AddColumn2DA);
                    string file = m.SaveAs ?? m.SourceFile ?? "unknown";
                    sb.Append(" * ");
                    sb.Append(file);
                    sb.Append(" - new rows: ");
                    sb.Append(newRows);
                    sb.Append(", modified rows: ");
                    sb.Append(modifiedRows);
                    sb.Append(", new columns: ");
                    sb.AppendLine(newColumns.ToString());
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("GFF file changes:");
            sb.AppendLine("-----------------");
            if (config.PatchesGFF == null || config.PatchesGFF.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (ModificationsGFF m in config.PatchesGFF)
                {
                    string file = m.SaveAs ?? m.SourceFile ?? "unknown";
                    string dest = m.Destination ?? "override";
                    sb.Append(" * ");
                    sb.Append(file);
                    sb.Append(" - modify existing, location: ");
                    sb.AppendLine(dest.ToLowerInvariant());
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("NCS file integer hacks:");
            sb.AppendLine("-----------------------");
            if (config.PatchesNCS == null || config.PatchesNCS.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (ModificationsNCS m in config.PatchesNCS)
                {
                    sb.Append(" * ");
                    sb.AppendLine(m.SaveAs ?? m.SourceFile ?? "unknown");
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Modified & recompiled scripts:");
            sb.AppendLine("------------------------------");
            if (config.PatchesNSS == null || config.PatchesNSS.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (ModificationsNSS m in config.PatchesNSS)
                {
                    sb.Append(" * ");
                    sb.AppendLine(m.SaveAs ?? m.SourceFile ?? "unknown");
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("New/modified Soundset files:");
            sb.AppendLine("----------------------------");
            if (config.PatchesSSF == null || config.PatchesSSF.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (ModificationsSSF m in config.PatchesSSF)
                {
                    sb.Append(" * ");
                    sb.AppendLine(m.SaveAs ?? m.SourceFile ?? "unknown");
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Unpatched files to install:");
            sb.AppendLine("---------------------------");
            if (config.InstallList == null || config.InstallList.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                var byDest = config.InstallList
                    .GroupBy(f => f.Destination ?? "Override")
                    .OrderBy(g => g.Key);
                foreach (var group in byDest)
                {
                    sb.Append(" * Location: ");
                    sb.AppendLine(group.Key);
                    foreach (InstallFile f in group)
                    {
                        string action = f.ReplaceFile ? "overwrite" : "skip existing";
                        sb.Append("   --> ");
                        sb.Append(f.SaveAs ?? f.SourceFile ?? "unknown");
                        sb.Append(" - ");
                        sb.AppendLine(action);
                    }
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static string LogLevelSummary(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Nothing: return "Nothing: No feedback.";
                case LogLevel.General: return "General: Progress only.";
                case LogLevel.Errors: return "Errors: Progress and errors.";
                case LogLevel.Warnings: return "Standard: Progress, errors and warnings.";
                case LogLevel.Full: return "Full: Verbose progress (debugging).";
                default: return "Standard: Progress, errors and warnings.";
            }
        }

        /// <summary>
        /// Installs a mod.
        /// </summary>
        public static InstallResult InstallMod(
            string modPath,
            string gamePath,
            List<PatcherNamespace> namespaces,
            string selectedNamespaceName,
            PatchLogger logger,
            CancellationToken cancellationToken,
            [CanBeNull] Action<int> progressCallback = null)
        {
            // Can be null if namespace not found
            PatcherNamespace namespaceOption = namespaces.FirstOrDefault(x => x.Name == selectedNamespaceName);
            if (namespaceOption is null)
            {
                throw new ArgumentException($"Namespace '{selectedNamespaceName}' not found in namespaces list");
            }

            string tslPatchDataPath = new CaseAwarePath(modPath, "tslpatchdata").GetResolvedPath();
            string iniFilePath = new CaseAwarePath(tslPatchDataPath, namespaceOption.ChangesFilePath()).GetResolvedPath();
            string namespaceModPath = Path.GetDirectoryName(iniFilePath) ?? tslPatchDataPath;

            var installer = new ModInstaller(namespaceModPath, gamePath, iniFilePath, logger)
            {
                TslPatchDataPath = tslPatchDataPath
            };

            DateTime installStartTime = DateTime.UtcNow;
            installer.Install(cancellationToken, progressCallback);
            TimeSpan totalInstallTime = DateTime.UtcNow - installStartTime;

            int numErrors = logger.Errors.Count();
            int numWarnings = logger.Warnings.Count();
            int numPatches = installer.Config().PatchCount();

            string timeStr = FormatInstallTime(totalInstallTime);
            logger.AddNote(string.Format(CultureInfo.CurrentCulture, UIResources.InstallationCompleteWithErrorsAndWarningsFormat, numErrors, numWarnings, Environment.NewLine, timeStr, numPatches));

            return new InstallResult
            {
                InstallTime = totalInstallTime,
                NumErrors = numErrors,
                NumWarnings = numWarnings,
                NumPatches = numPatches
            };
        }

        /// <summary>
        /// Validates a mod's configuration.
        /// </summary>
        public static void ValidateConfig(
            string modPath,
            List<PatcherNamespace> namespaces,
            string selectedNamespaceName,
            PatchLogger logger)
        {
            // Can be null if namespace not found
            PatcherNamespace namespaceOption = namespaces.FirstOrDefault(x => x.Name == selectedNamespaceName);
            if (namespaceOption is null)
            {
                throw new ArgumentException($"Namespace '{selectedNamespaceName}' not found in namespaces list");
            }

            string iniFilePath = new CaseAwarePath(modPath, "tslpatchdata", namespaceOption.ChangesFilePath()).GetResolvedPath();
            string tslPatchDataPath = new CaseAwarePath(modPath, "tslpatchdata").GetResolvedPath();

            var reader = ConfigReader.FromFilePath(iniFilePath, logger, tslPatchDataPath: tslPatchDataPath);
            reader.Load(reader.Config);
            if (iniFilePath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                reader.WriteEquivalentYaml(Path.ChangeExtension(iniFilePath, ".yaml"));
            }
        }

        /// <summary>
        /// Localized strings for <see cref="ModUninstaller"/> dialogs (current UI culture).
        /// </summary>
        [NotNull]
        public static ModUninstallerUiStrings CreateModUninstallerUiStrings()
        {
            return new ModUninstallerUiStrings
            {
                NoBackupsTitle = UIResources.NoBackupsFoundTitle,
                GetNoBackupsMessage = path => string.Format(CultureInfo.CurrentCulture, UIResources.NoBackupsFoundMessageFormat, path, Environment.NewLine),
                BackupMismatchTitle = UIResources.BackupOutOfDateOrMismatched,
                GetBackupMismatchMessage = () => string.Format(CultureInfo.CurrentCulture, UIResources.BackupMismatchMessageFormat, Environment.NewLine),
                ConfirmationTitle = UIResources.Confirmation,
                GetReallyUninstallMessage = (existing, files, folders) => string.Format(CultureInfo.CurrentCulture, UIResources.ReallyUninstallConfirmFormat, existing, files, folders, Environment.NewLine),
                GetFailedToRestoreMessage = exMessage => string.Format(CultureInfo.CurrentCulture, UIResources.FailedToRestoreBackupFormat, Environment.NewLine, exMessage),
                UninstallCompletedTitle = UIResources.UninstallCompletedTitle,
                GetDeleteBackupPromptMessage = (deletedCount, backupName) => string.Format(CultureInfo.CurrentCulture, UIResources.UninstallCompletedDeleteBackupFormat, deletedCount, backupName, Environment.NewLine),
                PermissionErrorTitle = UIResources.PermissionErrorTitle,
                UnableToDeleteBackupPermissionMessage = UIResources.UnableToDeleteBackupPermissionFormat,
                GainingPermissionPleaseWait = UIResources.GainingPermissionPleaseWait,
            };
        }

        /// <summary>
        /// Uninstalls a mod using its backup.
        /// </summary>
        public static bool UninstallMod(
            string modPath,
            string gamePath,
            PatchLogger logger,
            [CanBeNull] Func<string, string, bool> showYesNoDialog = null,
            [CanBeNull] Func<string, string, bool?> showYesNoCancelDialog = null,
            [CanBeNull] Action<string, string> showErrorDialog = null)
        {
            string backupParentFolder = Path.Combine(modPath, "backup");
            if (!Directory.Exists(backupParentFolder))
            {
                throw new DirectoryNotFoundException($"Backup folder not found: {backupParentFolder}");
            }

            var uninstaller = new ModUninstaller(
                new CaseAwarePath(backupParentFolder),
                new CaseAwarePath(gamePath),
                logger);

            return uninstaller.UninstallSelectedMod(
                showErrorDialog: showErrorDialog ?? ((title, msg) => { }),
                showYesNoDialog: showYesNoDialog ?? ((title, msg) => true),
                showYesNoCancelDialog: showYesNoCancelDialog ?? ((title, msg) => true),
                ui: CreateModUninstallerUiStrings());
        }

        /// <summary>
        /// Formats an installation time as a human-readable string.
        /// </summary>
        public static string FormatInstallTime(TimeSpan installTime)
        {
            int days = (int)installTime.TotalDays;
            int hours = installTime.Hours;
            int minutes = installTime.Minutes;
            int seconds = installTime.Seconds;

            var parts = new List<string>();
            if (days > 0) { parts.Add($"{days} {(days == 1 ? UIResources.TimeUnitDay : UIResources.TimeUnitDays)}"); }
            if (hours > 0) { parts.Add($"{hours} {(hours == 1 ? UIResources.TimeUnitHour : UIResources.TimeUnitHours)}"); }
            if (minutes > 0 || (days == 0 && hours == 0)) { parts.Add($"{minutes} {(minutes == 1 ? UIResources.TimeUnitMinute : UIResources.TimeUnitMinutes)}"); }
            parts.Add($"{seconds} {(seconds == 1 ? UIResources.TimeUnitSecond : UIResources.TimeUnitSeconds)}");

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Gets the log file path for a mod.
        /// </summary>
        public static string GetLogFilePath(string modPath)
        {
            return Path.Combine(modPath, "installlog.txt");
        }

        /// <summary>
        /// Returns all detected KOTOR installation paths (K1 + TSL), flattened and deduplicated.
        /// Matches HoloPatcher behavior: same sources as find_kotor_paths_from_default() at UI init.
        /// </summary>
        public static List<string> GetDetectedKotorPaths()
        {
            Dictionary<Game, List<string>> byGame = FindKotorPathsFromDefault();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (List<string> paths in byGame.Values)
            {
                foreach (string path in paths)
                {
                    if (!string.IsNullOrEmpty(path) && seen.Add(path))
                    {
                        result.Add(path);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Decodes bytes with fallback encodings.
        /// </summary>
        private static string DecodeBytesWithFallbacks(byte[] data)
        {
            // Try UTF-8 first
            try
            {
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Fallback to Windows-1252
                try
                {
                    Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    return Encoding.GetEncoding("windows-1252").GetString(data);
                }
                catch
                {
                    // Final fallback to ASCII
                    return Encoding.ASCII.GetString(data);
                }
            }
        }

        /// <summary>
        /// Strips RTF formatting from text.
        /// Uses RtfStripper class which matches Python's striprtf implementation.
        /// </summary>
        private static string StripRtf(string rtfText)
        {
            return RtfStripper.StripRtf(rtfText);
        }

        /// <summary>
        /// Finds existing KOTOR paths: default paths (expanded, resolved, existence-filtered)
        /// plus Windows registry paths when on Windows. Returns sorted, deduplicated lists per game.
        /// </summary>
        private static Dictionary<Game, List<string>> FindKotorPathsFromDefault()
        {
            Dictionary<Game, List<string>> defaults = GetDefaultPaths();
            var result = new Dictionary<Game, HashSet<string>>
            {
                [Game.K1] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [Game.TSL] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };

            foreach (KeyValuePair<Game, List<string>> kv in defaults)
            {
                Game key = kv.Key == Game.K2 ? Game.TSL : kv.Key;
                if (!result.ContainsKey(key))
                {
                    result[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                foreach (string pathTemplate in kv.Value)
                {
                    string expanded = ExpandPath(pathTemplate);
                    string resolved = Path.GetFullPath(expanded);
                    if (Directory.Exists(resolved))
                    {
                        result[key].Add(resolved);
                    }
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddRegistryPaths(result);
            }

            return new Dictionary<Game, List<string>>
            {
                [Game.K1] = result[Game.K1].OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList(),
                [Game.TSL] = result[Game.TSL].OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        /// <summary>
        /// Returns default installation path templates for the current OS (with ~ for home).
        /// </summary>
        private static Dictionary<Game, List<string>> GetDefaultPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetDefaultPathsWindows();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetDefaultPathsDarwin();
            }

            return GetDefaultPathsLinux();
        }

        private static Dictionary<Game, List<string>> GetDefaultPathsWindows()
        {
            return new Dictionary<Game, List<string>>
            {
                [Game.K1] = new List<string>
                {
                    @"C:\Program Files\Steam\steamapps\common\swkotor",
                    @"C:\Program Files (x86)\Steam\steamapps\common\swkotor",
                    @"C:\Program Files\LucasArts\SWKotOR",
                    @"C:\Program Files (x86)\LucasArts\SWKotOR",
                    @"C:\GOG Games\Star Wars - KotOR",
                    @"C:\Amazon Games\Library\Star Wars - Knights of the Old"
                },
                [Game.TSL] = new List<string>
                {
                    @"C:\Program Files\Steam\steamapps\common\Knights of the Old Republic II",
                    @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II",
                    @"C:\Program Files\LucasArts\SWKotOR2",
                    @"C:\Program Files (x86)\LucasArts\SWKotOR2",
                    @"C:\GOG Games\Star Wars - KotOR2"
                }
            };
        }

        private static Dictionary<Game, List<string>> GetDefaultPathsDarwin()
        {
            return new Dictionary<Game, List<string>>
            {
                [Game.K1] = new List<string>
                {
                    "~/Library/Application Support/Steam/steamapps/common/swkotor/Knights of the Old Republic.app/Contents/Assets",
                    "~/Library/Applications/Steam/steamapps/common/swkotor/Knights of the Old Republic.app/Contents/Assets/"
                },
                [Game.TSL] = new List<string>
                {
                    "~/Library/Application Support/Steam/steamapps/common/Knights of the Old Republic II/Knights of the Old Republic II.app/Contents/Assets",
                    "~/Library/Applications/Steam/steamapps/common/Knights of the Old Republic II/Star Wars™: Knights of the Old Republic II.app/Contents/GameData",
                    "~/Library/Application Support/Steam/steamapps/common/Knights of the Old Republic II/KOTOR2.app/Contents/GameData/",
                    "~/Applications/Knights of the Old Republic 2.app/Contents/Resources/transgaming/c_drive/Program Files/SWKotOR2/",
                    "/Applications/Knights of the Old Republic 2.app/Contents/Resources/transgaming/c_drive/Program Files/SWKotOR2/"
                }
            };
        }

        private static Dictionary<Game, List<string>> GetDefaultPathsLinux()
        {
            return new Dictionary<Game, List<string>>
            {
                [Game.K1] = new List<string>
                {
                    "~/.local/share/steam/common/steamapps/swkotor",
                    "~/.steam/root/steamapps/common/swkotor",
                    "~/.steam/debian-installation/steamapps/common/swkotor",
                    "~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/swkotor",
                    "/mnt/C/Program Files/Steam/steamapps/common/swkotor",
                    "/mnt/C/Program Files (x86)/Steam/steamapps/common/swkotor",
                    "/mnt/C/Program Files/LucasArts/SWKotOR",
                    "/mnt/C/Program Files (x86)/LucasArts/SWKotOR",
                    "/mnt/C/GOG Games/Star Wars - KotOR",
                    "/mnt/C/Amazon Games/Library/Star Wars - Knights of the Old"
                },
                [Game.TSL] = new List<string>
                {
                    "~/.local/share/Steam/common/steamapps/Knights of the Old Republic II",
                    "~/.steam/root/steamapps/common/Knights of the Old Republic II",
                    "~/.steam/debian-installation/steamapps/common/Knights of the Old Republic II",
                    "~/.local/share/aspyr-media/kotor2",
                    "~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Knights of the Old Republic II/steamassets",
                    "/mnt/C/Program Files/Steam/steamapps/common/Knights of the Old Republic II",
                    "/mnt/C/Program Files (x86)/Steam/steamapps/common/Knights of the Old Republic II",
                    "/mnt/C/Program Files/LucasArts/SWKotOR2",
                    "/mnt/C/Program Files (x86)/LucasArts/SWKotOR2",
                    "/mnt/C/GOG Games/Star Wars - KotOR2"
                }
            };
        }

        private static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            string expanded = path;
            if (expanded.StartsWith("~", StringComparison.Ordinal))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (expanded.Length == 1 || expanded[1] == '/' || expanded[1] == '\\')
                {
                    expanded = home + expanded.Substring(1);
                }
                else
                {
                    expanded = Path.Combine(home, expanded.Substring(1));
                }
            }

            return Path.GetFullPath(expanded);
        }

        private static void AddRegistryPaths(Dictionary<Game, HashSet<string>> result)
        {
            try
            {
                AddRegistryPathsK1(result);
                AddRegistryPathsTsl(result);
                AddAmazonK1Path(result);
            }
            catch
            {
                // Registry access can fail (permissions, not Windows); ignore.
            }
        }

        private static void AddRegistryPathsK1(Dictionary<Game, HashSet<string>> result)
        {
            string path;
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 32370", "InstallLocation");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.K1].Add(Path.GetFullPath(path));
            }

            string subkey = Environment.Is64BitProcess
                ? @"SOFTWARE\WOW6432Node\GOG.com\Games\1207666283"
                : @"SOFTWARE\GOG.com\Games\1207666283";
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "PATH");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.K1].Add(Path.GetFullPath(path));
            }

            subkey = Environment.Is64BitProcess
                ? @"SOFTWARE\WOW6432Node\BioWare\SW\KOTOR"
                : @"SOFTWARE\BioWare\SW\KOTOR";
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "InternalPath");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.K1].Add(Path.GetFullPath(path));
            }

            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "Path");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.K1].Add(Path.GetFullPath(path));
            }
        }

        private static void AddRegistryPathsTsl(Dictionary<Game, HashSet<string>> result)
        {
            string path;
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 208580", "InstallLocation");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.TSL].Add(Path.GetFullPath(path));
            }

            string subkey = Environment.Is64BitProcess
                ? @"SOFTWARE\WOW6432Node\GOG.com\Games\1421404581"
                : @"SOFTWARE\GOG.com\Games\1421404581";
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "PATH");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.TSL].Add(Path.GetFullPath(path));
            }

            subkey = Environment.Is64BitProcess
                ? @"SOFTWARE\WOW6432Node\LucasArts\KotOR2"
                : @"SOFTWARE\LucasArts\KotOR2";
            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "InternalPath");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.TSL].Add(Path.GetFullPath(path));
            }

            path = GetRegistryValue(Microsoft.Win32.RegistryHive.LocalMachine, subkey, "Path");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.TSL].Add(Path.GetFullPath(path));
            }
        }

        private static void AddAmazonK1Path(Dictionary<Game, HashSet<string>> result)
        {
            string path = FindAmazonGamesPath("AmazonGames/Star Wars - Knights of the Old");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                result[Game.K1].Add(Path.GetFullPath(path));
            }
        }

        private static string GetRegistryValue(Microsoft.Win32.RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(hive,
                    Environment.Is64BitProcess ? Microsoft.Win32.RegistryView.Registry64 : Microsoft.Win32.RegistryView.Registry32))
                using (Microsoft.Win32.RegistryKey key = baseKey.OpenSubKey(keyPath))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    object value = key.GetValue(valueName);
                    return value?.ToString()?.Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        private static string FindAmazonGamesPath(string softwareName)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey hkeyUsers = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.Users, Microsoft.Win32.RegistryView.Default))
                {
                    string[] sids = hkeyUsers.GetSubKeyNames();
                    foreach (string sid in sids)
                    {
                        string subKeyPath = sid + @"\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + softwareName;
                        try
                        {
                            using (Microsoft.Win32.RegistryKey key = hkeyUsers.OpenSubKey(subKeyPath))
                            {
                                if (key != null)
                                {
                                    object value = key.GetValue("InstallLocation");
                                    string path = value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        return path;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Skip this SID
                        }
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }
    }
}


