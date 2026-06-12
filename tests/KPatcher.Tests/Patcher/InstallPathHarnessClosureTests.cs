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
    /// <summary>
    /// Single gate asserting install-path harness completeness (characterization, oracle, manifest taxonomy).
    /// </summary>
    [Trait("Category", "GeneratedGenericModInstallerSmoke")]
    public sealed class InstallPathHarnessClosureTests
    {
        private static readonly HashSet<string> CliOracleExcludedScenarioIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "inline_settings_only",
            "inline_custom_ini_name",
            "inline_variant_ini_filename"
        };

        [Fact]
        public void ExpertHarnessClosure_AllInstallPathGatesSatisfied()
        {
            IReadOnlyList<EmbeddedInstallScenario> scenarios = EmbeddedScenarioDefinitions.RunnableScenarios;
            scenarios.Count.Should().BeGreaterOrEqualTo(25);

            var inlineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EmbeddedInstallScenario scenario in scenarios)
            {
                inlineIds.Add(scenario.Id);
                ScenarioGoldenManifests.TryGetFingerprint(scenario.Id, out string golden).Should().BeTrue();
                golden.Should().NotBeNullOrWhiteSpace();
            }

            inlineIds.Intersect(ManifestScenarioInventoryIds()).Should().BeEmpty();

            foreach (KeyValuePair<string, IReadOnlyList<string>> stage in ManifestScenarioCoverageRegistry.PipelineStageInlineScenarios)
            {
                stage.Value.Should().NotBeEmpty();
                foreach (string scenarioId in stage.Value)
                {
                    inlineIds.Should().Contain(scenarioId);
                }
            }

            foreach (KeyValuePair<string, IReadOnlyList<string>> pattern in ManifestIniPathPatternRegistry.PatternInlineScenarios)
            {
                pattern.Value.Should().NotBeEmpty();
                foreach (string scenarioId in pattern.Value)
                {
                    inlineIds.Should().Contain(scenarioId);
                }
            }

            foreach (string manifestIniRelative in ManifestScenarioInventoryIniPaths())
            {
                string pattern = ManifestIniPathPatternRegistry.Classify(manifestIniRelative);
                ManifestIniPathPatternRegistry.PatternInlineScenarios[pattern].Should().NotBeEmpty();
            }

            int cliEligible = scenarios.Count(s => !CliOracleExcludedScenarioIds.Contains(s.Id));
            cliEligible.Should().Be(scenarios.Count - CliOracleExcludedScenarioIds.Count);
        }

        private static HashSet<string> ManifestScenarioInventoryIds()
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

                ids.Count.Should().Be(116);
                return ids;
            }
        }

        private static IEnumerable<string> ManifestScenarioInventoryIniPaths()
        {
            string manifestPath = Path.Combine(
                AppContext.BaseDirectory,
                "EmbeddedIntegrationMods",
                "scenario_patterns",
                "manifest.json");
            string json = File.ReadAllText(manifestPath);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                foreach (JsonElement row in doc.RootElement.EnumerateArray())
                {
                    yield return row.GetProperty("ChangesIniRelative").GetString();
                }
            }
        }
    }
}
