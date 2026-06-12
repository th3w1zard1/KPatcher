using System;
using System.IO;
using System.Text;

namespace KPatcher.Core.Common.LZMA
{
    /// <summary>
    /// Helpers for BioWare BZF wrappers (whole-file LZMA-compressed BIF payloads).
    /// </summary>
    internal static class BzfHelper
    {
        public const int WholeFileHeaderLength = 8;

        public static bool IsWholeFileWrapper(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < WholeFileHeaderLength)
            {
                return false;
            }

            return fileBytes[0] == (byte)'B'
                && fileBytes[1] == (byte)'Z'
                && fileBytes[2] == (byte)'F'
                && fileBytes[3] == (byte)' '
                && fileBytes[4] == (byte)'V'
                && fileBytes[5] == (byte)'1'
                && fileBytes[6] == (byte)'.'
                && fileBytes[7] == (byte)'0';
        }

        public static byte[] WrapWholeFile(byte[] bifBytes)
        {
            if (bifBytes == null)
            {
                throw new ArgumentNullException(nameof(bifBytes));
            }

            byte[] compressed = LzmaHelper.Compress(bifBytes);
            byte[] wrapped = new byte[WholeFileHeaderLength + compressed.Length];
            Encoding.ASCII.GetBytes("BZF ").CopyTo(wrapped, 0);
            Encoding.ASCII.GetBytes("V1.0").CopyTo(wrapped, 4);
            Array.Copy(compressed, 0, wrapped, WholeFileHeaderLength, compressed.Length);
            return wrapped;
        }

        public static byte[] DecompressWholeFile(byte[] bzfBytes)
        {
            if (bzfBytes == null)
            {
                throw new ArgumentNullException(nameof(bzfBytes));
            }

            if (!IsWholeFileWrapper(bzfBytes))
            {
                throw new InvalidDataException("Not a whole-file BZF wrapper (expected 'BZF ' + 'V1.0' header).");
            }

            int payloadLength = bzfBytes.Length - WholeFileHeaderLength;
            if (payloadLength <= 0)
            {
                throw new InvalidDataException("BZF wrapper contains no LZMA payload.");
            }

            byte[] compressed = new byte[payloadLength];
            Array.Copy(bzfBytes, WholeFileHeaderLength, compressed, 0, payloadLength);
            return LzmaHelper.Decompress(compressed, 0);
        }
    }
}
