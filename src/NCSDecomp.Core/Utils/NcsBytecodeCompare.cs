// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// NCS bytecode equality / first-difference reporting (round-trip and regression tooling).

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Compare two NCS payloads byte-for-byte (or up to a maximum length).
    /// </summary>
    public static class NcsBytecodeCompare
    {
        /// <summary>Index of first differing byte, or -1 if equal within compared range.</summary>
        public static int FirstMismatchIndex(byte[] a, byte[] b, int maxBytesToCompare = int.MaxValue)
        {
            if (a == null)
            {
                a = Array.Empty<byte>();
            }

            if (b == null)
            {
                b = Array.Empty<byte>();
            }

            int cap = maxBytesToCompare;
            if (cap < 0)
            {
                cap = int.MaxValue;
            }

            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int compareCount = minLen < cap ? minLen : cap;
            for (int i = 0; i < compareCount; i++)
            {
                if (a[i] != b[i])
                {
                    return i;
                }
            }

            if (a.Length != b.Length && compareCount == minLen)
            {
                return minLen;
            }

            return -1;
        }

        public static bool AreEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            return FirstMismatchIndex(a, b, a.Length) < 0;
        }

        public static int FirstMismatchIndexFromFiles(string pathA, string pathB, int maxBytesToCompare = int.MaxValue)
        {
            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
            {
                throw new ArgumentException("Paths required.");
            }

            byte[] a = File.ReadAllBytes(pathA);
            byte[] b = File.ReadAllBytes(pathB);
            return FirstMismatchIndex(a, b, maxBytesToCompare);
        }

        /// <summary>Human-readable summary for logs/UI.</summary>
        public static string DescribeFirstDifference(byte[] a, byte[] b, int contextBytes = 8)
        {
            int idx = FirstMismatchIndex(a, b);
            if (idx < 0)
            {
                return "NCS byte streams are identical.";
            }

            if (a == null)
            {
                a = Array.Empty<byte>();
            }

            if (b == null)
            {
                b = Array.Empty<byte>();
            }

            var sb = new StringBuilder();
            sb.Append("First mismatch at offset ").Append(idx.ToString(CultureInfo.InvariantCulture));
            sb.Append(" (lengths ").Append(a.Length.ToString(CultureInfo.InvariantCulture));
            sb.Append(" vs ").Append(b.Length.ToString(CultureInfo.InvariantCulture)).Append("). ");
            if (idx < a.Length && idx < b.Length)
            {
                sb.Append("a[").Append(idx.ToString(CultureInfo.InvariantCulture)).Append("]=0x");
                sb.Append(a[idx].ToString("X2", CultureInfo.InvariantCulture));
                sb.Append(" b[").Append(idx.ToString(CultureInfo.InvariantCulture)).Append("]=0x");
                sb.Append(b[idx].ToString("X2", CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("Stream ended in one file before the other.");
            }

            sb.Append(" Context: ");
            AppendHexSnippet(sb, a, idx, contextBytes);
            sb.Append(" | ");
            AppendHexSnippet(sb, b, idx, contextBytes);
            return sb.ToString();
        }

        private static void AppendHexSnippet(StringBuilder sb, byte[] data, int center, int radius)
        {
            if (data == null || data.Length == 0)
            {
                sb.Append("<empty>");
                return;
            }

            int start = center - radius;
            if (start < 0)
            {
                start = 0;
            }

            int end = center + radius;
            if (end > data.Length)
            {
                end = data.Length;
            }

            for (int i = start; i < end; i++)
            {
                if (i > start)
                {
                    sb.Append(' ');
                }

                if (i == center)
                {
                    sb.Append('[');
                }

                sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));
                if (i == center)
                {
                    sb.Append(']');
                }
            }
        }
    }
}
