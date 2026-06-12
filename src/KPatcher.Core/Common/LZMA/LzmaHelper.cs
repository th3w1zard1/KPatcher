using System;
using System.IO;
using SharpCompress.Compressors.LZMA;

namespace KPatcher.Core.Common.LZMA
{
    /// <summary>
    /// Raw LZMA1 helpers for iOS BZF chitin archives (lc=3, lp=0, pb=2, 8MB dictionary).
    /// </summary>
    internal static class LzmaHelper
    {
        // LZMA properties for raw LZMA1 format: lc=3, lp=0, pb=2, dict=8MB
        private static readonly byte[] LzmaProperties = { 0x5D, 0x00, 0x00, 0x80, 0x00 };

        public static byte[] Decompress(byte[] compressedData, int uncompressedSize)
        {
            if (compressedData == null)
            {
                throw new ArgumentNullException(nameof(compressedData));
            }

            if (compressedData.Length == 0)
            {
                throw new ArgumentException("Compressed data cannot be empty.", nameof(compressedData));
            }

            using (var input = new MemoryStream(compressedData, writable: false))
            using (var output = new MemoryStream(uncompressedSize > 0 ? uncompressedSize : 64))
            {
                Stream lzma = uncompressedSize > 0
                    ? new LzmaStream(LzmaProperties, input, compressedData.Length, uncompressedSize)
                    : new LzmaStream(LzmaProperties, input, compressedData.Length);

                using (lzma)
                {
                    lzma.CopyTo(output);
                }

                byte[] result = output.ToArray();
                if (uncompressedSize > 0 && result.Length != uncompressedSize)
                {
                    throw new InvalidDataException(
                        string.Format(
                            "LZMA decompression size mismatch: expected {0} bytes, got {1}.",
                            uncompressedSize,
                            result.Length));
                }

                return result;
            }
        }

        public static byte[] Compress(byte[] uncompressedData)
        {
            if (uncompressedData == null)
            {
                throw new ArgumentNullException(nameof(uncompressedData));
            }

            using (var output = new MemoryStream())
            {
                // lc=3, lp=0, pb=2, dictionary=8MB — matches LzmaProperties used for KotOR BZF payloads.
                var encoderProperties = new LzmaEncoderProperties(true, 1 << 23);
                LzmaStream lzma = new LzmaStream(encoderProperties, false, output);
                try
                {
                    lzma.Write(uncompressedData, 0, uncompressedData.Length);
                    lzma.Flush();
                }
                finally
                {
                    lzma.Close();
                }

                return output.ToArray();
            }
        }
    }
}
