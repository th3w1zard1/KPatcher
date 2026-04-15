using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Decompiler;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Full managed NCS->NSS (<see cref="NCSManagedDecompiler"/> / DeNCS <c>FileDecompiler</c> parse path).
    /// Kept narrow while parser parity is validated against <c>vendor/DeNCS</c>.
    /// </summary>
    public sealed class NcsManagedFullDecompileSmokeTests
    {
        [Fact]
        public void ParseAst_TestNcs_WhenCopiedToOutput_Succeeds()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "test_files", "test.ncs");
            if (!File.Exists(path))
                return;

            byte[] bytes = File.ReadAllBytes(path);
            Start ast = NcsParsePipeline.ParseAst(bytes, ActionsData.LoadFromEmbedded(false));
            ast.Should().NotBeNull();
        }

        [Theory]
        [InlineData(Game.K1)]
        [InlineData(Game.TSL)]
        public void DecompileToNss_VoidMain_ProducesNonEmptyNss(Game game)
        {
            const string nss = "void main() { }";
            NCS ncs = NCSAuto.CompileNss(nss, game, null, null, null);
            string @out = NCSManagedDecompiler.DecompileToNss(ncs, game.IsK2());
            @out.Should().NotBeNullOrWhiteSpace();
            @out.Should().Contain("main", "decompiled NSS should reference main");
        }

        [Theory]
        [InlineData(Game.K1)]
        [InlineData(Game.TSL)]
        public void DecompileToNss_SimpleArithmetic_Recompiles(Game game)
        {
            const string source = @"
void main()
{
    int value = 41;
    value = value + 1;
}
";
            NCS original = NCSAuto.CompileNss(source, game, null, null, null);
            string decompiled = NCSManagedDecompiler.DecompileToNss(original, game.IsK2());
            decompiled.Should().NotBeNullOrWhiteSpace();

            NCS round = NCSAuto.CompileNss(decompiled, game, null, null, null);
            round.Should().NotBeNull();
            round.Instructions.Count.Should().BeGreaterThan(0);
        }
    }
}
