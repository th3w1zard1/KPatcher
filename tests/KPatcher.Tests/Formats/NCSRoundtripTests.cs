using System.Collections.Generic;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using NCSDecomp.Core;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// NCS binary roundtrip tests ported from Andastra <c>NCSRoundtripTests</c>, plus decoder-token checks and
    /// <see cref="RoundTripUtil.CompareManagedRecompileToOriginalDecoderText"/> (managed recompile vs decoder text).
    /// Full decompile → recompile with on-disk <c>nwscript</c> mirrors NCSDecomp.NET <c>NCSDecompCliRoundTripTests</c>
    /// in <see cref="NcsDecompNetStyleRoundTripTests"/>.
    /// </summary>
    public sealed class NCSRoundtripTests
    {
        private static readonly string[] RoundtripScripts =
        {
            "void main() { int i = 0; while (i < 3) { i = i + 1; } }",
            "void main() { float f = 1.5; string s = \"abc\"; f = f + 2.25; s = s + \"def\"; }",
            "void main() { int a = 1; if (a == 1) { a = 2; } else { a = 3; } }"
        };

        [Theory]
        [InlineData(Game.K1)]
        [InlineData(Game.TSL)]
        public void CompileReadWrite_RoundtripsBytes_WithoutValidationIssues(Game game)
        {
            for (int i = 0; i < RoundtripScripts.Length; i++)
            {
                string source = RoundtripScripts[i];
                NCS compiled = NCSAuto.CompileNss(source, game, null, null, null);
                compiled.Should().NotBeNull($"compile null for script index {i}");
                List<string> issues = compiled.Validate();
                issues.Should().BeEmpty($"validation after compile (index {i}):\n{string.Join("\n", issues)}");

                byte[] bytes1 = NCSAuto.BytesNcs(compiled);
                bytes1.Should().NotBeNull($"BytesNcs null for index {i}");
                bytes1.Length.Should().BeGreaterThan(0, $"BytesNcs empty for index {i}");

                NCS readBack = NCSAuto.ReadNcs(bytes1);
                readBack.Should().NotBeNull($"ReadNcs null for index {i}");

                List<string> issuesAfterRead = readBack.Validate();
                issuesAfterRead.Should().BeEmpty($"validation after ReadNcs (index {i}):\n{string.Join("\n", issuesAfterRead)}");

                byte[] bytes2 = NCSAuto.BytesNcs(readBack);
                bytes2.Should().Equal(bytes1, $"binary roundtrip mismatch for index {i}");
            }
        }

        [Theory]
        [InlineData(Game.K1)]
        [InlineData(Game.TSL)]
        public void CompileReadWrite_DecodeTokenStream_Unchanged(Game game)
        {
            bool tsl = game.IsK2();
            ActionsData actions = ActionsData.LoadFromEmbedded(tsl);
            for (int i = 0; i < RoundtripScripts.Length; i++)
            {
                string source = RoundtripScripts[i];
                NCS compiled = NCSAuto.CompileNss(source, game, null, null, null);
                byte[] bytes1 = NCSAuto.BytesNcs(compiled);
                byte[] bytes2 = NCSAuto.BytesNcs(NCSAuto.ReadNcs(bytes1));

                string tok1 = NcsParsePipeline.DecodeToTokenStream(bytes1, actions);
                string tok2 = NcsParsePipeline.DecodeToTokenStream(bytes2, actions);
                tok2.Should().Be(tok1, $"decoder text must match after binary roundtrip (index {i})");
            }
        }

        [Theory]
        [InlineData(Game.K1)]
        [InlineData(Game.TSL)]
        public void AndastraStyleSimpleScript_ManagedCompile_DecoderMatchesRecompile(Game game)
        {
            const string source = @"
void main()
{
    int value = 41;
    value = value + 1;
}
";
            NCS compiled = NCSAuto.CompileNss(source, game, null, null, null);
            compiled.Should().NotBeNull();
            byte[] ncsBytes = NCSAuto.BytesNcs(compiled);
            ManagedRoundTripCompareResult r = RoundTripUtil.CompareManagedRecompileToOriginalDecoderText(
                ncsBytes,
                source,
                k2: game.IsK2(),
                k1NwscriptPath: null,
                k2NwscriptPath: null);

            Assert.True(r.CompileSucceeded, r.Summary);
            Assert.True(r.DecoderOutputsMatch, r.Summary);
        }
    }
}
