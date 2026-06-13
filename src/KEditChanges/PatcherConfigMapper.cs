using System.Collections.Generic;
using KPatcher.Core.Config;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.GFF;
using KPatcher.Core.Mods.NCS;
using KPatcher.Core.Mods.NSS;
using KPatcher.Core.Mods.SSF;
using KPatcher.Core.Mods.TwoDA;

namespace KEditChanges
{
    internal static class PatcherConfigMapper
    {
        public static ModificationsByType ToModificationsByType(PatcherConfig config)
        {
            var modifications = ModificationsByType.CreateEmpty();
            if (config == null)
            {
                return modifications;
            }

            if (config.PatchesTLK != null && config.PatchesTLK.Modifiers.Count > 0)
            {
                modifications.Tlk.Add(config.PatchesTLK);
            }

            modifications.Install = config.InstallList ?? new List<InstallFile>();
            modifications.Twoda = config.Patches2DA ?? new List<Modifications2DA>();
            modifications.Gff = config.PatchesGFF ?? new List<ModificationsGFF>();
            modifications.Ssf = config.PatchesSSF ?? new List<ModificationsSSF>();
            modifications.Nss = config.PatchesNSS ?? new List<ModificationsNSS>();
            modifications.Ncs = config.PatchesNCS ?? new List<ModificationsNCS>();
            return modifications;
        }
    }
}
