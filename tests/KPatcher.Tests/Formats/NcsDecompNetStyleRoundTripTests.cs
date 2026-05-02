using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using NCSDecomp.Core;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Parity with <c>KNCSDecomp.RoundTripTests/NCSDecompCliRoundTripTests</c> (NCSDecomp.NET): compile → write NCS →
    /// decompile via <see cref="RoundTripUtil.DecompileNcsToNss"/> with <c>null</c> nwscript paths (NCSDecomp loads embedded action tables).
    /// Optional strict bytecode equality: env <c>KNCSDECOMP_STRICT_ROUNDTRIP=1</c> (same name as upstream).
    /// </summary>
    public sealed class NcsDecompNetStyleRoundTripTests
    {
        private const string SimpleScript = @"
void main()
{
    int value = 41;
    value = value + 1;
}
";

        [Fact]
        public void RoundTrip_K1_EmbeddedActions_DecompilesAndRecompiles()
        {
            NCS original = NCSAuto.CompileNss(SimpleScript, Game.K1, null, null, null);
            byte[] originalBytes = NCSAuto.BytesNcs(original);
            originalBytes.Should().NotBeNullOrEmpty();

            string tempRoot = Path.Combine(Path.GetTempPath(), "kpatcher-ncs-roundtrip", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                string inputNcsPath = Path.Combine(tempRoot, "input.ncs");
                File.WriteAllBytes(inputNcsPath, originalBytes);

                string decompiled = RoundTripUtil.DecompileNcsToNss(inputNcsPath, "k1", null, null);
                decompiled.Should().NotBeNullOrWhiteSpace("decompiled NSS should not be empty");

                NCS recompiled = NCSAuto.CompileNss(decompiled, Game.K1, null, null, null);
                byte[] recompiledBytes = NCSAuto.BytesNcs(recompiled);
                recompiledBytes.Should().NotBeNullOrEmpty();

                AssertStrictBytecodeOptional(originalBytes, recompiledBytes, "K1");
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        [Fact]
        public void RoundTrip_Tsl_EmbeddedActions_DecompilesAndRecompiles()
        {
            NCS original = NCSAuto.CompileNss(SimpleScript, Game.TSL, null, null, null);
            byte[] originalBytes = NCSAuto.BytesNcs(original);
            originalBytes.Should().NotBeNullOrEmpty();

            string tempRoot = Path.Combine(Path.GetTempPath(), "kpatcher-ncs-roundtrip-tsl", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                string inputNcsPath = Path.Combine(tempRoot, "input.ncs");
                File.WriteAllBytes(inputNcsPath, originalBytes);

                string decompiled = RoundTripUtil.DecompileNcsToNss(inputNcsPath, "tsl", null, null);
                decompiled.Should().NotBeNullOrWhiteSpace();

                NCS recompiled = NCSAuto.CompileNss(decompiled, Game.TSL, null, null, null);
                byte[] recompiledBytes = NCSAuto.BytesNcs(recompiled);
                recompiledBytes.Should().NotBeNullOrEmpty();

                AssertStrictBytecodeOptional(originalBytes, recompiledBytes, "TSL");
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        private static void AssertStrictBytecodeOptional(byte[] originalBytes, byte[] recompiledBytes, string label)
        {
            string strictMode = Environment.GetEnvironmentVariable("KNCSDECOMP_STRICT_ROUNDTRIP");
            if (!string.Equals(strictMode, "1", StringComparison.Ordinal))
            {
                return;
            }

            recompiledBytes.SequenceEqual(originalBytes).Should().BeTrue(
                $"{label}: round-trip bytecode mismatch under KNCSDECOMP_STRICT_ROUNDTRIP=1");
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }
    }
}
