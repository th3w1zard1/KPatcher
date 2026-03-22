using System;
using System.Collections.Generic;
using System.Globalization;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.TwoDA
{

    /// <summary>
    /// 2DA modification algorithms for KPatcher.
    /// 
    /// This module implements 2DA modification logic for applying patches from changes.ini files.
    /// Handles row/column additions, cell modifications, and memory token resolution.
    /// </summary>

    /// <summary>
    /// Container for 2DA file modifications.
    /// </summary>
    public class Modifications2DA : PatcherModifications
    {
        public new const string DEFAULT_DESTINATION = PatcherModifications.DEFAULT_DESTINATION;
        public static string DefaultDestination => DEFAULT_DESTINATION;

        public static readonly Dictionary<string, int> HardcappedRowLimits = new Dictionary<string, int>()
        {
            { "placeables.2da", 256 },
            { "upcrystals.2da", 256 },
            { "upgrade.2da", 32 }
        };

        public List<Modify2DA> Modifiers { get; set; } = new List<Modify2DA>();
        public Dictionary<int, RowValue> FileStore2DA { get; } = new Dictionary<int, RowValue>();
        public Dictionary<int, RowValue> FileStoreTLK { get; } = new Dictionary<int, RowValue>();

        public Modifications2DA(string filename)
            : base(filename)
        {
            Modifiers = new List<Modify2DA>();
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            string label = SaveAs ?? SourceFile ?? "";
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "Modifications2DA.PatchResource: saveAs={0} sourceBytes={1} modifierCount={2} game={3}",
                label, source?.Length ?? 0, Modifiers.Count, game));

            Formats.TwoDA.TwoDA twoda;
            try
            {
                twoda = new TwoDABinaryReader(source).Load();
            }
            catch (Exception)
            {
                // TSLPatcher parity: "Unable to load the 2DA file %s! Skipping it..."
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "Modifications2DA.PatchResource: TwoDABinaryReader.Load failed for {0}; returning original bytes", label));
                logger.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.UnableToLoad2DAFileSkipping, SaveAs ?? SourceFile ?? ""));
                return source;
            }

            Apply(twoda, memory, logger, game);

            // If game is K2, return before hardcap check.
            // K1 enforces 256-row caps on placeables/upcrystals etc.; TSL/K2 does not (vanilla K2 placeables can exceed 256).
            if (!game.IsK2() && HardcappedRowLimits.TryGetValue(SaveAs.ToLowerInvariant(), out int twodaRowLimit))
            {
                int curRowCount = twoda.GetHeight();
                if (curRowCount > twodaRowLimit)
                {
                    int rowsOverLimit = curRowCount - twodaRowLimit;
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "Modifications2DA.PatchResource: K1 hardcap rejected saveAs={0} limit={1} rows={2} overBy={3}; returning original bytes",
                        SaveAs, twodaRowLimit, curRowCount, rowsOverLimit));
                    logger.AddError(
                        $"{SaveAs} has a max row count of {twodaRowLimit} on KOTOR 1. Result has {curRowCount} rows ({rowsOverLimit} over the limit); changes were not applied.");
                    return source;
                }
            }

            byte[] written = new TwoDABinaryWriter(twoda).Write();
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "Modifications2DA.PatchResource: wrote saveAs={0} outBytes={1} rowCount={2}",
                label, written.Length, twoda.GetHeight()));
            return written;
        }

        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            if (!(mutableData is Formats.TwoDA.TwoDA twoda))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedTwoDAObjectButGotFormat, mutableData.GetType().Name));
                return;
            }

            TwoDARow lastRow = null;

            var ordered = new List<Modify2DA>();
            ordered.AddRange(Modifiers.FindAll(m => m is AddColumn2DA));
            ordered.AddRange(Modifiers.FindAll(m => m is ChangeRow2DA));
            ordered.AddRange(Modifiers.FindAll(m => m is AddRow2DA));
            ordered.AddRange(Modifiers.FindAll(m => m is CopyRow2DA));
            ordered.AddRange(Modifiers.FindAll(m =>
                !(m is AddColumn2DA) && !(m is ChangeRow2DA) && !(m is CopyRow2DA) && !(m is AddRow2DA)));

            foreach (Modify2DA row in ordered)
            {
                try
                {
                    row.Apply(twoda, memory);
                    if (row is IRowTracking2DA tracker && tracker.LastRow != null)
                    {
                        lastRow = tracker.LastRow;
                    }
                }
                catch (Exception e)
                {
                    if (e is WarningError)
                    {
                        logger.AddWarning($"{e.Message} when patching the file '{SaveAs}'");
                    }
                    else if (e is IndexOutOfRangeException)
                    {
                        // TSLPatcher parity: "Error looking up row label for row index %s"
                        string identifier = (row is ChangeRow2DA cr ? cr.Identifier :
                                            row is AddRow2DA ar ? ar.Identifier :
                                            row is CopyRow2DA cpr ? cpr.Identifier :
                                            row is AddColumn2DA ac ? ac.Identifier : "");
                        logger.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.ErrorLookingUpRowLabelForRowIndex, e.Message?.Trim() ?? identifier));
                        break;
                    }
                    else if (e is KeyNotFoundException kn)
                    {
                        // TSLPatcher parity: column vs row label
                        string detail = kn.Message?.Trim() ?? "";
                        string identifier = (row is ChangeRow2DA cr ? cr.Identifier :
                                            row is AddRow2DA ar ? ar.Identifier :
                                            row is CopyRow2DA cpr ? cpr.Identifier :
                                            row is AddColumn2DA ac ? ac.Identifier : "");
                        if (detail.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0)
                            logger.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.ErrorLookingUpColumnLabelForColumnIndex, detail));
                        else
                            logger.AddError(string.Format(TSLPatcherMessages.UsedAsIndexBut2DAHasNoLabel, detail, identifier));
                        break;
                    }
                    else
                    {
                        // TSLPatcher parity: copy/add-line messages
                        if (row is CopyRow2DA)
                            logger.AddError(TSLPatcherMessages.FailedToCopyLine2DA);
                        else if (row is AddRow2DA addRow)
                            logger.AddError(string.Format(System.Globalization.CultureInfo.CurrentCulture, TSLPatcherMessages.ErrorAddingNewLine2DA, addRow.Identifier));
                        else
                            logger.AddError($"{e.Message} when patching the file '{SaveAs}'");
                        break;
                    }
                }
            }
            if (game.IsK2())
            {
                return;
            }

            // Apply file-level token storage using the last modified row (if any).
            foreach ((int tokenId, RowValue value) in FileStore2DA)
            {
                memory.Memory2DA[tokenId] = value.Value(memory, twoda, lastRow);
            }

            foreach ((int tokenId, RowValue value) in FileStoreTLK)
            {
                string strVal = value.Value(memory, twoda, lastRow);
                if (!string.IsNullOrEmpty(strVal))
                {
                    memory.MemoryStr[tokenId] = int.Parse(strVal);
                }
            }
        }
    }
}
