using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.GFF
{

    /// <summary>
    /// Container for GFF file modifications.
    /// </summary>
    public class ModificationsGFF : PatcherModifications
    {
        public new const string DEFAULT_DESTINATION = "Override";
        public static string DefaultDestination => DEFAULT_DESTINATION;

        public List<ModifyGFF> Modifiers { get; set; } = new List<ModifyGFF>();

        public ModificationsGFF(
            string filename,
            bool replace = false,
            [CanBeNull] List<ModifyGFF> modifiers = null)
            : base(filename, replace)
        {
            Modifiers = modifiers ?? new List<ModifyGFF>();
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            string label = SaveAs ?? SourceFile ?? "";
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsGFF.PatchResource: saveAs={0} sourceBytes={1} modifierCount={2} replaceFile={3} game={4}",
                label, source?.Length ?? 0, Modifiers.Count, ReplaceFile, game));

            var reader = new GFFBinaryReader(source);
            Formats.GFF.GFF gff = reader.Load();
            Apply(gff, memory, logger, game);
            var writer = new GFFBinaryWriter(gff);
            byte[] written = writer.Write();
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsGFF.PatchResource: wrote saveAs={0} outBytes={1}", label, written.Length));
            return written;
        }

        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            if (mutableData is Formats.GFF.GFF gff)
            {
                foreach (ModifyGFF changeField in Modifiers)
                {
                    changeField.Apply(gff.Root, memory, logger);
                }
            }
            else
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedGffObjectButGotFormat, mutableData.GetType().Name));
            }
        }
    }
}
