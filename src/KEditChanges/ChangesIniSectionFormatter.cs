using System;
using System.Collections.Generic;
using System.Globalization;
using KPatcher.Core.Config;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.GFF;
using KPatcher.Core.Mods.NCS;
using KPatcher.Core.Mods.NSS;
using KPatcher.Core.Mods.SSF;
using KPatcher.Core.Mods.TLK;
using KPatcher.Core.Mods.TwoDA;

namespace KEditChanges
{
    /// <summary>
    /// Read-only section entry lines for UI and CLI summary views.
    /// </summary>
    public static class ChangesIniSectionFormatter
    {
        public static List<string> FormatEntries(ChangesIniSectionKind kind, PatcherConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            switch (kind)
            {
                case ChangesIniSectionKind.Settings:
                    return new List<string>
                    {
                        "WindowTitle=" + (config.WindowTitle ?? string.Empty),
                        "LogLevel=" + ((int)config.LogLevel).ToString(CultureInfo.InvariantCulture),
                        "InstallerMode=" + (config.InstallerMode ? "1" : "0")
                    };
                case ChangesIniSectionKind.TlkList:
                    return FormatTlkEntries(config.PatchesTLK);
                case ChangesIniSectionKind.InstallList:
                    return FormatFilePatches(config.InstallList);
                case ChangesIniSectionKind.TwoDaList:
                    return FormatModifierPatches(config.Patches2DA);
                case ChangesIniSectionKind.GffList:
                    return FormatModifierPatches(config.PatchesGFF);
                case ChangesIniSectionKind.CompileList:
                    return FormatFilePatches(config.PatchesNSS);
                case ChangesIniSectionKind.HackList:
                    return FormatFilePatches(config.PatchesNCS);
                case ChangesIniSectionKind.SsfList:
                    return FormatModifierPatches(config.PatchesSSF);
                default:
                    return new List<string>();
            }
        }

        private static List<string> FormatFilePatches<T>(List<T> patches) where T : PatcherModifications
        {
            var lines = new List<string>();
            if (patches == null)
            {
                return lines;
            }

            for (int i = 0; i < patches.Count; i++)
            {
                PatcherModifications patch = patches[i];
                lines.Add(FormatPatchLine(i + 1, patch));
            }

            return lines;
        }

        private static List<string> FormatModifierPatches<T>(List<T> patches) where T : PatcherModifications
        {
            var lines = new List<string>();
            if (patches == null)
            {
                return lines;
            }

            for (int i = 0; i < patches.Count; i++)
            {
                T patch = patches[i];
                int modifierCount = 0;
                if (patch is Modifications2DA twoda)
                {
                    modifierCount = twoda.Modifiers?.Count ?? 0;
                }
                else if (patch is ModificationsGFF gff)
                {
                    modifierCount = gff.Modifiers?.Count ?? 0;
                }
                else if (patch is ModificationsSSF ssf)
                {
                    modifierCount = ssf.Modifiers?.Count ?? 0;
                }

                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}. {1} ({2} modifiers) -> {3}",
                    i + 1,
                    patch.SourceFile ?? "?",
                    modifierCount,
                    patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION));
            }

            return lines;
        }

        private static List<string> FormatTlkEntries(ModificationsTLK tlk)
        {
            var lines = new List<string>();
            if (tlk == null)
            {
                return lines;
            }

            if (!string.IsNullOrEmpty(tlk.SourceFile))
            {
                lines.Add("Source=" + tlk.SourceFile + " SaveAs=" + (tlk.SaveAs ?? string.Empty));
            }

            if (tlk.Modifiers == null)
            {
                return lines;
            }

            for (int i = 0; i < tlk.Modifiers.Count; i++)
            {
                ModifyTLK mod = tlk.Modifiers[i];
                string textPreview = mod.Text ?? string.Empty;
                if (textPreview.Length > 48)
                {
                    textPreview = textPreview.Substring(0, 48) + "...";
                }

                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}. token={1} replace={2} {3}",
                    i + 1,
                    mod.TokenId,
                    mod.IsReplacement ? "1" : "0",
                    textPreview));
            }

            return lines;
        }

        private static string FormatPatchLine(int index, PatcherModifications patch)
        {
            string dest = patch.Destination ?? PatcherModifications.DEFAULT_DESTINATION;
            string saveAs = patch.SaveAs ?? patch.SourceFile ?? "?";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}. {1} -> {2} ({3})",
                index,
                patch.SourceFile ?? "?",
                saveAs,
                dest);
        }
    }
}
