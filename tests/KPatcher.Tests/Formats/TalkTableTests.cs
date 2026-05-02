using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Tests.Common;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for TalkTable (read-only TLK accessor).
    /// </summary>
    public sealed class TalkTableTests : IDisposable
    {
        private readonly string _tlkPath;
        private readonly TalkTable _talkTable;

        public TalkTableTests()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            _tlkPath = Path.Combine(Path.GetTempPath(), "kp_talktable_" + Guid.NewGuid().ToString("N") + ".tlk");
            BinaryFormatFixtures.BuildTalkTableFixtureTlk().Save(_tlkPath);
            _talkTable = new TalkTable(_tlkPath);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tlkPath))
                {
                    File.Delete(_tlkPath);
                }
            }
            catch
            {
            }
        }

        [Fact]
        public void TestStringsAndResrefsAreCorrect()
        {
            _talkTable.GetString(0).Should().Be("abcdef");
            _talkTable.GetSound(0).ToString().Should().Be("resref01");

            _talkTable.GetString(1).Should().Be("ghijklmnop");
            _talkTable.GetSound(1).ToString().Should().Be("resref02");

            _talkTable.GetString(2).Should().Be("qrstuvwxyz");
            _talkTable.GetSound(2).ToString().Should().Be("");
        }

        [Fact]
        public void TestLanguageIsCorrect()
        {
            _talkTable.GetLanguage().Should().Be(Language.English);
        }

        [Fact]
        public void TestSizeIsCorrect()
        {
            _talkTable.Size().Should().Be(3);
        }
    }
}
