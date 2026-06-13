using System;
using System.Collections.Generic;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Maps <c>scenario_patterns/manifest.json</c> inventory ids to coverage status.
    /// Inline characterization lives in <see cref="EmbeddedScenarioDefinitions"/> (disjoint ids).
    /// </summary>
    public static class ManifestScenarioCoverageRegistry
    {
        public const string InlineCharacterization = "inline_characterization";
        public const string LegacyInventory = "legacy_inventory_pending_migration";

        /// <summary>
        /// Install pipeline stages covered by inline characterization scenarios (not manifest row ids).
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> PipelineStageInlineScenarios { get; } =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Settings", new[] { "inline_settings_only", "inline_installer_mode_false_hack" } },
                { "InstallList", new[] { "inline_install_marker", "inline_install_replace", "inline_namespace_subfolder", "inline_backup_files_replace", "inline_install_source_subfolder" } },
                { "2DAList", new[] { "inline_2da_change_row", "inline_2da_add_column", "inline_2da_exclusive_fallback", "inline_2da_ssf_memory" } },
                { "GFFList", new[] { "inline_gff_uint8", "inline_tlk_gff_strref", "inline_gff_add_field" } },
                { "TLKList", new[] { "inline_tlk_gff_strref", "inline_capsule_dialog_tlk", "inline_protected_dialog_skip" } },
                { "HACKList", new[] { "inline_hack_byte", "inline_installer_mode_false_hack", "inline_hack_rename_source" } },
                { "CompileList", new[] { "inline_compile_void_main", "inline_compile_module_capsule", "inline_custom_nwscript_compile" } },
                { "SSFList", new[] { "inline_ssf_battlecry", "inline_2da_ssf_memory" } },
                { "OverrideType", new[] { "inline_override_type_ignore_module" } },
                { "Namespace", new[] { "inline_namespace_subfolder", "inline_custom_ini_name", "inline_variant_ini_filename" } }
            };

        public static string GetCoverageKind(string manifestId)
        {
            if (string.IsNullOrWhiteSpace(manifestId))
            {
                throw new ArgumentException("manifest id required", nameof(manifestId));
            }

            return LegacyInventory;
        }

        public static bool IsInlineCharacterizationScenario(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                return false;
            }

            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                if (string.Equals(scenario.Id, scenarioId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyCollection<string> InlineCharacterizationScenarioIds
        {
            get
            {
                var ids = new List<string>();
                foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
                {
                    ids.Add(scenario.Id);
                }

                return ids;
            }
        }
    }
}
