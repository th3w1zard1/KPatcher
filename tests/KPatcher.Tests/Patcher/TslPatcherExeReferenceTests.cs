using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    /// <summary>
    /// Optional KPatcher vs TSLPatcher.exe install comparison tier.
    /// Skips when <c>KPATCHER_TSLPATCHER_EXE</c> is unset; run via TslPatcherExeReference.runsettings.
    /// </summary>
    public sealed class TslPatcherExeReferenceTests
    {
        [Fact]
        [Trait("Category", "TslPatcherExeReference")]
        public void ReferenceExe_WhenConfigured_IsAvailableForFutureGoldenComparisons()
        {
            if (!TslPatcherOracleHarness.ShouldRunExeReferenceTier)
            {
                return;
            }
        }
    }
}
