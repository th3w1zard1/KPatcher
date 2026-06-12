using System;
using System.IO;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Common.LZMA;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    public sealed class LzmaHelperTests
    {
        [Fact]
        public void CompressThenDecompress_RoundTripsPayload()
        {
            byte[] original = Encoding.ASCII.GetBytes("kotor-bzf-lzma-roundtrip");

            byte[] compressed = LzmaHelper.Compress(original);
            compressed.Should().NotBeEmpty();

            byte[] roundTrip = LzmaHelper.Decompress(compressed, original.Length);
            roundTrip.Should().Equal(original);
        }

        [Fact]
        public void BzfWholeFileWrapper_RoundTripsBifPayload()
        {
            byte[] bifPayload = Encoding.ASCII.GetBytes("BIFFV1  minimal-bif-payload-for-bzf");

            byte[] wrapped = BzfHelper.WrapWholeFile(bifPayload);
            BzfHelper.IsWholeFileWrapper(wrapped).Should().BeTrue();

            byte[] roundTrip = BzfHelper.DecompressWholeFile(wrapped);
            roundTrip.Should().Equal(bifPayload);
        }

        [Fact]
        public void Decompress_WithWrongUncompressedSize_Throws()
        {
            byte[] compressed = LzmaHelper.Compress(Encoding.ASCII.GetBytes("size-check"));
            Action act = () => LzmaHelper.Decompress(compressed, 1);
            act.Should().Throw<Exception>();
        }
    }
}
