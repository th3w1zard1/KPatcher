using FluentAssertions;
using KPatcher.Core.Common;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    public class RtfStripperTests
    {
        [Fact]
        public void StripRtf_EmptyOrNull_ReturnsEmpty()
        {
            RtfStripper.StripRtf(null).Should().Be("");
            RtfStripper.StripRtf("").Should().Be("");
        }

        [Fact]
        public void StripRtf_PlainText_ReturnsSame()
        {
            const string plain = "Hello world";
            RtfStripper.StripRtf(plain).Should().Be(plain);
        }

        [Fact]
        public void StripRtf_SimpleRtf_StripsToPlainText()
        {
            // Minimal RTF: {\rtf1\ansi Hello}
            string rtf = "{\\rtf1\\ansi Hello}";
            RtfStripper.StripRtf(rtf).Should().Be("Hello");
        }

        [Fact]
        public void StripRtf_ParControl_BecomesNewline()
        {
            string rtf = "{\\rtf1\\ansi Line1\\par Line2}";
            RtfStripper.StripRtf(rtf).Should().Be("Line1\nLine2");
        }

        [Fact]
        public void StripRtf_UnicodeEscape_Decodes()
        {
            // \u65 = 'A' in RTF (decimal 65)
            string rtf = "{\\rtf1\\ansi \\u65?}";
            RtfStripper.StripRtf(rtf).Should().Be("A");
        }

        [Fact]
        public void StripRtf_HexEscape_Decodes()
        {
            // \'41 = 0x41 = 'A'
            string rtf = "{\\rtf1\\ansi \\'41}";
            RtfStripper.StripRtf(rtf).Should().Be("A");
        }

        [Fact]
        public void StripRtf_TabControl_BecomesTab()
        {
            string rtf = "{\\rtf1\\ansi a\\tab b}";
            RtfStripper.StripRtf(rtf).Should().Be("a\tb");
        }

        [Fact]
        public void StripRtf_Destination_Ignored()
        {
            // \fonttbl is a destination - content until next brace group is ignored
            string rtf = "{\\rtf1\\ansi text {\\fonttbl ignored} more}";
            RtfStripper.StripRtf(rtf).Should().Contain("text").And.Contain("more");
        }

        [Fact]
        public void StripRtf_NestedBraces_HandlesState()
        {
            string rtf = "{\\rtf1 a { b } c }";
            RtfStripper.StripRtf(rtf).Should().Be("a  b  c ");
        }
    }
}
