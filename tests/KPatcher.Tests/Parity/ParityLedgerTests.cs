using System;
using System.Collections.Generic;
using KPatcher.UI.Parity;
using Xunit;

namespace KPatcher.Tests.Parity
{
    public sealed class ParityLedgerTests
    {
        [Fact]
        public void BuildReport_RendersEntriesInOrderWithEvidenceAndCaveats()
        {
            var entries = new List<ParityLedgerEntry>
            {
                new ParityLedgerEntry(
                    "CLI",
                    ParityConfidenceState.Proven,
                    new[] { "Lists namespaces", "Prints dry-run summary" },
                    new[] { "Curated manually" }),
                new ParityLedgerEntry(
                    "Help window",
                    ParityConfidenceState.Inferred,
                    new[] { "Uses shared formatter" },
                    new[] { "No dedicated UI harness" })
            };

            string report = ParityLedger.BuildReport(entries);

            Assert.Contains("KPatcher Parity Confidence Ledger", report);
            Assert.True(report.IndexOf("CLI", StringComparison.Ordinal) < report.IndexOf("Help window", StringComparison.Ordinal));
            Assert.Contains("[PROVEN]", report);
            Assert.Contains("[INFERRED]", report);
            Assert.Contains("Evidence:", report);
            Assert.Contains("Caveats:", report);
            Assert.Contains("Curated manually", report);
        }

        [Fact]
        public void BuildReport_OmitsCaveatSection_WhenEntryHasNoCaveats()
        {
            var entries = new List<ParityLedgerEntry>
            {
                new ParityLedgerEntry(
                    "CLI",
                    ParityConfidenceState.Proven,
                    new[] { "Lists namespaces" },
                    new string[0])
            };

            string report = ParityLedger.BuildReport(entries);

            Assert.Contains("Evidence:", report);
            Assert.DoesNotContain("Caveats:", report);
        }

        [Fact]
        public void FormatConfidenceState_ThrowsForUnknownValue()
        {
            var invalid = (ParityConfidenceState)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => ParityLedger.FormatConfidenceState(invalid));
        }
    }
}
