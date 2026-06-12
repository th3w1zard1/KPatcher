using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using KPatcher.Core.Logger;
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
                scenario.Seed(env);
                env.WriteChangesIni(scenario.ChangesIniBody);
                var installer = env.CreateInstaller(new PatchLogger());
                installer.Install();
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
                root.GetArrayLength().Should().BeGreaterThan(0);

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

        public static IEnumerable<object[]> InlineScenarioIds()
        {
            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                yield return new object[] { scenario.Id };
            }
        }
    }
}
