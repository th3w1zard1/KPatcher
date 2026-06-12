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
    /// Structural validation of <c>scenario_patterns/manifest.json</c> (exhaustive tier).
    /// </summary>
    [Trait("Category", "GeneratedGenericModExhaustive")]
    public sealed class ManifestScenarioInventoryValidationTests
    {
        [Fact]
        public void ManifestInventory_HasExpectedRowCountAndUniqueIds()
        {
            List<ManifestRow> rows = ReadManifestRows();
            rows.Should().HaveCount(116);
            rows.Select(r => r.Id.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void ManifestInventory_EveryRowHasRequiredFieldsAndKnownSourceGame()
        {
            foreach (ManifestRow row in ReadManifestRows())
            {
                row.Id.Should().NotBeNullOrWhiteSpace();
                row.ChangesIniRelative.Should().NotBeNullOrWhiteSpace();
                row.SourceGame.Should().BeOneOf("K1", "TSL", "K2");
                row.ChangesIniRelative.Should().NotContain("..");
            }
        }

        [Fact]
        public void ManifestInventory_HasExpectedSourceGameDistribution()
        {
            List<ManifestRow> rows = ReadManifestRows();
            int k1 = rows.Count(r => r.SourceGame == "K1");
            int tsl = rows.Count(r => r.SourceGame == "TSL");
            k1.Should().Be(64);
            tsl.Should().Be(52);
        }

        [Fact]
        public void ManifestInventory_AllRowsRemainLegacyPendingMigration()
        {
            foreach (ManifestRow row in ReadManifestRows())
            {
                ManifestScenarioCoverageRegistry.GetCoverageKind(row.Id)
                    .Should().Be(ManifestScenarioCoverageRegistry.LegacyInventory);
            }
        }

        private static List<ManifestRow> ReadManifestRows()
        {
            string manifestPath = Path.Combine(
                AppContext.BaseDirectory,
                "EmbeddedIntegrationMods",
                "scenario_patterns",
                "manifest.json");
            string json = File.ReadAllText(manifestPath);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var rows = new List<ManifestRow>();
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    rows.Add(new ManifestRow
                    {
                        Id = element.GetProperty("Id").GetString(),
                        ChangesIniRelative = element.GetProperty("ChangesIniRelative").GetString(),
                        SourceGame = element.GetProperty("SourceGame").GetString()
                    });
                }

                return rows;
            }
        }

        private sealed class ManifestRow
        {
            public string Id { get; set; }
            public string ChangesIniRelative { get; set; }
            public string SourceGame { get; set; }
        }
    }
}
