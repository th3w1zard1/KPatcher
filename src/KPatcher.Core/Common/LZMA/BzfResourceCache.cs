using System;
using System.Collections.Generic;
using System.IO;

namespace KPatcher.Core.Common.LZMA
{
    /// <summary>
    /// Per-process cache for iOS BZF chitin reads (whole-file decompressed BIF bytes and packed LZMA segments).
    /// </summary>
    internal static class BzfResourceCache
    {
        private static readonly Dictionary<string, byte[]> WholeFileDecompressed =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Dictionary<int, PackedSegment>> PackedSegments =
            new Dictionary<string, Dictionary<int, PackedSegment>>(StringComparer.OrdinalIgnoreCase);

        internal sealed class PackedSegment
        {
            public PackedSegment(int packedOffset, int packedSize, int uncompressedSize)
            {
                PackedOffset = packedOffset;
                PackedSize = packedSize;
                UncompressedSize = uncompressedSize;
            }

            public int PackedOffset { get; }
            public int PackedSize { get; }
            public int UncompressedSize { get; }
        }

        public static void Clear()
        {
            WholeFileDecompressed.Clear();
            PackedSegments.Clear();
        }

        public static void RegisterWholeFileDecompressed(string bzfPath, byte[] decompressedBifBytes)
        {
            if (string.IsNullOrEmpty(bzfPath))
            {
                throw new ArgumentNullException(nameof(bzfPath));
            }

            if (decompressedBifBytes == null)
            {
                throw new ArgumentNullException(nameof(decompressedBifBytes));
            }

            WholeFileDecompressed[bzfPath] = decompressedBifBytes;
            PackedSegments.Remove(bzfPath);
        }

        public static bool TryGetWholeFileBytes(string bzfPath, out byte[] bytes)
        {
            if (WholeFileDecompressed.TryGetValue(bzfPath, out byte[] cached))
            {
                bytes = cached;
                return true;
            }

            bytes = null;
            return false;
        }

        public static void RegisterPackedSegment(string bzfPath, int packedOffset, int packedSize, int uncompressedSize)
        {
            if (string.IsNullOrEmpty(bzfPath))
            {
                throw new ArgumentNullException(nameof(bzfPath));
            }

            if (!PackedSegments.TryGetValue(bzfPath, out Dictionary<int, PackedSegment> segments))
            {
                segments = new Dictionary<int, PackedSegment>();
                PackedSegments[bzfPath] = segments;
            }

            segments[packedOffset] = new PackedSegment(packedOffset, packedSize, uncompressedSize);
        }

        public static bool TryGetPackedSegment(string bzfPath, int packedOffset, out PackedSegment segment)
        {
            if (PackedSegments.TryGetValue(bzfPath, out Dictionary<int, PackedSegment> segments)
                && segments.TryGetValue(packedOffset, out segment))
            {
                return true;
            }

            segment = null;
            return false;
        }

        public static byte[] ReadPackedBytes(string bzfPath, PackedSegment segment)
        {
            byte[] packed = new byte[segment.PackedSize];
            using (FileStream fs = File.OpenRead(bzfPath))
            {
                fs.Seek(segment.PackedOffset, SeekOrigin.Begin);
                int read = fs.Read(packed, 0, packed.Length);
                if (read != packed.Length)
                {
                    throw new EndOfStreamException(
                        string.Format(
                            "Expected {0} packed BZF bytes at offset {1} in {2}, read {3}.",
                            segment.PackedSize,
                            segment.PackedOffset,
                            bzfPath,
                            read));
                }
            }

            return packed;
        }
    }
}
