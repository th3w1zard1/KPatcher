using System;
using JetBrains.Annotations;

namespace KPatcher.Core.Uninstall
{
    /// <summary>
    /// Localizable strings for ModUninstaller GUI dialogs. When null is passed to uninstall APIs,
    /// <see cref="EnglishDefaults"/> is used (CLI / headless).
    /// </summary>
    public sealed class ModUninstallerUiStrings
    {
        [NotNull]
        public string NoBackupsTitle { get; set; }

        [NotNull]
        public Func<string, string> GetNoBackupsMessage { get; set; }

        [NotNull]
        public string BackupMismatchTitle { get; set; }

        [NotNull]
        public Func<string> GetBackupMismatchMessage { get; set; }

        [NotNull]
        public string ConfirmationTitle { get; set; }

        [NotNull]
        public Func<int, int, int, string> GetReallyUninstallMessage { get; set; }

        [NotNull]
        public Func<string, string> GetFailedToRestoreMessage { get; set; }

        [NotNull]
        public string UninstallCompletedTitle { get; set; }

        [NotNull]
        public Func<int, string, string> GetDeleteBackupPromptMessage { get; set; }

        [NotNull]
        public string PermissionErrorTitle { get; set; }

        [NotNull]
        public string UnableToDeleteBackupPermissionMessage { get; set; }

        [NotNull]
        public string GainingPermissionPleaseWait { get; set; }

        /// <summary>English strings matching legacy ModUninstaller behavior.</summary>
        [NotNull]
        public static ModUninstallerUiStrings EnglishDefaults { get; } = new ModUninstallerUiStrings
        {
            NoBackupsTitle = "No backups found!",
            GetNoBackupsMessage = path =>
                $"No backups found at '{path}'!{Environment.NewLine}KPatcher cannot uninstall TSLPatcher.exe installations.",
            BackupMismatchTitle = "Backup out of date or mismatched",
            GetBackupMismatchMessage = () =>
                $"This backup doesn't match your current KOTOR installation. Files are missing/changed in your KOTOR install.{Environment.NewLine}" +
                $"It is important that you uninstall all mods in their installed order when utilizing this feature.{Environment.NewLine}" +
                $"Also ensure you selected the right mod, and the right KOTOR folder.{Environment.NewLine}" +
                "Continue anyway?",
            ConfirmationTitle = "Confirmation",
            GetReallyUninstallMessage = (existing, files, folders) =>
                $"Really uninstall {existing} files and restore the most recent backup (containing {files} files and {folders} folders)?{Environment.NewLine}" +
                "Note: This uses the most recent mod-specific backup, the namespace option displayed does not affect this tool.",
            GetFailedToRestoreMessage = exMessage =>
                $"Failed to restore backup because of exception.{Environment.NewLine}{Environment.NewLine}{exMessage}",
            UninstallCompletedTitle = "Uninstall completed!",
            GetDeleteBackupPromptMessage = (deletedCount, backupName) =>
                $"Deleted {deletedCount} files and successfully restored backup created on {backupName}{Environment.NewLine}{Environment.NewLine}" +
                $"Would you like to delete the backup created on {backupName} since it now has been restored?",
            PermissionErrorTitle = "Permission Error",
            UnableToDeleteBackupPermissionMessage =
                "Unable to delete the restored backup due to permission issues. Would you like to gain permission and try again?",
            GainingPermissionPleaseWait = "Gaining permission, please wait...",
        };
    }
}
