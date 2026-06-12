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

        public static string GetCoverageKind(string manifestId)
        {
            if (string.IsNullOrWhiteSpace(manifestId))
            {
                throw new ArgumentException("manifest id required", nameof(manifestId));
            }

            return LegacyInventory;
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
