using KCompiler;
using KPatcher.Core.Common;
using NCSDecomp.Core;
using Xunit;

namespace KPatcher.Tests
{
    /// <summary>
    /// <see cref="RoundTripUtil.CompareManagedRecompileToOriginalDecoderText"/> — managed compile + decoder token equality.
    /// </summary>
    public sealed class RoundTripUtilManagedCompareTests
    {
        [Fact]
        public void CompareManagedRecompile_EmptyNss_ReturnsFailure()
        {
            byte[] dummy = { 0x01 };
            ManagedRoundTripCompareResult r = RoundTripUtil.CompareManagedRecompileToOriginalDecoderText(
                dummy,
                string.Empty,
                k2: false,
                k1NwscriptPath: null,
                k2NwscriptPath: null);

            Assert.False(r.CompileSucceeded);
            Assert.Contains("No NSS", r.Summary);
        }

        [Fact]
        public void CompareManagedRecompile_EmptyOriginalNcs_ReturnsFailure()
        {
            ManagedRoundTripCompareResult r = RoundTripUtil.CompareManagedRecompileToOriginalDecoderText(
                new byte[0],
                "void main() { }",
                k2: false,
                k1NwscriptPath: null,
                k2NwscriptPath: null);

            Assert.False(r.CompileSucceeded);
            Assert.Contains("No original NCS", r.Summary);
        }

        [Fact]
        public void CompareManagedRecompile_VoidMain_DecoderStreamsMatch()
        {
            const string nss = "void main() { }";
            byte[] ncs = ManagedNwnnsscomp.CompileSourceToBytes(nss, Game.K1, null, debug: false, nwscriptPath: null);

            ManagedRoundTripCompareResult r = RoundTripUtil.CompareManagedRecompileToOriginalDecoderText(
                ncs,
                nss,
                k2: false,
                k1NwscriptPath: null,
                k2NwscriptPath: null);

            Assert.True(r.CompileSucceeded, r.Summary);
            Assert.True(r.DecoderOutputsMatch, r.Summary);
        }
    }
}
