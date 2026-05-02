using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.TLK
{

    /// <summary>
    /// TLK modification algorithms for KPatcher/KPatcher.
    ///
    /// This module implements TLK modification logic for applying patches from changes.ini files.
    /// Handles string additions, modifications, and memory token resolution.
    /// </summary>

    /// <summary>
    /// Container for TLK (talk table) modifications.
    /// </summary>
    public class ModificationsTLK : PatcherModifications
    {
        public new const string DEFAULT_DESTINATION = ".";
        public const string DEFAULT_SOURCEFILE = "append.tlk";
        public const string DEFAULT_SOURCEFILE_F = "appendf.tlk";
        public const string DEFAULT_SAVEAS_FILE = "dialog.tlk";
        public const string DEFAULT_SAVEAS_FILE_F = "dialogf.tlk";

        public static string DefaultDestination => DEFAULT_DESTINATION;

        public List<ModifyTLK> Modifiers { get; set; } = new List<ModifyTLK>();
        public string SourcefileF { get; set; } = DEFAULT_SOURCEFILE_F;

        public ModificationsTLK(
            [CanBeNull] string filename = null,
            bool replace = false,
            [CanBeNull] List<ModifyTLK> modifiers = null)
            : base(filename, replace)
        {
            Destination = DEFAULT_DESTINATION;
            Modifiers = modifiers ?? new List<ModifyTLK>();
            SourcefileF = DEFAULT_SOURCEFILE_F; // Polish version of k1
            if (string.IsNullOrWhiteSpace(SourceFile))
            {
                SourceFile = DEFAULT_SOURCEFILE;
            }

            SaveAs = DEFAULT_SAVEAS_FILE;
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            string label = SaveAs ?? SourceFile ?? "";
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsTLK.PatchResource: saveAs={0} sourceBytes={1} modifierCount={2} game={3}",
                label, source?.Length ?? 0, Modifiers.Count, game));

            var reader = new TLKBinaryReader(source);
            Formats.TLK.TLK dialog = reader.Load();
            Apply(dialog, memory, logger, game);

            var writer = new TLKBinaryWriter(dialog);
            byte[] written = writer.Write();
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsTLK.PatchResource: wrote saveAs={0} outBytes={1} entryCount={2}",
                label, written.Length, dialog.Count));
            return written;
        }

        /// <summary>
        /// Populates the KPatcher variables from the file section dictionary.
        ///
        /// Args:
        /// ----
        ///     file_section_dict: CaseInsensitiveDict[str] - The file section dictionary
        ///     default_destination: str | None - The default destination
        ///     default_sourcefolder: str - The default source folder
        /// </summary>
        public override void PopTslPatcherVars(
            [CanBeNull] Dictionary<string, string> fileSectionDict,
            [CanBeNull] string defaultDestination = null,
            string defaultSourceFolder = ".")
        {
            if (fileSectionDict.ContainsKey("!ReplaceFile"))
            {
                throw new ArgumentException("!ReplaceFile is not supported in [TLKList]");
            }
            if (fileSectionDict.ContainsKey("!OverrideType"))
            {
                throw new ArgumentException("!OverrideType is not supported in [TLKList]");
            }

            // Can be null if not found
            SourcefileF = fileSectionDict.TryGetValue("!SourceFileF", out string sf) ? sf : DEFAULT_SOURCEFILE_F;
            if (fileSectionDict.ContainsKey("!SourceFileF"))
            {
                fileSectionDict.Remove("!SourceFileF");
            }
            base.PopTslPatcherVars(fileSectionDict, defaultDestination ?? DEFAULT_DESTINATION, defaultSourceFolder);
        }

        /// <summary>
        /// Applies the TLK patches to the TLK.
        ///
        /// Args:
        /// ----
        ///     mutable_data: TLK - The TLK to apply the patches to
        ///     memory: PatcherMemory - The memory context
        ///     logger: PatchLogger - The logger
        ///     game: Game - The game
        /// </summary>
        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            if (!(mutableData is Formats.TLK.TLK dialog))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedTlkObjectButGotFormat, mutableData.GetType().Name));
                return;
            }

            int countBefore = dialog.Count;
            int appendModifierCount = 0;

            foreach (ModifyTLK modifier in Modifiers)
            {
                try
                {
                    if (!modifier.IsReplacement)
                        appendModifierCount++;
                    modifier.Apply(dialog, memory);
                    logger.CompletePatch();
                }
                catch (IndexOutOfRangeException ex)
                {
                    // TSLPatcher: "StrRef token \"%s\" in modifier list that was not present in the TL..."
                    logger.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.StrRefTokenNotInTLK, modifier.TokenId));
                }
            }

            int appended = dialog.Count - countBefore;
            if (appendModifierCount > 0 && appended == 0)
                logger.AddWarning(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.NoNewEntriesAppendedToTlk, SaveAs ?? "TLK"));
        }
    }

    /// <summary>
    /// Represents a single TLK string modification.
    /// </summary>
    public class ModifyTLK
    {
        [CanBeNull]
        public string TlkFilePath { get; set; }
        [CanBeNull]
        public string Text { get; set; } = "";
        [CanBeNull]
        public string Sound { get; set; } = "";

        public int ModIndex { get; set; }
        public int TokenId { get; set; }
        public bool IsReplacement { get; set; }

        public ModifyTLK(int tokenId, bool isReplacement = false)
        {
            TokenId = tokenId;
            ModIndex = tokenId;
            IsReplacement = isReplacement;
        }

        public void Apply(Formats.TLK.TLK dialog, PatcherMemory memory)
        {
            Load();
            if (IsReplacement)
            {
                // For replacements, replace the entry at TokenId
                // dialog.replace(self.token_id, self.text, str(self.sound))
                dialog.Replace(TokenId, Text ?? "", Sound ?? "");
                // Replace operations do NOT store memory tokens
            }
            else
            {
                int stringref = dialog.Add(Text ?? "", Sound ?? "");
                memory.MemoryStr[TokenId] = stringref;
            }
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(TlkFilePath))
            {
                return;
            }

            if (!File.Exists(TlkFilePath))
            {
                throw new FileNotFoundException($"TLK file not found: {TlkFilePath}", TlkFilePath);
            }

            byte[] bytes = File.ReadAllBytes(TlkFilePath);
            var reader = new TLKBinaryReader(bytes);
            Formats.TLK.TLK lookupTlk = reader.Load();
            if (string.IsNullOrEmpty(Text))
            {
                Text = lookupTlk.String(ModIndex);
            }

            if (string.IsNullOrEmpty(Sound))
            {
                Sound = lookupTlk.Get(ModIndex)?.Voiceover.ToString();
            }
        }
    }
}
