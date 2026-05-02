using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Resources;
using KPatcher.Core.Tests.Common;
using Xunit;
using static global::KPatcher.Core.Formats.TwoDA.TwoDAAuto;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for 2DA binary I/O.
    /// 1:1 port of Python test_twoda.py from tests/resource/formats/test_twoda.py
    /// </summary>
    public class TwoDAFormatTests
    {
        [Fact]
        public void TestBinaryIO()
        {
            TwoDA twoda = BinaryFormatFixtures.BuildCanonicalTwoDA();
            ValidateIO(twoda);

            byte[] data = new TwoDABinaryWriter(twoda).Write();
            twoda = new TwoDABinaryReader(data).Load();
            ValidateIO(twoda);
        }

        private static void ValidateIO(TwoDA twoda)
        {
            twoda.GetCellString(0, "col1").Should().Be("abc");
            twoda.GetCellString(0, "col2").Should().Be("def");
            twoda.GetCellString(0, "col3").Should().Be("ghi");

            twoda.GetCellString(1, "col1").Should().Be("def");
            twoda.GetCellString(1, "col2").Should().Be("ghi");
            twoda.GetCellString(1, "col3").Should().Be("123");

            twoda.GetCellString(2, "col1").Should().Be("123");
            twoda.GetCellString(2, "col2").Should().Be("");
            twoda.GetCellString(2, "col3").Should().Be("abc");
        }

        [Fact]
        public void TestReadRaises()
        {
            Action act1 = () => new TwoDABinaryReader(".").Load();
            act1.Should().Throw<Exception>();

            Action act2 = () => new TwoDABinaryReader("./thisfiledoesnotexist").Load();
            act2.Should().Throw<FileNotFoundException>();

            byte[] corrupt = FormatCorruptBinarySamples.CorruptTwoDa;
            Action act3 = () => new TwoDABinaryReader(corrupt).Load();
            act3.Should().Throw<IOException>();
        }

        [Fact]
        public void TestWriteRaises()
        {
            var twoda = new TwoDA(new List<string> { "col1", "col2" });

            Action act1 = () => WriteTwoDA(twoda, ".", ResourceType.TwoDA);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => WriteTwoDA(twoda, ".", ResourceType.INVALID);
            act2.Should().Throw<ArgumentException>().WithMessage("*Unsupported format*");
        }

        [Fact]
        public void TestRowMax()
        {
            var twoda = new TwoDA();
            twoda.AddRow("0");
            twoda.AddRow("1");
            twoda.AddRow("2");

            twoda.LabelMax().Should().Be(3);
        }
    }
}
