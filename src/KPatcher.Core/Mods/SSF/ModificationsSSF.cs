using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.SSF
{

    /// <summary>
    /// Represents a single SSF sound modification.
    /// </summary>
    public class ModifySSF
    {
        public SSFSound Sound { get; set; }
        public TokenUsage Stringref { get; set; }

        public ModifySSF(SSFSound sound, TokenUsage stringref)
        {
            Sound = sound;
            Stringref = stringref;
        }

        public void Apply(Formats.SSF.SSF ssf, PatcherMemory memory)
        {
            ssf.SetData(Sound, int.Parse(Stringref.Value(memory)));
        }
    }

    /// <summary>
    /// SSF modification algorithms for KPatcher/KPatcher.
    /// 
    /// This module implements SSF modification logic for applying patches from changes.ini files.
    /// Handles sound set entry modifications and memory token resolution.
    /// </summary>
    public class ModificationsSSF : PatcherModifications
    {
        public new const string DEFAULT_DESTINATION = PatcherModifications.DEFAULT_DESTINATION;
        public static string DefaultDestination => DEFAULT_DESTINATION;

        public List<ModifySSF> Modifiers { get; set; }

        public ModificationsSSF(string filename, bool replace, [CanBeNull] List<ModifySSF> modifiers = null)
            : base(filename, replace)
        {
            ReplaceFile = replace;
            Modifiers = modifiers ?? new List<ModifySSF>();
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            string label = SaveAs ?? SourceFile ?? "";
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsSSF.PatchResource: saveAs={0} sourceBytes={1} modifierCount={2} game={3}",
                label, source?.Length ?? 0, Modifiers.Count, game));

            var reader = new SSFBinaryReader(source);
            Formats.SSF.SSF ssf = reader.Load();
            Apply(ssf, memory, logger, game);

            var writer = new SSFBinaryWriter(ssf);
            byte[] written = writer.Write();
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsSSF.PatchResource: wrote saveAs={0} outBytes={1}", label, written.Length));
            return written;
        }

        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            if (mutableData is Formats.SSF.SSF ssf)
            {
                foreach (ModifySSF modifier in Modifiers)
                {
                    modifier.Apply(ssf, memory);
                }
            }
            else
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedSsfObjectButGotFormat, mutableData.GetType().Name));
            }
        }
    }
}
