using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Logger;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Uninstall
{

    /// <summary>
    /// A class that provides functionality to uninstall a selected mod using the most recent backup folder created during the last install.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the ModUninstaller class.
    /// </remarks>
    /// <param name="backupsLocationPath">The path to the location of the backup folders</param>
    /// <param name="gamePath">The path to the game folder</param>
    /// <param name="logger">An optional logger object. Defaults to a new PatchLogger if null</param>
    public class ModUninstaller
    {
        private readonly CaseAwarePath _backupsLocationPath;
        private readonly CaseAwarePath _gamePath;
        private readonly PatchLogger _logger;

        public ModUninstaller(CaseAwarePath backupsLocationPath, [CanBeNull] CaseAwarePath gamePath, PatchLogger logger = null)
        {
            _backupsLocationPath = backupsLocationPath;
            _gamePath = gamePath;
            _logger = logger ?? new PatchLogger();
            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModUninstaller ctor: backups={0}, gamePath={1}",
                backupsLocationPath,
                gamePath?.ToString() ?? "null"));
        }

        /// <summary>
        /// Check if a folder name is a valid backup folder name based on a datetime pattern.
        /// Same behavior as HoloPatcher for validating backup folders.
        /// </summary>
        /// <param name="folder">Path object of the folder to validate</param>
        /// <param name="datetimePattern">String pattern to match folder name against (default: "yyyy-MM-dd_HH.mm.ss")</param>
        /// <returns>True if folder name matches datetime pattern, False otherwise</returns>
        public static bool IsValidBackupFolder(CaseAwarePath folder, string datetimePattern = "yyyy-MM-dd_HH.mm.ss")
        {
            try
            {
                DateTime.ParseExact(folder.Name, datetimePattern, CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the most recent valid backup folder.
        /// Same behavior as HoloPatcher for selecting the most recent backup.
        /// </summary>
        /// <param name="backupFolder">Path to the backup folder</param>
        /// <param name="showErrorDialog">Function to show error dialog (optional)</param>
        /// <returns>Path to the most recent valid backup folder or null</returns>
        public static CaseAwarePath GetMostRecentBackup(
            [CanBeNull] CaseAwarePath backupFolder,
            Action<string, string> showErrorDialog = null,
            [CanBeNull] ModUninstallerUiStrings ui = null,
            [CanBeNull] PatchLogger logger = null)
        {
            ModUninstallerUiStrings text = ui ?? ModUninstallerUiStrings.EnglishDefaults;
            string backupPathStr = backupFolder?.ToString() ?? "";
            logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetMostRecentBackup: backupRoot={0}", backupPathStr));

            if (!Directory.Exists(backupFolder))
            {
                logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "GetMostRecentBackup: directory does not exist path={0}", backupPathStr));
                showErrorDialog?.Invoke(text.NoBackupsTitle, text.GetNoBackupsMessage(backupPathStr));
                return null;
            }

            var validBackups = new List<CaseAwarePath>();
            string[] subdirs = Directory.GetDirectories(backupFolder);
            logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetMostRecentBackup: scanning subdirCount={0}", subdirs.Length));

            foreach (string subfolder in subdirs)
            {
                var subfolderPath = new CaseAwarePath(subfolder);
                bool hasEntries = Directory.EnumerateFileSystemEntries(subfolder).Any();
                bool nameOk = IsValidBackupFolder(subfolderPath);
                if (hasEntries && nameOk)
                {
                    validBackups.Add(subfolderPath);
                    logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "GetMostRecentBackup: valid backup folder name={0}", subfolderPath.Name));
                }
                else
                {
                    logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "GetMostRecentBackup: skip subdir={0} hasEntries={1} validName={2}",
                        subfolderPath.Name, hasEntries, nameOk));
                }
            }

            if (validBackups.Count == 0)
            {
                logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "GetMostRecentBackup: zero valid backups under {0}", backupPathStr));
                showErrorDialog?.Invoke(text.NoBackupsTitle, text.GetNoBackupsMessage(backupPathStr));
                return null;
            }

            // Return the folder with the maximum datetime parsed from folder name
            CaseAwarePath chosen = validBackups.MaxBy(x => DateTime.ParseExact(x.Name, "yyyy-MM-dd_HH.mm.ss", CultureInfo.InvariantCulture));
            logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetMostRecentBackup: chosen={0} (from {1} valid)", chosen, validBackups.Count));
            return chosen;
        }

        /// <summary>
        /// Restores a game backup folder to the existing game files.
        /// Same behavior as HoloPatcher for restoring a backup tree.
        /// </summary>
        /// <param name="backupFolder">Path to the backup folder</param>
        /// <param name="existingFiles">Set of existing file paths</param>
        /// <param name="filesInBackup">List of file paths in the backup</param>
        public void RestoreBackup(
            CaseAwarePath backupFolder,
            HashSet<string> existingFiles,
            List<CaseAwarePath> filesInBackup)
        {
            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "RestoreBackup: deleteFirstCount={0}, restoreFileCount={1}",
                existingFiles.Count,
                filesInBackup.Count));

            // Remove any existing files not in the backup
            foreach (string fileStr in existingFiles)
            {
                var filePath = new CaseAwarePath(fileStr);
                string relFilePath = Path.GetRelativePath(_gamePath, filePath);

                if (File.Exists(filePath))
                {
                    _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "RestoreBackup: deleting existing file rel={0} full={1}", relFilePath, filePath));
                    File.Delete(filePath);
                }
                else
                {
                    _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "RestoreBackup: delete list entry not on disk rel={0} full={1}", relFilePath, filePath));
                }

                _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.RemovedFormat, relFilePath));
            }

            // Copy each file from the backup folder to the destination restoring the file structure
            foreach (CaseAwarePath file in filesInBackup)
            {
                if (file.Name == "remove these files.txt")
                {
                    _logger.AddDiagnostic("RestoreBackup: skip marker file 'remove these files.txt'");
                    continue;
                }

                string relativePathFromBackup = Path.GetRelativePath(backupFolder, file);
                string destinationPath = Path.Combine(_gamePath, relativePathFromBackup);

                // [CanBeNull] Ensure parent directory exists
                string parentDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "RestoreBackup: copy src={0} dst={1} bytes={2}",
                    file.GetResolvedPath(),
                    destinationPath,
                    File.Exists(file) ? new FileInfo(file).Length : 0L));

                File.Copy(file, destinationPath, overwrite: true);

                string relativeToGameParent = Path.GetRelativePath(Path.GetDirectoryName(_gamePath) ?? "", destinationPath);
                _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.RestoringBackupOf, file.Name, relativeToGameParent));
            }

            _logger.AddDiagnostic("RestoreBackup: completed delete pass and restore copies");
        }

        /// <summary>
        /// Get information about the most recent valid backup.
        /// Same behavior as HoloPatcher for reading backup metadata.
        /// </summary>
        /// <param name="showErrorDialog">Function to show error dialog (optional)</param>
        /// <param name="showYesNoDialog">Function to show yes/no dialog (optional)</param>
        /// <returns>Tuple containing: most recent backup folder path, existing files set, files in backup list, folder count</returns>
        public (CaseAwarePath BackupFolder, HashSet<string> ExistingFiles, List<CaseAwarePath> FilesInBackup, int FolderCount) GetBackupInfo(
            [CanBeNull] Action<string, string> showErrorDialog = null,
            [CanBeNull] Func<string, string, bool> showYesNoDialog = null,
            [CanBeNull] ModUninstallerUiStrings ui = null)
        {
            ModUninstallerUiStrings text = ui ?? ModUninstallerUiStrings.EnglishDefaults;
            CaseAwarePath mostRecentBackupFolder = GetMostRecentBackup(_backupsLocationPath, showErrorDialog, ui, _logger);
            if (mostRecentBackupFolder is null)
            {
                _logger.AddDiagnostic("GetBackupInfo: no valid backup folder from GetMostRecentBackup");
                return (null, new HashSet<string>(), new List<CaseAwarePath>(), 0);
            }

            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetBackupInfo: selected backup={0}", mostRecentBackupFolder));

            string deleteListFile = Path.Combine(mostRecentBackupFolder, "remove these files.txt");
            var filesToDelete = new HashSet<string>();
            var existingFiles = new HashSet<string>();

            if (File.Exists(deleteListFile))
            {
                string[] lines = File.ReadAllLines(deleteListFile);
                _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "GetBackupInfo: delete list file lines={0} path={1}", lines.Length, deleteListFile));
                filesToDelete = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                                     .Select(line => line.Trim())
                                     .ToHashSet();
                existingFiles = filesToDelete.Where(line => !string.IsNullOrWhiteSpace(line) && File.Exists(line.Trim()))
                                            .ToHashSet();

                if (existingFiles.Count < filesToDelete.Count)
                {
                    _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "GetBackupInfo: delete list mismatch entries={0} existingOnDisk={1}",
                        filesToDelete.Count, existingFiles.Count));
                    bool continueAnyway = showYesNoDialog?.Invoke(
                        text.BackupMismatchTitle,
                        text.GetBackupMismatchMessage()
                    ) ?? false;

                    if (!continueAnyway)
                    {
                        _logger.AddDiagnostic("GetBackupInfo: user declined backup mismatch continue");
                        return (null, new HashSet<string>(), new List<CaseAwarePath>(), 0);
                    }
                }
            }
            else
            {
                _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "GetBackupInfo: no delete list file at {0}", deleteListFile));
            }

            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetBackupInfo: deleteListEntries={0} existingTargetFiles={1}",
                filesToDelete.Count,
                existingFiles.Count));

            var filesInBackup = Directory.EnumerateFiles(mostRecentBackupFolder, "*", SearchOption.AllDirectories)
                                        .Select(f => new CaseAwarePath(f))
                                        .ToList();

            int allEntries = Directory.EnumerateFileSystemEntries(mostRecentBackupFolder, "*", SearchOption.AllDirectories).Count();
            int folderCount = allEntries - filesInBackup.Count;

            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "GetBackupInfo: backupFileCount={0} allEntries={1} folderCount={2}",
                filesInBackup.Count, allEntries, folderCount));

            return (mostRecentBackupFolder, existingFiles, filesInBackup, folderCount);
        }

        /// <summary>
        /// Uninstalls the selected mod using the most recent backup folder created during the last install.
        /// Same behavior as HoloPatcher for uninstalling the selected mod.
        /// </summary>
        /// <param name="showErrorDialog">Function to show error dialog</param>
        /// <param name="showYesNoDialog">Function to show yes/no dialog</param>
        /// <param name="showYesNoCancelDialog">Function to show yes/no/cancel dialog (returns true for yes, false for no, null for cancel)</param>
        /// <returns>True if uninstall completed successfully, False otherwise</returns>
        public bool UninstallSelectedMod(
            [CanBeNull] Action<string, string> showErrorDialog = null,
            [CanBeNull] Func<string, string, bool> showYesNoDialog = null,
            [CanBeNull] Func<string, string, bool?> showYesNoCancelDialog = null,
            [CanBeNull] ModUninstallerUiStrings ui = null)
        {
            ModUninstallerUiStrings text = ui ?? ModUninstallerUiStrings.EnglishDefaults;
            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "UninstallSelectedMod: begin backupsRoot={0} gamePath={1}", _backupsLocationPath, _gamePath));

            (
                CaseAwarePath mostRecentBackupFolder,
                HashSet<string> existingFiles,
                List<CaseAwarePath> filesInBackup,
                int folderCount) = GetBackupInfo(showErrorDialog, showYesNoDialog, ui);

            if (mostRecentBackupFolder is null)
            {
                _logger.AddDiagnostic("UninstallSelectedMod: abort (no backup folder from GetBackupInfo)");
                return false;
            }

            _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "UninstallSelectedMod: backupFolder={0}, existingFiles={1}, filesInBackup={2}, folderCount={3}",
                mostRecentBackupFolder,
                existingFiles.Count,
                filesInBackup.Count,
                folderCount));

            _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.UsingBackupFolder, mostRecentBackupFolder));

            // Show files to be restored if there are less than 6
            if (filesInBackup.Count < 6)
            {
                foreach (CaseAwarePath item in filesInBackup)
                {
                    string relativePath = Path.GetRelativePath(mostRecentBackupFolder, item);
                    _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.WouldRestoreFile, relativePath));
                }
            }

            // Confirm uninstall with user
            bool confirmed = showYesNoDialog?.Invoke(
                text.ConfirmationTitle,
                text.GetReallyUninstallMessage(existingFiles.Count, filesInBackup.Count, folderCount)
            ) ?? false;

            if (!confirmed)
            {
                _logger.AddDiagnostic("UninstallSelectedMod: user declined uninstall confirmation");
                return false;
            }

            try
            {
                RestoreBackup(mostRecentBackupFolder, existingFiles, filesInBackup);
                _logger.AddDiagnostic("UninstallSelectedMod: RestoreBackup finished without exception");
            }
            catch (Exception e)
            {
                _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "UninstallSelectedMod: RestoreBackup failed type={0} message={1}",
                    e.GetType().FullName, e.Message));
                showErrorDialog?.Invoke(
                    e.GetType().Name,
                    text.GetFailedToRestoreMessage(e.Message)
                );
                return false;
            }

            // Offer to delete restored backup
            int deleteBackupPromptIteration = 0;
            while (true)
            {
                deleteBackupPromptIteration++;
                _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "UninstallSelectedMod: delete-backup prompt iteration={0}", deleteBackupPromptIteration));

                bool deleteBackup = showYesNoDialog?.Invoke(
                    "Uninstall completed!",
                    $"Deleted {existingFiles.Count} files and successfully restored backup created on {mostRecentBackupFolder.Name}{Environment.NewLine}{Environment.NewLine}" +
                    $"Would you like to delete the backup created on {mostRecentBackupFolder.Name} since it now has been restored?"
                ) ?? false;

                if (!deleteBackup)
                {
                    _logger.AddDiagnostic("UninstallSelectedMod: user kept backup folder");
                    break;
                }

                try
                {
                    Directory.Delete(mostRecentBackupFolder, recursive: true);
                    _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "UninstallSelectedMod: deleted backup folder path={0}", mostRecentBackupFolder));
                    _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.DeletedRestoredBackup, mostRecentBackupFolder.Name));
                    break;
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.AddDiagnostic("UninstallSelectedMod: UnauthorizedAccessException while deleting backup; showing permission dialog");
                    bool? result = showYesNoCancelDialog?.Invoke(
                        text.PermissionErrorTitle,
                        text.UnableToDeleteBackupPermissionMessage
                    );

                    _logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "UninstallSelectedMod: permission dialog result={0}", result?.ToString() ?? "null"));

                    if (result == true)
                    {
                        Console.WriteLine(text.GainingPermissionPleaseWait);
                        // Attempt to gain access - in C# this would require platform-specific code
                        // For now, we'll just retry
                        continue;
                    }
                    if (result == false)
                    {
                        continue;
                    }
                    if (result is null)
                    {
                        break;
                    }
                }
            }

            _logger.AddDiagnostic("UninstallSelectedMod: completed successfully");
            return true;
        }
    }
}

