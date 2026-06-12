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
    [Trait("Category", "GeneratedGenericModInstallerSmoke")]
    public sealed class EmbeddedScenarioPatternInstallTests
    {
        [Theory]
        [MemberData(nameof(InlineScenarioIds))]
        public void Install_InlineScenario_CompletesWithoutThrow(string scenarioId)
        {
            EmbeddedInstallScenario scenario = EmbeddedScenarioDefinitions.RunnableScenarios
                .First(s => s.Id == scenarioId);

            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Embedded_"))
            {
                scenario.RunInstall(env);
            }
        }

        [Theory]
        [MemberData(nameof(InlineScenarioIds))]
        public void Install_InlineScenario_MatchesGoldenGameManifest(string scenarioId)
        {
            ScenarioGoldenManifests.TryGetFingerprint(scenarioId, out string golden).Should().BeTrue();
            EmbeddedInstallScenario scenario = EmbeddedScenarioDefinitions.RunnableScenarios
                .First(s => s.Id == scenarioId);

            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_EmbeddedGolden_"))
            {
                InstallManifestSnapshot actual = TslPatcherOracleHarness.InstallAndCaptureManifest(env, scenario);
                InstallManifestSnapshot expected = InstallManifestSnapshot.FromFingerprintText(golden);
                InstallManifestDiff diff = actual.DiffAgainst(expected);
                diff.IsEmpty.Should().BeTrue(diff.Describe());
            }
        }

        [Fact]
        public void ManifestInventory_ListsScenarioMetadataForEveryRow()
        {
            string manifestPath = Path.Combine(
                AppContext.BaseDirectory,
                "EmbeddedIntegrationMods",
                "scenario_patterns",
                "manifest.json");
            File.Exists(manifestPath).Should().BeTrue("build copies EmbeddedIntegrationMods to output");

            string json = File.ReadAllText(manifestPath);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                root.ValueKind.Should().Be(JsonValueKind.Array);
                root.GetArrayLength().Should().BeGreaterThan(50);

                foreach (JsonElement row in root.EnumerateArray())
                {
                    row.TryGetProperty("Id", out JsonElement id).Should().BeTrue();
                    id.GetString().Should().NotBeNullOrWhiteSpace();
                    row.TryGetProperty("ChangesIniRelative", out JsonElement iniRel).Should().BeTrue();
                    iniRel.GetString().Should().NotBeNullOrWhiteSpace();
                    row.TryGetProperty("SourceGame", out JsonElement game).Should().BeTrue();
                    game.GetString().Should().NotBeNullOrWhiteSpace();
                }
            }
        }

        [Fact]
        public void ManifestInventory_InlineScenarioIdsAreDisjointFromLegacyManifestIds()
        {
            HashSet<string> manifestIds = ReadManifestIds();
            var inlineIds = new HashSet<string>(EmbeddedScenarioDefinitions.RunnableScenarios.Select(s => s.Id));
            inlineIds.Intersect(manifestIds).Should().BeEmpty(
                "inline characterization ids must not collide with legacy manifest inventory ids");
        }

        public static IEnumerable<object[]> InlineScenarioIds()
        {
            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                yield return new object[] { scenario.Id };
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
