using System.Collections.Generic;
using System.IO;
using KPatcher.Core.Tests.Common;
using NCSDecomp.Core;
using NCSDecomp.Core.Utils;
using Xunit;

namespace KPatcher.Tests
{
    /// <summary>
    /// NWScript / decoder token segmentation for NCSDecomp UI (no Avalonia).
    /// </summary>
    public sealed class NCSDecompSyntaxHighlighterTests
    {
        [Fact]
        public void NwScriptSegment_Empty_ReturnsEmpty()
        {
            Assert.Empty(NwScriptSyntaxHighlighter.Segment(string.Empty));
        }

        [Fact]
        public void NwScriptSegment_LineComment_IsComment()
        {
            IReadOnlyList<NwScriptHighlightedSegment> segs = NwScriptSyntaxHighlighter.Segment("// line");
            Assert.Contains(segs, s => s.Kind == NwScriptHighlightKind.Comment && s.Text.Contains("//"));
        }

        [Fact]
        public void NwScriptSegment_DoubleQuotedString_IsString()
        {
            IReadOnlyList<NwScriptHighlightedSegment> segs = NwScriptSyntaxHighlighter.Segment("\"ab\"");
            Assert.Contains(segs, s => s.Kind == NwScriptHighlightKind.String && s.Text.Contains('"'));
        }

        [Fact]
        public void NwScriptSegment_VoidMain_HighlightsVoidAndMain()
        {
            IReadOnlyList<NwScriptHighlightedSegment> segs = NwScriptSyntaxHighlighter.Segment("void main() { }");
            Assert.Contains(
                segs,
                s => s.Text.Contains("void")
                     && (s.Kind == NwScriptHighlightKind.Keyword || s.Kind == NwScriptHighlightKind.Type));
            Assert.Contains(segs, s => s.Kind == NwScriptHighlightKind.Function && s.Text.Contains("main"));
        }

        [Fact]
        public void NcsBytecodeSegment_DecodedFixtureNcs_MarksInstructions()
        {
            byte[] ncs = CompiledNcsTestFixture.ReferenceK1NcsBytes();
            ActionsData actions = ActionsData.LoadForGame(tsl: false, k1Path: null, k2Path: null);
            string tokens = NcsParsePipeline.DecodeToTokenStream(ncs, actions);

            IReadOnlyList<NcsBytecodeHighlightedSegment> segs = NcsBytecodeSyntaxHighlighter.Segment(tokens);
            Assert.NotEmpty(segs);
            Assert.Contains(segs, s => s.Kind == NcsBytecodeHighlightKind.Instruction);
        }
    }
}
