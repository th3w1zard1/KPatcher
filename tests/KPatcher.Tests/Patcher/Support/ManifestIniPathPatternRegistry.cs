using System;
using System.Collections.Generic;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Classifies <c>manifest.json</c> <c>ChangesIniRelative</c> path shapes and maps each class
    /// to inline characterization scenarios (pattern coverage, not per-mod byte regression).
    /// </summary>
    public static class ManifestIniPathPatternRegistry
    {
        public const string RootChangesIni = "root_changes_ini";
        public const string SubfolderChangesIni = "subfolder_changes_ini";
        public const string VariantChangesIni = "variant_changes_ini";
        public const string CustomIniName = "custom_ini_name";

        public static string Classify(string changesIniRelative)
        {
            if (string.IsNullOrWhiteSpace(changesIniRelative))
            {
                throw new ArgumentException("changesIniRelative required", nameof(changesIniRelative));
            }

            string normalized = changesIniRelative.Replace('\\', '/').Trim();
            string lower = normalized.ToLowerInvariant();

            if (lower == "changes.ini")
            {
                return RootChangesIni;
            }

            if (lower.EndsWith("/changes.ini", StringComparison.Ordinal))
            {
                return SubfolderChangesIni;
            }

            if (lower.Contains("changes") && lower.EndsWith(".ini", StringComparison.Ordinal))
            {
                return VariantChangesIni;
            }

            if (lower.EndsWith(".ini", StringComparison.Ordinal))
            {
                return CustomIniName;
            }

            throw new ArgumentException("unsupported ini path: " + changesIniRelative, nameof(changesIniRelative));
        }

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> PatternInlineScenarios { get; } =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { RootChangesIni, new[] { "inline_install_marker", "inline_settings_only" } },
                { SubfolderChangesIni, new[] { "inline_namespace_subfolder" } },
                { VariantChangesIni, new[] { "inline_variant_ini_filename" } },
                { CustomIniName, new[] { "inline_custom_ini_name" } }
            };

    }
}
