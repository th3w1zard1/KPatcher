// Copyright (c) 2025 KEditChanges contributors / KPatcher
// ChangeEdit.exe reverse-engineering metadata (OpenKotOR/ChangeEdit Delphi reference).

namespace KEditChanges
{
    /// <summary>
    /// Ghidra / agdec mapping metadata for ChangeEdit.exe (Borland Delphi 7 VCL).
  /// Product logic lives in managed C# under KEditChanges and KPatcher.Core.
    /// </summary>
    public static class ChangeEditReMapping
    {
        public const string ReferenceBinaryName = "ChangeEdit.exe";
        public const string GhidraProjectHint = "ChangeEdit.exe.gzf";
        public const string BorlandDelphiVersion = "Delphi 7";
        public const string ManagedCounterpart = "KEditChanges";

        /// <summary>Primary entry from agdec/Ghidra (ChangeEdit.exe).</summary>
        public const string EntryAddress = "0x004b03a0";

        public const string Info =
            "KEditChanges — managed port of OpenKotOR/ChangeEdit (changes.ini editor). " +
            "Binary formats and patch application: KPatcher.Core. RE entry " + EntryAddress + ".";
    }
}
