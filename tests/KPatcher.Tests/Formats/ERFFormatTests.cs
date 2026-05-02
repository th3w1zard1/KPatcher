using System;
using System.IO;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Formats.ERF;
using KPatcher.Core.Resources;
using KPatcher.Core.Tests.Common;
using Xunit;
using static global::KPatcher.Core.Formats.ERF.ERFAuto;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for ERF binary I/O operations.
    /// 1:1 port from tests/resource/formats/test_erf.py
    /// </summary>
    public class ERFFormatTests
    {
        private static readonly string DoesNotExistFile = "./thisfiledoesnotexist";

        [Fact]
        public void TestBinaryIO()
        {
            ERF erf = BinaryFormatFixtures.BuildCanonicalErf();
            ValidateIO(erf);

            byte[] data = BytesErf(erf);

            erf = new ERFBinaryReader(data).Load();
            ValidateIO(erf);
        }

        private static void ValidateIO(ERF erf)
        {
            erf.Count.Should().Be(3);
            erf.Get("1", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("abc"));
            erf.Get("2", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("def"));
            erf.Get("3", ResourceType.TXT).Should().Equal(Encoding.ASCII.GetBytes("ghi"));
        }

        [Fact]
        public void TestReadRaises()
        {
            Action act1 = () => ReadErf(".");
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => ReadErf(DoesNotExistFile);
            act2.Should().Throw<FileNotFoundException>();

            byte[] corrupt = FormatCorruptBinarySamples.CorruptErf;
            Action act3 = () => ReadErf(corrupt);
            act3.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TestWriteRaises()
        {
            var erf = new ERF(ERFType.ERF);

            Action act1 = () => WriteErf(erf, ".", ResourceType.ERF);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => WriteErf(erf, ".", ResourceType.INVALID);
            act2.Should().Throw<ArgumentException>().WithMessage("*Unsupported format*");
        }
    }
}
