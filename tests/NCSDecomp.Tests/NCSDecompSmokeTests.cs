using NCSDecomp.Core.ScriptNode;
using Xunit;

namespace NCSDecomp.Tests
{
    public sealed class NCSDecompSmokeTests
    {
        [Fact]
        public void ScriptNode_types_are_public_and_loadable()
        {
            Assert.NotNull(typeof(AIf));
        }
    }
}
