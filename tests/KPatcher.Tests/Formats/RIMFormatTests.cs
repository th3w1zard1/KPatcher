using System;
using System.IO;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Formats.RIM;
using KPatcher.Core.Resources;
using KPatcher.Core.Tests.Common;
using Xunit;
using static global::KPatcher.Core.Formats.RIM.RIMAuto;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Tests for RIM binary I/O operations.
    /// 1:1 port from tests/resource/formats/test_rim.py
    /// </summary>
    public class RIMFormatTests
    {
        private static readonly string DoesNotExistFile = "./thisfiledoesnotexist";

        [Fact]
        public void TestBinaryIO()
        {
            RIM rim = BinaryFormatFixtures.BuildCanonicalRim();
            ValidateIO(rim);

            byte[] data = BytesRim(rim);

            rim = ReadRim(data);
            ValidateIO(rim);
        }

        [Fact]
        public void WriteRoundTrip_BytesMatchSourceFile()
        {
            byte[] onDisk = new RIMBinaryWriter(BinaryFormatFixtures.BuildCanonicalRim()).Write();
            RIM rim = new RIMBinaryReader(onDisk).Load();
            byte[] serialized = BytesRim(rim);
            serialized.Should().Equal(onDisk);
        }

        private static void ValidateIO(RIM rim)
        {
            rim.Count.Should().Be(3);
            rim.Get("1", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("abc"));
            rim.Get("2", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("def"));
            rim.Get("3", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("ghi"));
        }

        [Fact]
        public void TestReadRaises()
        {
            Action act1 = () => ReadRim(".");
            if (
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows
                )
            )
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => ReadRim(DoesNotExistFile);
            act2.Should().Throw<FileNotFoundException>();

            byte[] corrupt = FormatCorruptBinarySamples.CorruptRim;
            Action act3 = () => ReadRim(corrupt);
            act3.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TestWriteRaises()
        {
            var rim = new RIM();

            Action act1 = () => WriteRim(rim, ".", ResourceType.RIM);
            if (
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows
                )
            )
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => WriteRim(rim, ".", ResourceType.INVALID);
            act2.Should().Throw<ArgumentException>().WithMessage("*Unsupported format*");
        }
    }
}
