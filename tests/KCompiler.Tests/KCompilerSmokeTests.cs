using KPatcher.Core.Common;
using Xunit;

namespace KCompiler.Tests
{
    public sealed class KCompilerSmokeTests
    {
        [Fact]
        public void Referenced_shared_core_types_resolve()
        {
            Assert.True(Game.K1.IsK1());
            Assert.False(Game.K2.IsK1());
        }
    }
}
