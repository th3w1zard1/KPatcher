using KEditChanges;
using Xunit;

namespace KEditChanges.Tests
{
    public sealed class ChangesIniSectionCatalogTests
    {
        [Fact]
        public void TryParseIniSectionName_compile_list()
        {
            ChangesIniSectionKind kind;
            Assert.True(ChangesIniSectionCatalog.TryParseIniSectionName("CompileList", out kind));
            Assert.Equal(ChangesIniSectionKind.CompileList, kind);
        }

        [Fact]
        public void ToIniSectionName_round_trip()
        {
            Assert.Equal(
                "HACKList",
                ChangesIniSectionCatalog.ToIniSectionName(ChangesIniSectionKind.HackList));
        }
    }
}
