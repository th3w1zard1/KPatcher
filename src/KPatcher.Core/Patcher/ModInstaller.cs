using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using IniParser.Model;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Config;
using KPatcher.Core.Formats.ERF;
using KPatcher.Core.Formats.RIM;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.GFF;
using KPatcher.Core.Mods.NCS;
using KPatcher.Core.Mods.NSS;
using KPatcher.Core.Mods.SSF;
using KPatcher.Core.Mods.TLK;
using KPatcher.Core.Mods.TwoDA;
using KPatcher.Core.Reader;
using KPatcher.Core.Resources;
using KPatcher.Core.Tools;

namespace KPatcher.Core.Patcher
{

    /// <summary>
    /// Main orchestrator for installing KPatcher mods.
    /// </summary>
    public class ModInstaller
    {
        private readonly string modPath;
        private readonly string gamePath;
        private readonly string changesIniPath;
        private readonly PatchLogger log;
        [CanBeNull]
        private InstallLogWriter installLog;

        [CanBeNull]
        private PatcherConfig config;
        [CanBeNull]
        private string backup;
        private readonly HashSet<string> processedBackupFiles = new HashSet<string>();

        public Game? Game { get; private set; }
        [CanBeNull]
        public string TslPatchDataPath { get; set; }

        public ModInstaller(
            string modPath,
            string gamePath,
            string changesIniPath,
            [CanBeNull] PatchLogger logger = null)
        {
            this.modPath = modPath ?? throw new ArgumentNullException(nameof(modPath));
            this.gamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
            this.changesIniPath = changesIniPath ?? throw new ArgumentNullException(nameof(changesIniPath));
            log = logger ?? new PatchLogger();

            Game = Heuristics.DetermineGame(this.gamePath, log);
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModInstaller ctor: modPath={0}, gamePath={1}, initialChangesIni={2}, game={3}",
                this.modPath, this.gamePath, this.changesIniPath, Game?.ToString() ?? "null"));

