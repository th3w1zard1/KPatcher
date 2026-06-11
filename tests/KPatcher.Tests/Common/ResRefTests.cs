using FluentAssertions;
using KPatcher.Core.Common;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    public sealed class ResRefTests
    {
        [Theory]
        [InlineData("valid_name01", "valid_name01")]
        [InlineData("test/sound.wav", "testsoundwav")]
        [InlineData("abc:def|ghi", "abcdefghi")]
        [InlineData("MoreThan16CharactersLong", "MoreThan16Charac")]
        [InlineData("", "")]
        public void FromTslPatcherIni_FiltersInvalidCharactersAndTruncates(string input, string expected)
        {
            ResRef resRef = ResRef.FromTslPatcherIni(input);
            resRef.ToString().Should().Be(expected);
        }

        [Fact]
        public void FromTslPatcherIni_DoesNotThrowOnInvalidIniText()
        {
            ResRef resRef = ResRef.FromTslPatcherIni("bad<>chars");
            resRef.ToString().Should().Be("badchars");
        }
    }
}
