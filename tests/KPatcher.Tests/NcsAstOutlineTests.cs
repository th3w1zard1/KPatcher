using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;
using Xunit;

namespace KPatcher.Tests
{
    /// <summary>
    /// <see cref="NcsAstOutline"/> — reflection-based AST outline for UI / tooling.
    /// </summary>
    public sealed class NcsAstOutlineTests
    {
        [Fact]
        public void Build_NullRoot_ReturnsNullPlaceholder()
        {
            AstOutlineNode n = NcsAstOutline.Build(null);
            Assert.Equal("(null)", n.Label);
            Assert.Empty(n.Children);
        }

        [Fact]
        public void Build_EmptyStart_NoChildNodes()
        {
            var start = new Start();
            AstOutlineNode outline = NcsAstOutline.Build(start);
            Assert.Equal("Start", outline.Label);
            Assert.Empty(outline.Children);
        }

        [Fact]
        public void Build_StartWithEof_ListsEofChild()
        {
            var start = new Start();
            start.SetEOF(new EOF());
            AstOutlineNode outline = NcsAstOutline.Build(start);

            Assert.Equal("Start", outline.Label);
            Assert.Single(outline.Children);
            Assert.Equal("EOF", outline.Children[0].Label);
        }

    }
}
