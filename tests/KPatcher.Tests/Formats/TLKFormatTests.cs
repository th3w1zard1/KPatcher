using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Resources;
using KPatcher.Core.Tests.Common;
using Xunit;
using static global::KPatcher.Core.Formats.TLK.TLKAuto;
using TlkFile = KPatcher.Core.Formats.TLK.TLK;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for TLK binary I/O operations.
    /// 1:1 port from tests/resource/formats/test_tlk.py
    /// </summary>
    public class TLKFormatTests
    {
        private static TlkFile BuildBaseTlk()
        {
            var baseTlk = new TlkFile(Language.English);
            baseTlk.Add("abcdef", "resref01");
            baseTlk.Add("ghijklmnop", "resref02");
            baseTlk.Add("qrstuvwxyz", "");
            return baseTlk;
        }

        [Fact]
        public void TestBinaryIO()
        {
            TlkFile baseTlk = BuildBaseTlk();
            var writer = new TLKBinaryWriter(baseTlk);
            byte[] binaryTestData = writer.Write();

            var reader = new TLKBinaryReader(binaryTestData);
            TlkFile tlk = reader.Load();
            ValidateIO(tlk);

            var writer2 = new TLKBinaryWriter(tlk);
            byte[] data = writer2.Write();

            var newReader = new TLKBinaryReader(data);
            TlkFile newTlk = newReader.Load();
            ValidateIO(newTlk);
        }

        private static void ValidateIO(TlkFile tlk)
        {
            tlk.Language.Should().Be(Language.English);
            tlk.Count.Should().Be(3);

            tlk[0].Text.Should().Be("abcdef");
            tlk[0].Voiceover.ToString().Should().Be("resref01");

            tlk[1].Text.Should().Be("ghijklmnop");
            tlk[1].Voiceover.ToString().Should().Be("resref02");

            tlk[2].Text.Should().Be("qrstuvwxyz");
            tlk[2].Voiceover.ToString().Should().Be("");
        }

        [Fact]
        public void TestResize()
        {
            TlkFile baseTlk = BuildBaseTlk();

            var writer = new TLKBinaryWriter(baseTlk);
            byte[] binaryTestData = writer.Write();

            var reader = new TLKBinaryReader(binaryTestData);
            TlkFile tlk = reader.Load();
            tlk.Count.Should().Be(3);
            tlk.Resize(4);
            tlk.Count.Should().Be(4);
        }

        [Fact]
        public void TestReadRaises()
        {
            Action act1 = () => new TLKBinaryReader(".").Load();
            act1.Should().Throw<UnauthorizedAccessException>();

            Action act2 = () => new TLKBinaryReader("./thisfiledoesnotexist").Load();
            act2.Should().Throw<FileNotFoundException>();

            byte[] corrupt = FormatCorruptBinarySamples.CorruptTlk;
            Action act3 = () => new TLKBinaryReader(corrupt).Load();
            act3.Should().Throw<InvalidDataException>().WithMessage("Attempted to load an invalid TLK file.");
        }

        [Fact]
        public void TestWriteRaises()
        {
            var tlk = new TlkFile(Language.English);

            Action act1 = () => WriteTlk(tlk, ".", ResourceType.TLK);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => WriteTlk(tlk, ".", ResourceType.INVALID);
            act2.Should().Throw<ArgumentException>().WithMessage("*Unsupported format*");
        }
    }
}
