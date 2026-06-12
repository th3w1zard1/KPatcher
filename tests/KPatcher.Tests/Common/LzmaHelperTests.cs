using System;
using FluentAssertions;
using KPatcher.Core.Common.LZMA;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    public sealed class LzmaHelperTests
    {
        [Fact]
        public void Decompress_ThrowsNotImplemented_WithDocumentedMessage()
        {
            Action act = () => LzmaHelper.Decompress(new byte[] { 0 }, 1);
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*LZMA decompression is not yet implemented*");
        }

        [Fact]
        public void Compress_ThrowsNotImplemented_WithDocumentedMessage()
        {
            Action act = () => LzmaHelper.Compress(new byte[] { 0 });
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*LZMA compression is not yet implemented*");
        }
    }
}
