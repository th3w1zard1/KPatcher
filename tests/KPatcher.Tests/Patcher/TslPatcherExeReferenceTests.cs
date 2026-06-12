using System;
using System.IO;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    /// <summary>
    /// Optional KPatcher vs TSLPatcher.exe install comparison tier.
    /// No-op when <c>KPATCHER_TSLPATCHER_EXE</c> is unset; run via TslPatcherExeReference.runsettings.
    /// </summary>
    public sealed class TslPatcherExeReferenceTests
    {
        [Fact]
        [Trait("Category", "TslPatcherExeReference")]
        public void ReferenceExe_WhenConfigured_IsAvailableForFutureGoldenComparisons()
        {
            string exePath = Environment.GetEnvironmentVariable("KPATCHER_TSLPATCHER_EXE");
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return;
            }

            Assert.True(File.Exists(exePath), "KPATCHER_TSLPATCHER_EXE must point to an existing TSLPatcher executable.");
        }
    }
}
