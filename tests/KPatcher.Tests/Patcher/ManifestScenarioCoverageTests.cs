using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ManifestScenarioCoverageTests
    {
        [Fact]
        public void ManifestInventory_EveryRowHasRegisteredCoverageKind()
        {
            HashSet<string> manifestIds = ReadManifestIds();
            manifestIds.Should().NotBeEmpty();

            foreach (string id in manifestIds)
            {
                ManifestScenarioCoverageRegistry.GetCoverageKind(id)
                    .Should().Be(ManifestScenarioCoverageRegistry.LegacyInventory);
            }
        }

        [Fact]
        public void InlineCharacterization_ScenarioIdsAreDisjointFromManifestInventory()
        {
            HashSet<string> manifestIds = ReadManifestIds();
            var inlineIds = new HashSet<string>(
                ManifestScenarioCoverageRegistry.InlineCharacterizationScenarioIds,
                StringComparer.OrdinalIgnoreCase);

            inlineIds.Should().NotBeEmpty();
            inlineIds.Intersect(manifestIds).Should().BeEmpty();
        }

        [Fact]
        public void InlineCharacterization_HasMinimumPipelineStageCoverage()
        {
            IReadOnlyCollection<string> ids = ManifestScenarioCoverageRegistry.InlineCharacterizationScenarioIds;
            ids.Should().Contain("inline_install_marker");
            ids.Should().Contain("inline_2da_change_row");
            ids.Should().Contain("inline_gff_uint8");
            ids.Should().Contain("inline_tlk_gff_strref");
            ids.Should().Contain("inline_ssf_battlecry");
            ids.Should().Contain("inline_compile_void_main");
            ids.Should().Contain("inline_hack_byte");
            ids.Should().Contain("inline_2da_ssf_memory");
            ids.Should().Contain("inline_compile_module_capsule");
            ids.Should().Contain("inline_2da_exclusive_fallback");
            ids.Should().Contain("inline_capsule_dialog_tlk");
            ids.Should().Contain("inline_protected_dialog_skip");
            ids.Should().Contain("inline_backup_files_replace");
            ids.Should().Contain("inline_custom_nwscript_compile");
            ids.Should().Contain("inline_override_type_ignore_module");
            ids.Should().Contain("inline_namespace_subfolder");
            ids.Should().Contain("inline_gff_add_field");
            ids.Should().Contain("inline_install_source_subfolder");
            ids.Should().Contain("inline_hack_rename_source");
            ids.Should().Contain("inline_custom_ini_name");
            ids.Should().Contain("inline_variant_ini_filename");
        }

        [Fact]
        public void InlineCharacterization_EveryRunnableScenarioHasGoldenFingerprint()
        {
            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                ScenarioGoldenManifests.TryGetFingerprint(scenario.Id, out string golden).Should().BeTrue(
                    "scenario {0} must have ScenarioGoldenManifests entry",
                    scenario.Id);
                golden.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public void PipelineStageRegistry_EveryStageReferencesRegisteredInlineScenarios()
        {
            var inlineIds = new HashSet<string>(
                ManifestScenarioCoverageRegistry.InlineCharacterizationScenarioIds,
                StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, IReadOnlyList<string>> stage in ManifestScenarioCoverageRegistry.PipelineStageInlineScenarios)
            {
                stage.Value.Should().NotBeEmpty("stage " + stage.Key);
                foreach (string scenarioId in stage.Value)
                {
                    inlineIds.Should().Contain(scenarioId, "stage {0} references {1}", stage.Key, scenarioId);
                    ManifestScenarioCoverageRegistry.IsInlineCharacterizationScenario(scenarioId).Should().BeTrue();
                }
            }
        }

        private static HashSet<string> ReadManifestIds()
        {
            string manifestPath = Path.Combine(
                AppContext.BaseDirectory,
                "EmbeddedIntegrationMods",
                "scenario_patterns",
                "manifest.json");
            string json = File.ReadAllText(manifestPath);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (JsonElement row in doc.RootElement.EnumerateArray())
                {
                    ids.Add(row.GetProperty("Id").GetString());
                }

                return ids;
            }
        }
    }
}
