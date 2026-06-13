using Xunit;

namespace KEditChanges.Tests
{
    public sealed class TstIniEscapesTests
    {
        [Fact]
        public void Encode_multiline_uses_lf_token()
        {
            string encoded = KEditChanges.Ini.TstIniEscapes.Encode("line1\nline2");
            Assert.Equal("line1<#LF#>line2", encoded);
        }

        [Fact]
        public void Decode_round_trip()
        {
            string original = "A<#LF#>B<#CR#>C";
            string decoded = KEditChanges.Ini.TstIniEscapes.Decode(original);
            Assert.Equal("A\nB\rC", decoded);
            Assert.Equal(original, KEditChanges.Ini.TstIniEscapes.Encode(decoded));
        }
    }
}