            // Handle legacy syntax - look for changes.ini in various locations
            if (!File.Exists(this.changesIniPath))
            {
                string fileName = Path.GetFileName(this.changesIniPath);
                this.changesIniPath = Path.Combine(this.modPath, fileName);

                if (!File.Exists(this.changesIniPath))
                {
                    this.changesIniPath = Path.Combine(this.modPath, "tslpatchdata", fileName);
                }

                if (!File.Exists(this.changesIniPath))
                {
                    throw new FileNotFoundException(
                        PatcherResources.CouldNotFindChangesIniFile,
                        this.changesIniPath);
                }
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModInstaller ctor: resolved changesIniPath={0}, modDirectory={1}",
                this.changesIniPath, Path.GetDirectoryName(this.changesIniPath) ?? this.modPath));

            // Initialize install log writer in the mod directory (where changes.ini is located)
            string modDirectory = Path.GetDirectoryName(this.changesIniPath) ?? this.modPath;
            try
            {
                installLog = new InstallLogWriter(modDirectory);
                installLog.WriteHeader(modDirectory, this.gamePath, Game);

                // Subscribe to PatchLogger events to also write to install log
                log.LogAdded += OnPatchLoggerLogAdded;
            }
            catch (Exception ex)
            {
                // Log error but don't fail installation if log file can't be created
                log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotCreateInstallLogFile, ex.Message));
            }
        }

        /// <summary>
        /// Handles PatchLogger log events and writes them to the install log file.
        /// </summary>
        private void OnPatchLoggerLogAdded(object sender, PatchLog logEntry)
        {
            if (installLog == null)
            {
                return;
            }

            try
            {
                switch (logEntry.LogType)
                {
                    case LogType.Error:
                        installLog.WriteError(logEntry.Message);
                        break;
                    case LogType.Warning:
                        installLog.WriteWarning(logEntry.Message);
                        break;
                    case LogType.Note:
                    case LogType.Verbose:
                        installLog.WriteInfo(logEntry.Message);
                        break;
                    case LogType.Diagnostic:
                        break;
                }
            }
            catch
            {
                // Ignore errors when writing to install log to avoid breaking installation
            }
        }

        /// <summary>
        /// Gets the patcher configuration, loading it if necessary.
        /// </summary>
        public PatcherConfig Config()
        {
            if (config != null)
            {
                log.AddDiagnostic("ModInstaller.Config: returning cached PatcherConfig instance");
                return config;
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModInstaller.Config: loading from path={0}, isYaml={1}",
                changesIniPath,
                Path.GetExtension(changesIniPath).Equals(".yaml", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(changesIniPath).Equals(".yml", StringComparison.OrdinalIgnoreCase)));

            if (!File.Exists(changesIniPath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, PatcherResources.ChangesConfigFileNotFound, changesIniPath));
            }

            string ext = Path.GetExtension(changesIniPath);
            bool isYaml = ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || ext.Equals(".yml", StringComparison.OrdinalIgnoreCase);
            IniData ini;
            if (isYaml)
            {
                ini = ConfigReaderYaml.LoadAndParseYaml(changesIniPath);
            }
            else
            {
                // ini_file_bytes: bytes = self.changes_ini_path.read_bytes()
                // ini_text: str = decode_bytes_with_fallbacks(ini_file_bytes)
                byte[] iniFileBytes = File.ReadAllBytes(changesIniPath);
                string iniText;
                try
                {
                    iniText = Encoding.UTF8.GetString(iniFileBytes);
                    if (iniText.Contains('\uFFFD'))
                    {
                        throw new DecoderFallbackException("UTF-8 decode failed");
                    }
                }
                catch (DecoderFallbackException)
                {
                    try
                    {
                        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        iniText = Encoding.GetEncoding("windows-1252").GetString(iniFileBytes);
                    }
                    catch
                    {
                        log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotDetermineEncoding, Path.GetFileName(changesIniPath)));
                        iniText = Encoding.UTF8.GetString(iniFileBytes);
                    }
                }
                ini = ConfigReader.ParseIniText(iniText, caseInsensitive: false, sourcePath: changesIniPath);
            }

            ConfigReader reader = new ConfigReader(ini, modPath, log, TslPatchDataPath);
            config = reader.Load(new PatcherConfig());
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModInstaller.Config: loaded patch counts installList={0} twoDa={1} gff={2} tlkMods={3} nss={4} ncs={5} ssf={6}",
                config.InstallList.Count,
                config.Patches2DA.Count,
                config.PatchesGFF.Count,
                config.PatchesTLK.Modifiers.Count,
                config.PatchesNSS.Count,
                config.PatchesNCS.Count,
                config.PatchesSSF.Count));

            // When loading from INI, create equivalent .yaml alongside (same dir, same base name)
            if (!isYaml && changesIniPath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    reader.WriteEquivalentYaml(Path.ChangeExtension(changesIniPath, ".yaml"));
                }
                catch (Exception ex)
                {
                    log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotWriteEquivalentYaml, ex.Message));
                }
            }

            // Check required files (if self._config.required_files:)
            if (config.RequiredFiles.Count > 0)
            {
                for (int i = 0; i < config.RequiredFiles.Count; i++)
                {
                    string[] files = config.RequiredFiles[i];
                    foreach (string file in files)
                    {
                        string requiredFilePath = Path.Combine(gamePath, "Override", file);
                        if (!File.Exists(requiredFilePath))
                        {
                            string message = i < config.RequiredMessages.Count
                                ? config.RequiredMessages[i].Trim()
                                : "cannot install - missing a required mod";
                            throw new InvalidOperationException(message);
                        }
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Creates a backup directory and returns its path.
        /// </summary>
        public (string backupPath, HashSet<string> processedFiles) GetBackup()
        {
            if (backup != null)
            {
                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "GetBackup: returning existing backup={0}, processedFiles={1}",
                    backup, processedBackupFiles.Count));
                return (backup, processedBackupFiles);
            }

            string backupDir = modPath;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");

            // Find the root directory containing tslpatchdata
            // while not backup_dir.joinpath("tslpatchdata").is_dir() and backup_dir.parent.name:
            // then checks backup_dir.parent.name (which is empty string for root), C# checks if GetDirectoryName is not null/empty
            // Can be null if not found
            string parentDir = Path.GetDirectoryName(backupDir);
            while (!Directory.Exists(Path.Combine(backupDir, "tslpatchdata")) &&
                   !string.IsNullOrEmpty(parentDir) && !string.IsNullOrEmpty(Path.GetFileName(parentDir)))
            {
                backupDir = parentDir;
                parentDir = Path.GetDirectoryName(backupDir);
            }

            // Remove old uninstall directory if it exists
            string uninstallDir = Path.Combine(backupDir, "uninstall");
            if (Directory.Exists(uninstallDir))
            {
                try
                {
                    Directory.Delete(uninstallDir, recursive: true);
                }
                catch (Exception ex)
                {
                    log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotInitializeUninstallDirectory, ex.Message));
                }
            }

            // Create new backup directory
            backupDir = Path.Combine(backupDir, "backup", timestamp);
            try
            {
                Directory.CreateDirectory(backupDir);
            }
            catch (Exception ex)
            {
                log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotCreateBackupFolder, ex.Message));
            }

            log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.UsingBackupDirectory, backupDir));
            backup = backupDir;
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetBackup: created backupDir={0}, timestampToken={1}", backupDir, timestamp));

            return (backup, processedBackupFiles);
        }

        /// <summary>
        /// Installs the mod by applying all patches.
        /// </summary>
        public void Install(
            CancellationToken? cancellationToken = null,
            [CanBeNull] Action<int> progressCallback = null)
        {
            try
            {
                if (Game is null)
                {
                    throw new InvalidOperationException(
                        "Chosen KOTOR directory is not a valid installation - cannot initialize ModInstaller.");
                }

                installLog?.WriteInfo(PatcherResources.StartingInstallation);
                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "Install: start game={0} gamePath={1} modPath={2} changesIni={3}",
                    Game, gamePath, modPath, changesIniPath));

                PatcherMemory memory = new PatcherMemory();
                PatcherConfig cfg = Config();

                installLog?.WriteInfo(string.Format(CultureInfo.CurrentCulture, PatcherResources.LoadingConfigurationFrom, Path.GetFileName(changesIniPath)));
                installLog?.WriteInfo(string.Format(CultureInfo.CurrentCulture, PatcherResources.FoundPatchesToApply, cfg.InstallList.Count + cfg.Patches2DA.Count + cfg.PatchesGFF.Count + cfg.PatchesTLK.Modifiers.Count + cfg.PatchesNSS.Count + cfg.PatchesNCS.Count + cfg.PatchesSSF.Count));

                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "Install: ordered queue TLK+install+2DA+GFF+NSS+NCS+SSF; TslPatchDataPath={0}",
                    TslPatchDataPath ?? "null"));

                List<PatcherModifications> patchesList = new List<PatcherModifications>();
                // TSLPatcher patch order: TLK -> InstallList -> 2DA -> GFF -> NSS -> NCS -> SSF
                patchesList.AddRange(GetTlkPatches(cfg));
                patchesList.AddRange(cfg.InstallList);
                patchesList.AddRange(cfg.Patches2DA);
                patchesList.AddRange(cfg.PatchesGFF);
                patchesList.AddRange(cfg.PatchesNSS);
                patchesList.AddRange(cfg.PatchesNCS);
                patchesList.AddRange(cfg.PatchesSSF);

                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "Install: total patch objects in run order={0}", patchesList.Count));

                bool finishedPreprocessedScripts = false;
                // Can be null if not found
                string tempScriptFolder = null;

                bool processingInstallList = true;
                bool processing2DA = false;
                bool processingGFF = false;
                bool processingTLK = false;
                bool processingNSS = false;
                bool processingNCS = false;
                bool processingSSF = false;

                int patchOrdinal = 0;
                foreach (PatcherModifications patch in patchesList)
                {
                    patchOrdinal++;
                    log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "Install patch {0}/{1}: clrType={2} action={3} sourceFile={4} saveAs={5} destination={6} replaceFile={7} skipIfNotReplace={8}",
                        patchOrdinal,
                        patchesList.Count,
                        patch.GetType().Name,
                        patch.Action,
                        patch.SourceFile ?? "",
                        patch.SaveAs ?? "",
                        patch.Destination ?? "",
                        patch.ReplaceFile,
                        patch.SkipIfNotReplace));

                    cancellationToken?.ThrowIfCancellationRequested();

                    // if should_cancel is not None and should_cancel.is_set(): sys.exit()
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        log.AddNote(PatcherResources.InstallationTerminationRequest);
                        Environment.Exit(0);
                    }

                    // Log when we start processing different patch types
                    if (processingInstallList && patch is InstallFile)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingInstallListEntries);
                        processingInstallList = false;
                    }
                    else if (!processingTLK && patch is ModificationsTLK)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingTlkPatches);
                        processingTLK = true;
                    }
                    else if (!processing2DA && patch is Modifications2DA)
                    {
                        installLog?.WriteInfo(PatcherResources.Processing2DAPatches);
                        processing2DA = true;
                    }
                    else if (!processingGFF && patch is ModificationsGFF)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingGffPatches);
                        processingGFF = true;
                    }
                    else if (!processingNSS && patch is ModificationsNSS)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingNssScriptPatches);
                        processingNSS = true;
                    }
                    else if (!processingNCS && patch is ModificationsNCS)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingNcsScriptPatches);
                        processingNCS = true;
                    }
                    else if (!processingSSF && patch is ModificationsSSF)
                    {
                        installLog?.WriteInfo(PatcherResources.ProcessingSsfSoundPatches);
                        processingSSF = true;
                    }

                    // Must run preprocessed scripts directly before GFFList so we don't interfere with !FieldPath assignments to 2DAMEMORY.
                    if (!finishedPreprocessedScripts && patch is ModificationsNSS)
                    {
                        tempScriptFolder = PrepareCompileList(cfg, memory);
                        finishedPreprocessedScripts = true;
                    }

                    try
                    {
                        string destination = patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION;
                        string saveAs = patch.SaveAs ?? patch.SourceFile ?? "";
                        string outputContainerPath = Path.Combine(gamePath, destination);
                        log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "Install: outputContainerPath={0}, saveAs={1}", outputContainerPath, saveAs));

                        // TSLPatcher: do not overwrite dialog.tlk directly, but still apply TLK changes in memory so
                        // later patches can resolve TLK tokens from the same install queue.
                        bool skipDialogTlkWrite = patch is ModificationsTLK
                            && string.Equals(saveAs, "dialog.tlk", StringComparison.OrdinalIgnoreCase);
                        if (skipDialogTlkWrite)
                        {
                            log.AddNote(string.Format(System.Globalization.CultureInfo.CurrentCulture, KPatcher.Core.Common.TSLPatcherMessages.SkippingFileNoOverwriteDialogTlk, saveAs));
                        }

                        HandleCapsuleResult result = HandleCapsuleAndBackup(patch, outputContainerPath, destination, saveAs);
                        log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "Install: HandleCapsuleAndBackup exists={0} capsule={1}",
                            result.Exists,
                            result.Capsule != null ? result.Capsule.Path.GetResolvedPath() : "null"));

                        if (!ShouldPatch(patch, result.Exists, result.Capsule))
                        {
                            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                "Install: ShouldPatch=false, skipping ordinal={0}", patchOrdinal));
                            continue;
                        }

                        // Can be null if not found
                        byte[] dataToPatch = LookupResource(patch, outputContainerPath, saveAs, result.Exists, result.Capsule);

                        if (dataToPatch is null)
                        {
                            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                "Install: LookupResource returned null for ordinal={0} saveAs={1}", patchOrdinal, saveAs));
                            // TSLPatcher parity: No TLK file loaded / Unable to locate TLK file
                            if (patch is ModificationsTLK tlkPatch)
                                log.AddError(string.Format(TSLPatcherMessages.UnableToLocateTLKFileToPatch, tlkPatch.SaveAs ?? tlkPatch.SourceFile ?? "dialog.tlk"));
                            else if (patch is Modifications2DA twodaPatch)
                                log.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.UnableToFind2DAFileToModify, twodaPatch.SaveAs ?? patch.SourceFile ?? ""));
                            else
                                log.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.CriticalErrorUnableToLocateFileToPatch, patch.SourceFile ?? patch.SaveAs ?? ""));
                            continue;
                        }

                        if (dataToPatch.Length == 0)
                        {
                            log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.FileHasNoContent, patch.SourceFile));
                        }

                        log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "Install: input bytes length={0} for ordinal={1}", dataToPatch.Length, patchOrdinal));
                        object patchedData = patch.PatchResource(dataToPatch, memory, log, Game.Value);
                        log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "Install: PatchResource resultType={0} ordinal={1}",
                            patchedData?.GetType().Name ?? "null",
                            patchOrdinal));

                        // If PatchResource returns the boolean true, it means skip
                        if (patchedData is bool b && b)
                        {
                            log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.SkippingFilePatchResource, patch.SourceFile));
                            continue;
                        }

                        if (skipDialogTlkWrite)
                        {
                            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                "Install: applied TLK patch for memory only; skipping write for {0}", saveAs));
                            continue;
                        }

                        if (patchedData is byte[] patchedDataBytes)
                        {
                            if (result.Capsule != null)
                            {
                                HandleOverrideType(patch);
                                HandleModRimShadow(patch);

                                (string resName, ResourceType resType) = ResourceIdentifier.FromPath(saveAs).Unpack();
                                result.Capsule.Add(resName, resType, patchedDataBytes);
                                result.Capsule.Save();
                                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                    "Install: wrote resource into capsule resName={0} resType={1} bytes={2} ordinal={3}",
                                    resName, resType, patchedDataBytes.Length, patchOrdinal));
                            }
                            else
                            {
                                // output_container_path.mkdir(exist_ok=True, parents=True)
                                Directory.CreateDirectory(outputContainerPath);
                                string destinationPath = Path.Combine(outputContainerPath, saveAs);
                                File.WriteAllBytes(destinationPath, patchedDataBytes);
                                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                    "Install: wrote file {0} bytes={1} ordinal={2}",
                                    destinationPath, patchedDataBytes.Length, patchOrdinal));
                            }

                            log.CompletePatch();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "Install: exception in patch ordinal={0} type={1} message={2}",
                            patchOrdinal, ex.GetType().FullName, ex.Message));
                        // exc_type, exc_msg = universal_simplify_exception(e)
                        string excType = ex.GetType().Name;
                        string excMsg = ex.Message;
                        string fmtExcStr = $"{excType}: {excMsg}";
                        string msg = $"An error occurred in patchlist {patch.GetType().Name}:{Environment.NewLine}{fmtExcStr}{Environment.NewLine}";
                        log.AddError(msg);
                        // RobustLogger().exception(msg) - log to file/console
                        System.Diagnostics.Debug.WriteLine($"Exception: {msg}{ex}");
                    }

                    progressCallback?.Invoke(patchesList.IndexOf(patch) + 1);
                }

                // if config.save_processed_scripts == 0 and temp_script_folder is not None and temp_script_folder.is_dir():
                if (cfg.SaveProcessedScripts == 0 && tempScriptFolder != null && Directory.Exists(tempScriptFolder))
                {
                    log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.CleaningTemporaryScriptFolder, tempScriptFolder));
                    try
                    {
                        Directory.Delete(tempScriptFolder, recursive: true);
                    }
                    catch
                    {
                        // Ignore errors when deleting temp folder
                    }
                }

                // num_patches_completed: int = config.patch_count()
                int numPatchesCompleted = cfg.PatchCount();
                log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.SuccessfullyCompletedFormat, numPatchesCompleted, numPatchesCompleted == 1 ? "patch" : "total patches"));
                installLog?.WriteInfo(PatcherResources.InstallationCompletedSuccessfully);
            }
            catch (Exception ex)
            {
                // Ensure errors are logged to install log even if installation fails
                installLog?.WriteError(string.Format(CultureInfo.CurrentCulture, PatcherResources.InstallationFailedFormat, ex.Message));
                throw;
            }
            finally
            {
                // Always dispose the install log to ensure it's flushed and closed
                installLog?.Dispose();
            }
        }

        /// <summary>
        /// Prepares NSS compilation by copying scripts to temp folder and preprocessing tokens.
        /// </summary>
        [CanBeNull]
        private string PrepareCompileList(PatcherConfig config, PatcherMemory memory)
        {
            // tslpatchdata should be read-only, this allows us to replace memory tokens while ensuring include scripts work correctly.
            if (config.PatchesNSS.Count == 0)
            {
                log.AddDiagnostic("PrepareCompileList: no NSS patches, skipping temp script folder");
                return null;
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "PrepareCompileList: NSS patch count={0}, modPath={1}", config.PatchesNSS.Count, modPath));

            // Move nwscript.nss to Override if there are any nss patches to do
            string nwscriptPath = Path.Combine(modPath, "nwscript.nss");
            if (File.Exists(nwscriptPath))
            {
                var fileInstall = new InstallFile("nwscript.nss", replaceExisting: true);
                if (!config.InstallList.Contains(fileInstall))
                {
                    config.InstallList.Add(fileInstall);
                }
            }

            // Copy all .nss files in the mod path, to a temp working directory
            string tempScriptFolder = Path.Combine(modPath, "temp_nss_working_dir");
            if (Directory.Exists(tempScriptFolder))
            {
                try
                {
                    Directory.Delete(tempScriptFolder, recursive: true);
                }
                catch
                {
                    // Ignore errors
                }
            }
            Directory.CreateDirectory(tempScriptFolder);

            // Copy .nss files
            foreach (string file in Directory.GetFiles(modPath))
            {
                if (Path.GetExtension(file).Equals(".nss", StringComparison.OrdinalIgnoreCase) && File.Exists(file))
                {
                    string destFile = Path.Combine(tempScriptFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
            }

            // Process the strref/2damemory in each script
            string[] scripts = Directory.GetFiles(tempScriptFolder, "*.nss", SearchOption.TopDirectoryOnly);
            log.AddVerbose($"Preprocessing #StrRef# and #2DAMEMORY# tokens for all {scripts.Length} scripts, before running [CompileList]");

            foreach (string script in scripts)
            {
                if (!File.Exists(script) || !Path.GetExtension(script).Equals(".nss", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                log.AddVerbose($"Parsing tokens in '{Path.GetFileName(script)}'...");
                byte[] scriptBytes = File.ReadAllBytes(script);
                string scriptText = Encoding.GetEncoding("windows-1252").GetString(scriptBytes);
                var mutableContent = new MutableString(scriptText);

                // Apply token replacement
                var nssMod = new ModificationsNSS(Path.GetFileName(script), false);
                nssMod.Apply(mutableContent, memory, log, Game.Value);

                // Write back with windows-1252 encoding
                File.WriteAllText(script, mutableContent.Value, Encoding.GetEncoding("windows-1252"));
            }

            // Store the location of the temp folder in each nss patch
            foreach (ModificationsNSS nssPatch in config.PatchesNSS)
            {
                nssPatch.TempScriptFolder = tempScriptFolder;
            }

            return tempScriptFolder;
        }

        private static string GetModuleRoot(string moduleFilePath)
        {
            string root = Path.GetFileNameWithoutExtension(moduleFilePath).ToLowerInvariant();
            root = root.EndsWith("_s") ? root.Substring(0, root.Length - 2) : root;
            root = root.EndsWith("_dlg") ? root.Substring(0, root.Length - 4) : root;
            return root;
        }

        /// <summary>
        /// Handle capsule file and create backup.
        /// </summary>
        private HandleCapsuleResult HandleCapsuleAndBackup(
            PatcherModifications patch,
            string outputContainerPath,
            string destination,
            string saveAs)
        {
            // Can be null if not found
            Capsule capsule = null;
            bool exists = false;

            if (IsCapsuleFile(destination))
            {
                // module_root: str = Installation.get_module_root(output_container_path)
                string moduleRoot = GetModuleRoot(outputContainerPath);
                string[] tslrcmOmittedRims = { "702KOR", "401DXN" };

                // if module_root.upper() not in tslrcm_omitted_rims and is_rim_file(output_container_path):
                if (!tslrcmOmittedRims.Contains(moduleRoot.ToUpperInvariant()) && IsRimFile(outputContainerPath))
                {
                    log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.PatchingRimFileWarning, Path.GetFileName(outputContainerPath)));
                }

                // if not output_container_path.is_file():
                if (!File.Exists(outputContainerPath))
                {
                    // if is_mod_file(output_container_path):
                    if (IsModFile(outputContainerPath))
                    {
                        string modulesPath = Path.Combine(gamePath, "Modules");
                        log.AddNote(
                            string.Format(CultureInfo.CurrentCulture, PatcherResources.ModuleDidNotExistBuilding, outputContainerPath) +
                            $"\n    Modules/{moduleRoot}.rim" +
                            $"\n    Modules/{moduleRoot}_s.rim" +
                            (Game != null && Game.Value == Common.Game.TSL ? $"\n    Modules/{moduleRoot}_dlg.erf" : "")
                        );
                        try
                        {
                            RimToMod(outputContainerPath, modulesPath, moduleRoot);
                        }
                        catch (Exception ex)
                        {
                            log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.FailedToBuildModule, Path.GetFileName(outputContainerPath), ex.Message));
                            throw;
                        }
                    }
                    else
                    {
                        // raise FileNotFoundError(errno.ENOENT, msg, str(output_container_path))
                        throw new FileNotFoundException(
                            string.Format(CultureInfo.CurrentCulture, PatcherResources.CapsuleNotFoundOrPermission, destination, patch.Action.ToLower().TrimEnd(), patch.SourceFile),
                            outputContainerPath);
                    }
                }

                capsule = new Capsule(outputContainerPath, createIfNotExist: false);
                (string backupPath, HashSet<string> processedFiles) = GetBackup();
                CreateBackupHelper(outputContainerPath, backupPath, processedFiles, Path.GetDirectoryName(destination) ?? "");

                // exists = capsule.contains(*ResourceIdentifier.from_path(patch.saveas).unpack())
                (string resName, ResourceType resType) = ResourceIdentifier.FromPath(saveAs).Unpack();
                exists = capsule.Contains(resName, resType);
            }
            else
            {
                // create_backup(self.log, output_container_path.joinpath(patch.saveas), *self.backup(), patch.destination)
                string fullPath = Path.Combine(outputContainerPath, saveAs);
                (string backupPath, HashSet<string> processedFiles) = GetBackup();
                CreateBackupHelper(fullPath, backupPath, processedFiles, destination);

                // exists = output_container_path.joinpath(patch.saveas).is_file()
                exists = File.Exists(fullPath);
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "HandleCapsuleAndBackup: destination={0} saveAs={1} exists={2} isCapsuleDestination={3}",
                destination,
                saveAs,
                exists,
                IsCapsuleFile(destination)));

            return new HandleCapsuleResult { Exists = exists, Capsule = capsule };
        }

        private class HandleCapsuleResult
        {
            public bool Exists { get; set; }
            [CanBeNull]
            public Capsule Capsule { get; set; }
        }

        /// <summary>
        /// Creates a backup of the provided file.
        /// </summary>
        private void CreateBackupHelper(string destinationFilePath, string backupFolderPath, HashSet<string> processedFiles, [CanBeNull] string subdirectoryPath = null)
        {
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "CreateBackupHelper: dest={0} backupRoot={1} subdir={2}",
                destinationFilePath, backupFolderPath, subdirectoryPath ?? ""));

            string destinationFileStr = destinationFilePath;
            string destinationFileStrLower = destinationFileStr.ToLowerInvariant();

            string backupFilepath;
            if (!string.IsNullOrEmpty(subdirectoryPath))
            {
                string subdirectoryBackupPath = Path.Combine(backupFolderPath, subdirectoryPath);
                backupFilepath = Path.Combine(subdirectoryBackupPath, Path.GetFileName(destinationFilePath));
                Directory.CreateDirectory(subdirectoryBackupPath);
            }
            else
            {
                backupFilepath = Path.Combine(backupFolderPath, Path.GetFileName(destinationFilePath));
            }

            // if destination_file_str_lower not in processed_files:
            if (!processedFiles.Contains(destinationFileStrLower))
            {
                // Write a list of files that should be removed in order to uninstall the mod
                string uninstallFolder = Path.Combine(Path.GetDirectoryName(backupFolderPath) ?? backupFolderPath, "..", "uninstall");
                uninstallFolder = Path.GetFullPath(uninstallFolder);
                string uninstallStrLower = uninstallFolder.ToLowerInvariant();

                if (!processedFiles.Contains(uninstallStrLower))
                {
                    Directory.CreateDirectory(uninstallFolder);

                    // game_folder: CaseAwarePath = destination_filepath.parents[len(subdir_temp.parts)] if subdir_temp else destination_filepath.parent
                    string gameFolder;
                    if (!string.IsNullOrEmpty(subdirectoryPath))
                    {
                        // Calculate game folder by going up the directory tree based on subdirectory depth
                        string[] subdirParts = subdirectoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                        string currentPath = destinationFilePath;
                        for (int i = 0; i < subdirParts.Length; i++)
                        {
                            currentPath = Path.GetDirectoryName(currentPath) ?? currentPath;
                        }
                        gameFolder = currentPath;
                    }
                    else
                    {
                        gameFolder = Path.GetDirectoryName(destinationFilePath) ?? gamePath;
                    }

                    CreateUninstallScripts(backupFolderPath, uninstallFolder, gameFolder, log);
                    processedFiles.Add(uninstallStrLower);
                }

                // if destination_filepath.is_file():
                if (File.Exists(destinationFilePath))
                {
                    // Check if the backup path exists and generate a new one if necessary
                    int i = 2;
                    string filestem = Path.GetFileNameWithoutExtension(backupFilepath);
                    string suffix = Path.GetExtension(backupFilepath);
                    string backupDir = Path.GetDirectoryName(backupFilepath) ?? backupFolderPath;

                    while (File.Exists(backupFilepath))
                    {
                        backupFilepath = Path.Combine(backupDir, $"{filestem} ({i}){suffix}");
                        i++;
                    }

                    log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.BackingUpFile, destinationFileStr));
                    try
                    {
                        File.Copy(destinationFilePath, backupFilepath, true);
                    }
                    catch (Exception ex)
                    {
                        log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.FailedToCreateBackup, destinationFileStr, ex.Message));
                    }
                }
                else
                {
                    // Write the file path to remove these files.txt in backup directory
                    string removalFilesTxt = Path.Combine(backupFolderPath, "remove these files.txt");
                    string line = (File.Exists(removalFilesTxt) ? "\n" : "") + destinationFileStr;
                    File.AppendAllText(removalFilesTxt, line);
                }

                // Add the lowercased path string to the processed_files set
                processedFiles.Add(destinationFileStrLower);
            }
        }

        /// <summary>
        /// Resolves a path under the mod package for install/NSS/etc. sources.
        /// When <see cref="TslPatchDataPath"/> is set and contains the file, that wins; otherwise <see cref="modPath"/>.
        /// </summary>
        private string ResolveModContentPath(string sourceFolder, string sourceFile)
        {
            string underModRoot = Path.Combine(modPath, sourceFolder, sourceFile);
            if (!string.IsNullOrWhiteSpace(TslPatchDataPath))
            {
                string underTsl = Path.Combine(TslPatchDataPath, sourceFolder, sourceFile);
                if (File.Exists(underTsl))
                {
                    return underTsl;
                }
            }

            return underModRoot;
        }

        /// <summary>
        /// Loads a resource file using BinaryReader.
        /// </summary>
        [NotNull]
        private static byte[] LoadResourceFile(string sourcePath)
        {
            // with BinaryReader.from_auto(source) as reader: return reader.read_all()
            using (var reader = RawBinaryReader.FromFile(sourcePath))
            {
                return reader.ReadAll();
            }
        }

        /// <summary>
        /// Looks up the file/resource that is expected to be patched.
        /// </summary>
        [CanBeNull]
        public byte[] LookupResource(
            PatcherModifications patch,
            string outputContainerPath,
            string saveAs,
            bool existsAtOutput,
            [CanBeNull] Capsule capsule)
        {
            try
            {
                string sourceFolder = patch.SourceFolder ?? "";
                string sourceFile = patch.SourceFile ?? "";
                // logic for loading the source file: if patch.replace_file or not exists_at_output_location:
                //   return self.load_resource_file(self.mod_path / patch.sourcefolder / patch.sourcefile)
                if (patch.ReplaceFile || !existsAtOutput)
                {
                    if (string.IsNullOrWhiteSpace(sourceFile))
                    {
                        log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotLoadSourceFileMissing, patch.Action.ToLower().Trim()));
                        return null;
                    }

                    log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "LookupResource: loading from mod (replaceOrMissingOutput) sourceFolder={0} sourceFile={1}",
                        sourceFolder, sourceFile));

                    // Path resolution: prefer tslpatchdata when TslPatchDataPath is set (assets live there),
                    // then mod root — matches INI comments and HoloPatcher-style layouts where modPath is the
                    // extracted mod folder and tslpatchdata is a subdirectory.
                    string sourcePath = ResolveModContentPath(sourceFolder, sourceFile);
                    return LoadResourceFile(sourcePath);
                }

                // if capsule is None:
                //   return self.load_resource_file(output_container_path / patch.saveas)
                if (capsule is null)
                {
                    if (string.IsNullOrWhiteSpace(saveAs))
                    {
                        log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotLoadFileSaveAsMissing, patch.Action.ToLower().Trim()));
                        return null;
                    }

                    string targetPath = Path.Combine(outputContainerPath, saveAs);
                    log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "LookupResource: loading existing override file {0}", targetPath));
                    return LoadResourceFile(targetPath);
                }

                if (string.IsNullOrWhiteSpace(saveAs))
                {
                    log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotLoadResourceFromCapsule, patch.Action.ToLower().Trim()));
                    return null;
                }

                // return capsule.resource(*ResourceIdentifier.from_path(patch.saveas).unpack())
                (string resName, ResourceType resType) = ResourceIdentifier.FromPath(saveAs).Unpack();
                log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "LookupResource: capsule get resName={0} resType={1} path={2}", resName, resType, capsule.Path.GetResolvedPath()));
                return capsule.GetResource(resName, resType);
            }
            catch (Exception ex)
            {
                // self.log.add_error(f"Could not load source file to {patch.action.lower().strip()}:{os.linesep}{universal_simplify_exception(e)}")
                log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotLoadSourceFileFormat, patch.Action.ToLower().Trim(), Environment.NewLine, ex.Message));
                return null;
            }
        }

        public bool ShouldPatch(
            PatcherModifications patch,
            bool exists,
            [CanBeNull] Capsule capsule = null)
        {
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ShouldPatch: exists={0} capsuleNull={1} replaceFile={2} skipIfNotReplace={3}",
                exists, capsule == null, patch.ReplaceFile, patch.SkipIfNotReplace));

            string destination = patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION;
            string localFolder = destination == "." ? new DirectoryInfo(gamePath).Name : destination;
            string containerType = capsule is null ? "folder" : "archive";

            // action[:-1] which removes last character, not just trailing whitespace
            string actionBase = patch.Action.Length > 0 ? patch.Action.Substring(0, patch.Action.Length - 1) : patch.Action;

            string saveAs = patch.SaveAs ?? patch.SourceFile ?? "";
            // should_patch() should not check for empty sourcefile/saveas; parity: skip this validation.
            if (patch.ReplaceFile && exists)
            {
                string saveAsStr = saveAs != patch.SourceFile ? $"'{saveAs}' in" : "in";
                log.AddNote($"{actionBase}ing '{patch.SourceFile}' and replacing existing file {saveAsStr} the '{localFolder}' {containerType}");
                log.AddDiagnostic("ShouldPatch: branch replaceFile && exists -> true");
                return true;
            }

            if (!patch.SkipIfNotReplace && !patch.ReplaceFile && exists)
            {
                log.AddNote($"{actionBase}ing existing file '{saveAs}' in the '{localFolder}' {containerType}");
                log.AddDiagnostic("ShouldPatch: branch patch existing without replace flag -> true");
                return true;
            }

            if (patch.SkipIfNotReplace && !patch.ReplaceFile && exists)
            {
                log.AddNote($"'{saveAs}' already exists in the '{localFolder}' {containerType}. Skipping file...");
                log.AddDiagnostic("ShouldPatch: branch skipIfNotReplace -> false");
                return false;
            }

            // If capsule doesn't exist on disk, return false
            if (capsule != null && !capsule.Path.IsFile())
            {
                log.AddDiagnostic("ShouldPatch: capsule path not a file -> false");
                log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CapsuleNotFoundWhenAttempting, destination, patch.Action.ToLower().TrimEnd(), patch.SourceFile));
                return false;
            }
            if (capsule != null && !capsule.ExistedOnDisk && !patch.ReplaceFile)
            {
                log.AddDiagnostic("ShouldPatch: capsule did not exist on disk and not replace -> false");
                log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CapsuleNotFoundWhenAttempting, destination, patch.Action.ToLower().TrimEnd(), patch.SourceFile));
                return false;
            }

            string saveType = (capsule != null && saveAs == patch.SourceFile) ? "adding" : "saving";
            string savingAsStr = saveAs != patch.SourceFile ? $"as '{saveAs}' in" : "to";
            log.AddNote($"{actionBase}ing '{patch.SourceFile}' and {saveType} {savingAsStr} the '{localFolder}' {containerType}");
            log.AddDiagnostic("ShouldPatch: default new/copy branch -> true");
            return true;
        }

        /// <summary>
        /// Handles the desired behavior set by the !OverrideType kpatcher var for the specified patch.
        /// </summary>
        private void HandleOverrideType(PatcherModifications patch)
        {
            // override_type: str = patch.override_type.lower().strip()
            string overrideType = patch.OverrideTypeValue.ToLowerInvariant().Trim();
            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "HandleOverrideType: saveAs={0} overrideType={1}",
                patch.SaveAs ?? patch.SourceFile ?? "", overrideType));
            if (string.IsNullOrEmpty(overrideType) || overrideType == OverrideType.IGNORE)
            {
                return;
            }

            string saveAs = patch.SaveAs ?? patch.SourceFile ?? "";
            string overrideDir = Path.Combine(gamePath, "Override");
            string overrideResourcePath = Path.Combine(overrideDir, saveAs);

            // if override_resource_path.is_file():
            if (!File.Exists(overrideResourcePath))
            {
                return;
            }

            if (overrideType == OverrideType.RENAME)
            {
                // renamed_file_path: CaseAwarePath = override_dir / f"old_{patch.saveas}"
                string renamedFilePath = Path.Combine(overrideDir, $"old_{saveAs}");
                int i = 2;
                string filestem = Path.GetFileNameWithoutExtension(renamedFilePath);

                // while renamed_file_path.is_file():
                while (File.Exists(renamedFilePath))
                {
                    // renamed_file_path = renamed_file_path.parent / f"{filestem} ({i}){renamed_file_path.suffix}"
                    string suffix = Path.GetExtension(renamedFilePath);
                    renamedFilePath = Path.Combine(overrideDir, $"{filestem} ({i}){suffix}");
                    i++;
                }

                try
                {
                    // shutil.move(str(override_resource_path), str(renamed_file_path))
                    File.Move(overrideResourcePath, renamedFilePath);
                    log.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.RenamedExistingOverrideFile, saveAs, Path.GetFileName(renamedFilePath)));
                }
                catch (Exception ex)
                {
                    // self.log.add_error(f"Could not rename '{patch.saveas}' to '{renamed_file_path.name}' in the Override folder: {universal_simplify_exception(e)}")
                    log.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotRenameInOverrideFolder, saveAs, Path.GetFileName(renamedFilePath), ex.Message));
                }
            }
            else if (overrideType == OverrideType.WARN)
            {
                // self.log.add_warning(f"A resource located at '{override_resource_path}' is shadowing this mod's changes in {patch.destination}!")
                string destination = patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION;
                log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.ResourceShadowingModChanges, overrideResourcePath, destination));
            }
        }

        /// <summary>
        /// Check if a patch is being installed into a rim and overshadowed by a .mod.
        /// </summary>
        private void HandleModRimShadow(PatcherModifications patch)
        {
            log.AddDiagnostic("HandleModRimShadow: checking .mod shadow for RIM/ERF write");
            // erfrim_path: CaseAwarePath = self.game_path / patch.destination / patch.saveas
            string destination = patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION;
            string saveAs = patch.SaveAs ?? patch.SourceFile ?? "";
            string erfrimPath = Path.Combine(gamePath, destination, saveAs);

            // mod_path: CaseAwarePath = erfrim_path.with_name(f"{Installation.get_module_root(erfrim_path.name)}.mod")
            string moduleRoot = GetModuleRoot(Path.GetFileName(erfrimPath));
            string modFilePath = Path.Combine(Path.GetDirectoryName(erfrimPath) ?? gamePath, $"{moduleRoot}.mod");

            // if erfrim_path != mod_path and mod_path.is_file():
            if (!erfrimPath.Equals(modFilePath, StringComparison.OrdinalIgnoreCase) && File.Exists(modFilePath))
            {
                log.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.ModOvershadowedByExisting, saveAs, destination, Path.GetFileName(modFilePath)));
            }
        }

        private static bool IsCapsuleFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mod" || ext == ".rim" || ext == ".erf" || ext == ".sav";
        }

        private static bool IsModFile(string path)
        {
            return Path.GetExtension(path).Equals(".mod", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRimFile(string path)
        {
            return Path.GetExtension(path).Equals(".rim", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a MOD file at the given filepath and copies the resources from the corresponding RIM files.
        /// </summary>
        private void RimToMod(string modFilePath, string rimFolderPath, string moduleRoot)
        {
            if (!IsModFile(modFilePath))
            {
                throw new ArgumentException("Specified file must end with the .mod extension", nameof(modFilePath));
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "RimToMod: modFilePath={0} rimFolderPath={1} moduleRoot={2}", modFilePath, rimFolderPath, moduleRoot));

            string filepathRim = Path.Combine(rimFolderPath, $"{moduleRoot}.rim");
            string filepathRimS = Path.Combine(rimFolderPath, $"{moduleRoot}_s.rim");
            string filepathDlgErf = Path.Combine(rimFolderPath, $"{moduleRoot}_dlg.erf");

            var mod = new ERF(ERFType.MOD, true);

            // Load main RIM using Capsule
            if (File.Exists(filepathRim))
            {
                var rimCapsule = new Capsule(filepathRim, createIfNotExist: false);
                foreach (CapsuleResource res in rimCapsule)
                {
                    mod.SetData(res.ResName, res.ResType, res.Data);
                }
            }

            // Load _s RIM if exists
            if (File.Exists(filepathRimS))
            {
                var rimSCapsule = new Capsule(filepathRimS, createIfNotExist: false);
                foreach (CapsuleResource res in rimSCapsule)
                {
                    mod.SetData(res.ResName, res.ResType, res.Data);
                }
            }

            // Load _dlg.erf if exists (TSL only)
            if ((Game is null || Game.Value == Common.Game.TSL) && File.Exists(filepathDlgErf))
            {
                var erfCapsule = new Capsule(filepathDlgErf, createIfNotExist: false);
                foreach (CapsuleResource res in erfCapsule)
                {
                    mod.SetData(res.ResName, res.ResType, res.Data);
                }
            }

            // Write MOD file
            var writer = new ERFBinaryWriter(mod);
            using (FileStream fs = File.Create(modFilePath))
            {
                writer.Write(fs);
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "RimToMod: completed write modFilePath={0} resourceCount={1}", modFilePath, mod.Count));
        }

        /// <summary>
        /// Gets TLK patches from the configuration.
        /// Returns main TLK patches and female dialog patches if applicable.
        /// </summary>
        [NotNull]
        private List<PatcherModifications> GetTlkPatches(PatcherConfig config)
        {
            var tlkPatches = new List<PatcherModifications>();

            if (config.PatchesTLK.Modifiers.Count == 0)
            {
                log.AddDiagnostic("GetTlkPatches: no TLK modifiers in config");
                return tlkPatches;
            }

            // Add main TLK patches
            tlkPatches.Add(config.PatchesTLK);

            // Check if female dialog file exists
            string femaleDialogFilename = "dialogf.tlk";
            string femaleDialogFilePath = Path.Combine(gamePath, femaleDialogFilename);

            if (File.Exists(femaleDialogFilePath))
            {
                // Create a deep copy of the TLK patches for female dialog
                var femaleTlkPatches = new Mods.TLK.ModificationsTLK(
                    config.PatchesTLK.SourceFile,
                    config.PatchesTLK.ReplaceFile);

                // Copy all modifiers
                foreach (Mods.TLK.ModifyTLK modifier in config.PatchesTLK.Modifiers)
                {
                    femaleTlkPatches.Modifiers.Add(modifier);
                }

                // Copy other properties
                femaleTlkPatches.SourceFolder = config.PatchesTLK.SourceFolder;
                femaleTlkPatches.Destination = config.PatchesTLK.Destination;
                femaleTlkPatches.OverrideTypeValue = config.PatchesTLK.OverrideTypeValue;
                femaleTlkPatches.SkipIfNotReplace = config.PatchesTLK.SkipIfNotReplace;

                // Use female source file if it exists, otherwise use main source file
                string femaleSourceFile = config.PatchesTLK.SourcefileF;
                if (!string.IsNullOrEmpty(femaleSourceFile))
                {
                    string femaleSourcePath = Path.Combine(modPath, config.PatchesTLK.SourceFolder, femaleSourceFile);
                    if (File.Exists(femaleSourcePath))
                    {
                        femaleTlkPatches.SourceFile = femaleSourceFile;
                    }
                }

                // Set save as to female dialog filename
                femaleTlkPatches.SaveAs = femaleDialogFilename;

                tlkPatches.Add(femaleTlkPatches);
                log.AddDiagnostic("GetTlkPatches: appended female dialogf.tlk patch clone");
            }

            log.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetTlkPatches: returning {0} TLK patch object(s)", tlkPatches.Count));
            return tlkPatches;
        }

        /// <summary>
        /// Creates uninstall scripts (PowerShell and Bash) in the uninstall folder.
        /// </summary>
        private static void CreateUninstallScripts([NotNull] string backupDir, [NotNull] string uninstallFolder, [NotNull] string mainFolder, PatchLogger patchLog)
        {
            patchLog.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "CreateUninstallScripts: backupDir={0} uninstallFolder={1} mainFolder={2}",
                backupDir, uninstallFolder, mainFolder));
            // PowerShell script - using StringBuilder to avoid verbatim string parsing issues
            string ps1Path = Path.Combine(uninstallFolder, "uninstall.ps1");
            var ps1Script = new StringBuilder();
            ps1Script.AppendLine("#!/usr/bin/env pwsh");
            ps1Script.AppendLine("$backupParentFolder = Get-Item -Path \"..$([System.IO.Path]::DirectorySeparatorChar)backup\"");
            ps1Script.AppendLine("$mostRecentBackupFolder = Get-ChildItem -LiteralPath $backupParentFolder.FullName -Directory | ForEach-Object {");
            ps1Script.AppendLine("    $dirName = $_.Name");
            ps1Script.AppendLine("    try {");
            ps1Script.AppendLine("        [datetime]$dt = [datetime]::ParseExact($dirName, \"yyyy-MM-dd_HH.mm.ss\", $null)");
            ps1Script.AppendLine("        Write-Host \"Found backup '$dirName'\"");
            ps1Script.AppendLine("        return [PSCustomObject]@{");
            ps1Script.AppendLine("            Directory = $_.FullName");
            ps1Script.AppendLine("            DateTime = $dt");
            ps1Script.AppendLine("        }");
            ps1Script.AppendLine("    } catch {");
            ps1Script.AppendLine("        if ($dirName -and $dirName -ne '' -and -not ($dirName -match \"^\\s*$\")) {");
            ps1Script.AppendLine("            Write-Host \"Ignoring directory '$dirName'. $($_.Exception.Message)\"");
            ps1Script.AppendLine("        }");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("} | Sort-Object DateTime -Descending | Select-Object -ExpandProperty Directory -First 1");
            ps1Script.AppendLine("if ($null -eq $mostRecentBackupFolder -or -not $mostRecentBackupFolder -or -not (Test-Path -LiteralPath $mostRecentBackupFolder -ErrorAction SilentlyContinue)) {");
            ps1Script.AppendLine("    $mostRecentBackupFolder = \"" + backupDir.Replace("\\", "\\\\") + "\"");
            ps1Script.AppendLine("    if (-not (Test-Path -LiteralPath $mostRecentBackupFolder -ErrorAction SilentlyContinue)) {");
            ps1Script.AppendLine("        Write-Host \"No backups found in '$($backupParentFolder.FullName)'\"");
            ps1Script.AppendLine("        Pause");
            ps1Script.AppendLine("        exit");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("    Write-Host \"Using hardcoded backup dir: '$mostRecentBackupFolder'\"");
            ps1Script.AppendLine("} else {");
            ps1Script.AppendLine("    Write-Host \"Selected backup folder '$mostRecentBackupFolder'\"");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine("$deleteListFile = $mostRecentBackupFolder + \"$([System.IO.Path]::DirectorySeparatorChar)remove these files.txt\"");
            ps1Script.AppendLine("$existingFiles = New-Object System.Collections.Generic.HashSet[string]");
            ps1Script.AppendLine("if (-not (Test-Path -LiteralPath $deleteListFile -ErrorAction SilentlyContinue)) {");
            ps1Script.AppendLine("    Write-Host \"Delete file list not found.\"");
            ps1Script.AppendLine("    #exit");
            ps1Script.AppendLine("} else {");
            ps1Script.AppendLine("    $filesToDelete = Get-Content -LiteralPath $deleteListFile");
            ps1Script.AppendLine("    foreach ($file in $filesToDelete) {");
            ps1Script.AppendLine("        if ($file) { # Check if $file is non-null and non-empty");
            ps1Script.AppendLine("            if (Test-Path -LiteralPath $file -ErrorAction SilentlyContinue) {");
            ps1Script.AppendLine("                # Check if the path is not a directory");
            ps1Script.AppendLine("                if (-not (Get-Item -LiteralPath $file).PSIsContainer) {");
            ps1Script.AppendLine("                    $existingFiles.Add($file) | Out-Null");
            ps1Script.AppendLine("                }");
            ps1Script.AppendLine("            } else {");
            ps1Script.AppendLine("                Write-Host \"WARNING! $file no longer exists! Running this script is no longer recommended!\"");
            ps1Script.AppendLine("            }");
            ps1Script.AppendLine("        }");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine();
            ps1Script.AppendLine("$numberOfExistingFiles = $existingFiles.Count");
            ps1Script.AppendLine();
            ps1Script.AppendLine("$allItemsInBackup = Get-ChildItem -LiteralPath $mostRecentBackupFolder -Recurse | Where-Object { $_.Name -ne 'remove these files.txt' }");
            ps1Script.AppendLine("$filesInBackup = ($allItemsInBackup | Where-Object { -not $_.PSIsContainer })");
            ps1Script.AppendLine("$folderCount = ($allItemsInBackup | Where-Object { $_.PSIsContainer }).Count");
            ps1Script.AppendLine();
            ps1Script.AppendLine("# Display relative file paths if file count is less than 6");
            ps1Script.AppendLine("if ($filesInBackup.Count -lt 6) {");
            ps1Script.AppendLine("    $allItemsInBackup |");
            ps1Script.AppendLine("    Where-Object { -not $_.PSIsContainer } |");
            ps1Script.AppendLine("    ForEach-Object {");
            ps1Script.AppendLine("        $relativePath = $_.FullName -replace [regex]::Escape($mostRecentBackupFolder), \"\"");
            ps1Script.AppendLine("        Write-Host $relativePath.TrimStart(\"\\\")");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine("$validConfirmations = @(\"y\", \"yes\")");
            ps1Script.AppendLine("$confirmation = Read-Host \"Really uninstall $numberOfExistingFiles files and restore the most recent backup (containing $($filesInBackup.Count) files and $folderCount folders)? (y/N)\"");
            ps1Script.AppendLine("if ($confirmation.Trim().ToLower() -notin $validConfirmations) {");
            ps1Script.AppendLine("    Write-Host \"Operation cancelled.\"");
            ps1Script.AppendLine("    exit");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine("$deletedCount = 0");
            ps1Script.AppendLine("foreach ($file in $existingFiles) {");
            ps1Script.AppendLine("    if ($file -and (Test-Path -LiteralPath $file -ErrorAction SilentlyContinue)) {");
            ps1Script.AppendLine("        Remove-Item $file -Force");
            ps1Script.AppendLine("        Write-Host \"Removed $file...\"");
            ps1Script.AppendLine("        $deletedCount++");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine("if ($deletedCount -ne 0) {");
            ps1Script.AppendLine("    Write-Host \"Deleted $deletedCount files.\"");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine();
            ps1Script.AppendLine("foreach ($file in $filesInBackup) {");
            ps1Script.AppendLine("    try {");
            ps1Script.AppendLine("        $relativePath = $file.FullName.Substring($mostRecentBackupFolder.Length)");
            ps1Script.AppendLine("        $destinationPath = Join-Path \"" + mainFolder.Replace("\\", "\\\\") + "\" -ChildPath $relativePath");
            ps1Script.AppendLine();
            ps1Script.AppendLine("        # Create the directory structure if it doesn't exist");
            ps1Script.AppendLine("        $destinationDir = [System.IO.Path]::GetDirectoryName($destinationPath)");
            ps1Script.AppendLine("        if (-not (Test-Path -LiteralPath $destinationDir)) {");
            ps1Script.AppendLine("            New-Item -LiteralPath $destinationDir -ItemType Directory -Force");
            ps1Script.AppendLine("        }");
            ps1Script.AppendLine();
            ps1Script.AppendLine("        # Copy the file to the destination");
            ps1Script.AppendLine("        Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force");
            ps1Script.AppendLine("        Write-Host \"Restoring backup of '$($file.Name)' to '$destinationDir'...\"");
            ps1Script.AppendLine("    } catch {");
            ps1Script.AppendLine("        Write-Host \"Failed to restore backup of $($file.Name) because of: $($_.Exception.Message)\"");
            ps1Script.AppendLine("    }");
            ps1Script.AppendLine("}");
            ps1Script.AppendLine("Pause");

            File.WriteAllText(ps1Path, ps1Script.ToString(), Encoding.UTF8);

            // Bash script
            string shPath = Path.Combine(uninstallFolder, "uninstall.sh");
            var shScript = new StringBuilder();
            shScript.AppendLine("#!/bin/bash");
            shScript.AppendLine();
            shScript.AppendLine("backupParentFolder=\"../backup\"");
            shScript.AppendLine("mostRecentBackupFolder=$(ls -d \"$backupParentFolder\"/* | while read -r dir; do");
            shScript.AppendLine("    dirName=$(basename \"$dir\")");
            shScript.AppendLine("    if [[ \"$dirName\" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}\\.[0-9]{2}\\.[0-9]{2}$ ]]; then");
            shScript.AppendLine("        # Convert the directory name to a sortable format YYYYMMDDHHMMSS and echo both the sortable format and the original directory");
            shScript.AppendLine("        echo \"${dirName:0:4}${dirName:5:2}${dirName:8:2}${dirName:11:2}${dirName:14:2}${dirName:17:2} $dir\"");
            shScript.AppendLine("    else");
            shScript.AppendLine("        if [[ -n \"$dirName\" && ! \"$dirName\" =~ ^[[:space:]]*$ ]]; then");
            shScript.AppendLine("            echo \"Ignoring directory '$dirName'\" >&2");
            shScript.AppendLine("        fi");
            shScript.AppendLine("    fi");
            shScript.AppendLine("done | sort -r | awk 'NR==1 {print $2}')");
            shScript.AppendLine();
            shScript.AppendLine();
            shScript.AppendLine();
            shScript.AppendLine("if [[ ! -d \"$mostRecentBackupFolder\" ]]; then");
            shScript.AppendLine("    mostRecentBackupFolder=\"" + backupDir.Replace("\\", "/") + "\"");
            shScript.AppendLine("    if [[ ! -d \"$mostRecentBackupFolder\" ]]; then");
            shScript.AppendLine("        echo \"No backups found in '$backupParentFolder'\"");
            shScript.AppendLine("        read -rp \"Press enter to continue...\"");
            shScript.AppendLine("        exit 1");
            shScript.AppendLine("    fi");
            shScript.AppendLine("    echo \"Using hardcoded backup dir: '$mostRecentBackupFolder'\"");
            shScript.AppendLine("else");
            shScript.AppendLine("    echo \"Selected backup folder '$mostRecentBackupFolder'\"");
            shScript.AppendLine("fi");
            shScript.AppendLine();
            shScript.AppendLine("existingFiles=()");
            shScript.AppendLine("deleteListFile=\"$mostRecentBackupFolder/remove these files.txt\"");
            shScript.AppendLine("if [[ ! -f \"$deleteListFile\" ]]; then");
            shScript.AppendLine("    echo \"File list not found.\"");
            shScript.AppendLine("    #exit 1");
            shScript.AppendLine("else");
            shScript.AppendLine("    declare -a filesToDelete");
            shScript.AppendLine("    mapfile -t filesToDelete < \"$deleteListFile\"");
            shScript.AppendLine("    echo \"Building file lists...\"");
            shScript.AppendLine("    for file in \"${filesToDelete[@]}\"; do");
            shScript.AppendLine("        normalizedFile=$(echo \"$file\" | tr '\\\\' '/')");
            shScript.AppendLine("        if [[ -n \"$file\" && -f \"$file\" ]]; then");
            shScript.AppendLine("            existingFiles+=(\"$file\")");
            shScript.AppendLine("        else");
            shScript.AppendLine("            echo \"WARNING! $file no longer exists! Running this script is no longer recommended!\"");
            shScript.AppendLine("        fi");
            shScript.AppendLine("    done");
            shScript.AppendLine("fi");
            shScript.AppendLine();
            shScript.AppendLine();
            shScript.AppendLine("fileCount=$(find \"$mostRecentBackupFolder\" -type f ! -name 'remove these files.txt' | wc -l)");
            shScript.AppendLine("folderCount=$(find \"$mostRecentBackupFolder\" -type d | wc -l)");
            shScript.AppendLine();
            shScript.AppendLine("# Display relative file paths if file count is less than 6");
            shScript.AppendLine("if [[ $fileCount -lt 6 ]]; then");
            shScript.AppendLine("    find \"$mostRecentBackupFolder\" -type f ! -name 'remove these files.txt' | sed \"s|^$mostRecentBackupFolder/||\"");
            shScript.AppendLine("fi");
            shScript.AppendLine();
            shScript.AppendLine("read -rp \"Really uninstall ${#existingFiles[@]} files and restore the most recent backup (containing $fileCount files and $folderCount folders)? \" confirmation");
            shScript.AppendLine("if [[ \"$confirmation\" != \"y\" && \"$confirmation\" != \"yes\" ]]; then");
            shScript.AppendLine("    echo \"Operation cancelled.\"");
            shScript.AppendLine("    exit 1");
            shScript.AppendLine("fi");
            shScript.AppendLine();
            shScript.AppendLine("deletedCount=0");
            shScript.AppendLine("for file in \"${existingFiles[@]}\"; do");
            shScript.AppendLine("    if [[ -f \"$file\" ]]; then");
            shScript.AppendLine("        rm -f \"$file\"");
            shScript.AppendLine("        echo \"Removed $file...\"");
            shScript.AppendLine("        ((deletedCount++))");
            shScript.AppendLine("    fi");
            shScript.AppendLine("done");
            shScript.AppendLine();
            shScript.AppendLine("if [[ $deletedCount -ne 0 ]]; then");
            shScript.AppendLine("    echo \"Deleted $deletedCount files.\"");
            shScript.AppendLine("fi");
            shScript.AppendLine();
            shScript.AppendLine("while IFS= read -r -d $'\\0' file; do");
            shScript.AppendLine("    relativePath=${file#$mostRecentBackupFolder}");
            shScript.AppendLine("    destinationPath=\"" + mainFolder.Replace("\\", "/") + "/$relativePath\"");
            shScript.AppendLine("    destinationDir=$(dirname \"$destinationPath\")");
            shScript.AppendLine("    if [[ ! -d \"$destinationDir\" ]]; then");
            shScript.AppendLine("        mkdir -p \"$destinationDir\"");
            shScript.AppendLine("    fi");
            shScript.AppendLine("    cp \"$file\" \"$destinationPath\" && echo \"Restoring backup of '$(basename $file)' to '$destinationDir'...\"");
            shScript.AppendLine("done < <(find \"$mostRecentBackupFolder\" -type f ! -name 'remove these files.txt' -print0)");
            shScript.AppendLine();
            shScript.AppendLine("read -rp \"Press enter to continue...\"");

            File.WriteAllText(shPath, shScript.ToString(), Encoding.UTF8);
        }
    }
}
