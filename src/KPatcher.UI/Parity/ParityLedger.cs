using System;
using System.Collections.Generic;
using System.Text;

namespace KPatcher.UI.Parity
{
    internal enum ParityConfidenceState
    {
        Proven = 0,
        Inferred = 1,
        ThinEvidence = 2
    }

    internal sealed class ParityLedgerEntry
    {
        internal ParityLedgerEntry(
            string title,
            ParityConfidenceState confidence,
            IReadOnlyList<string> evidence,
            IReadOnlyList<string> caveats)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Confidence = confidence;
            Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
            Caveats = caveats ?? throw new ArgumentNullException(nameof(caveats));
        }

        internal string Title { get; }
        internal ParityConfidenceState Confidence { get; }
        internal IReadOnlyList<string> Evidence { get; }
        internal IReadOnlyList<string> Caveats { get; }
    }

    internal static class ParityLedger
    {
        private static readonly IReadOnlyList<ParityLedgerEntry> _entries = new[]
        {
            new ParityLedgerEntry(
                "Read-only CLI inspection surfaces",
                ParityConfidenceState.Proven,
                new[]
                {
                    "--list-namespaces enumerates available namespace options without requiring a game directory.",
                    "--dry-run renders the configuration summary without starting installation.",
                    "--parity-report prints this shared ledger directly from the CLI."
                },
                new[]
                {
                    "This ledger is curated manually rather than generated from test metadata."
                }),
            new ParityLedgerEntry(
                "Desktop Help trust surface",
                ParityConfidenceState.Proven,
                new[]
                {
                    "The Help window renders the same shared ledger text used by the CLI."
                },
                new[]
                {
                    "This slice improves visibility inside the app, not a full interactive parity dashboard."
                }),
            new ParityLedgerEntry(
                "Broader install parity evidence",
                ParityConfidenceState.ThinEvidence,
                new[]
                {
                    "docs/TSLPATCHER_BUILD_VERIFICATION.md records vendor-pipeline verification and maintenance rules.",
                    "docs/TESTING.md describes the regression and optional parity tiers used to catch drift."
                },
                new[]
                {
                    "Most broader parity evidence still lives in repo docs and tests outside the in-app surface."
                }),
        };

        internal static IReadOnlyList<ParityLedgerEntry> Entries => _entries;

        internal static string BuildReport()
        {
            return BuildReport(Entries);
        }

        internal static string BuildReport(IReadOnlyList<ParityLedgerEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var sb = new StringBuilder();
            sb.AppendLine("KPatcher Parity Confidence Ledger");
            sb.AppendLine("================================");

            for (int i = 0; i < entries.Count; i++)
            {
                ParityLedgerEntry entry = entries[i];
                sb.AppendLine();
                sb.Append(i + 1);
                sb.Append(". ");
                sb.Append(entry.Title);
                sb.Append(" [");
                sb.Append(FormatConfidenceState(entry.Confidence));
                sb.AppendLine("]");
                sb.AppendLine("   Evidence:");
                foreach (string evidence in entry.Evidence)
                {
                    sb.Append("   - ");
                    sb.AppendLine(evidence);
                }

                if (entry.Caveats.Count > 0)
                {
                    sb.AppendLine("   Caveats:");
                    foreach (string caveat in entry.Caveats)
                    {
                        sb.Append("   - ");
                        sb.AppendLine(caveat);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        internal static string FormatConfidenceState(ParityConfidenceState confidence)
        {
            switch (confidence)
            {
                case ParityConfidenceState.Proven:
                    return "PROVEN";
                case ParityConfidenceState.Inferred:
                    return "INFERRED";
                case ParityConfidenceState.ThinEvidence:
                    return "THIN EVIDENCE";
                default:
                    throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Unknown parity confidence state.");
            }
        }
    }
}
