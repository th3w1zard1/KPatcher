using KPatcher.Core.Resources;

namespace KPatcher.Core.Common
{
    /// <summary>
    /// User-facing messages that match TSLPatcher.exe exactly for parity.
    /// Sourced from reverse engineering (docs/TSLPATCHER_RE_*_DELIVERABLE.md).
    /// Values come from PatcherResources (localized).
    /// </summary>
    public static class TSLPatcherMessages
    {
        /// <summary>Button label (TSLPatcher .rsrc 0x004a2ba8). Ampersand = Alt+S shortcut.</summary>
        public static string StartPatchingButton => PatcherResources.StartPatchingButton;

        public static string StartPatchingConfirmation => PatcherResources.StartPatchingConfirmation;

        public static string InvalidGameFolderDialogTlkNotFound => PatcherResources.InvalidGameFolderDialogTlkNotFound;

        public static string NoValidGameFolderSelected => PatcherResources.NoValidGameFolderSelected;

        public static string SkippingFileNoOverwriteDialogTlk => PatcherResources.SkippingFileNoOverwriteDialogTlk;

        public static string NwnnsscompNotFoundInTslPatchData => PatcherResources.NwnnsscompNotFoundInTslPatchData;

        public static string Invalid2DAMemoryToken => PatcherResources.Invalid2DAMemoryToken;

        public static string PatcherFinished => PatcherResources.PatcherFinished;

        public static string PatcherFinishedWithWarnings => PatcherResources.PatcherFinishedWithWarnings;

        public static string PatcherFinishedWithErrors => PatcherResources.PatcherFinishedWithErrors;

        public static string PatcherFinishedWithErrorsAndWarnings => PatcherResources.PatcherFinishedWithErrorsAndWarnings;

        public static string StrRefTokenNotInTLK => PatcherResources.StrRefTokenNotInTLK;

        public static string NoNewEntriesAppendedToTlk => PatcherResources.NoNewEntriesAppendedToTlk;

        public static string NoTLKFileLoaded => PatcherResources.NoTLKFileLoaded;

        public static string UnableToLocateTLKFileToPatch => PatcherResources.UnableToLocateTLKFileToPatch;

        public static string UnableToFind2DAFileToModify => PatcherResources.UnableToFind2DAFileToModify;

        public static string ErrorLookingUpRowLabelForRowIndex => PatcherResources.ErrorLookingUpRowLabelForRowIndex;

        public static string ErrorLookingUpColumnLabelForColumnIndex => PatcherResources.ErrorLookingUpColumnLabelForColumnIndex;

        public static string UsedAsIndexBut2DAHasNoLabel => PatcherResources.UsedAsIndexBut2DAHasNoLabel;

        public static string FailedToCopyLine2DA => PatcherResources.FailedToCopyLine2DA;

        public static string UnableToLoad2DAFileSkipping => PatcherResources.UnableToLoad2DAFileSkipping;

        public static string NoValueAssignedColumn2DA => PatcherResources.NoValueAssignedColumn2DA;

        public static string ErrorAddingNewLine2DA => PatcherResources.ErrorAddingNewLine2DA;

        public static string InternalErrorInvalidTLKFileType => PatcherResources.InternalErrorInvalidTLKFileType;

        public static string CriticalErrorUnableToLocateFileToPatch => PatcherResources.CriticalErrorUnableToLocateFileToPatch;

        public static string NoFileToInstallSpecified => PatcherResources.NoFileToInstallSpecified;

        public static string NoInstallPathSet => PatcherResources.NoInstallPathSet;

        public static string FileSetToPatchDoesNotExist => PatcherResources.FileSetToPatchDoesNotExist;

        public static string UnableToLoadInstructionsTslpatchdata => PatcherResources.UnableToLoadInstructionsTslpatchdata;

        /// <summary>Help/About description (.rsrc 0x004ad324).</summary>
        public static string FilePatcherDescription => PatcherResources.FilePatcherDescription;
    }
}
