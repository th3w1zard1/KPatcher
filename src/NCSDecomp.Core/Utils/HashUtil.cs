// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Cryptographic file hashes (DeNCS HashUtil.java).
    /// Used to fingerprint external compiler binaries when integrating tooling.
    /// </summary>
    public static class HashUtil
    {
        /// <summary>
        /// SHA-256 of file contents as uppercase hexadecimal (no delimiters).
        /// </summary>
        /// <exception cref="IOException">Read failure or algorithm unavailable.</exception>
        public static string CalculateSha256(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using (var stream = File.OpenRead(filePath))
            {
                return CalculateSha256(stream);
            }
        }

        /// <summary>
        /// SHA-256 of stream contents as uppercase hexadecimal (no delimiters).
        /// </summary>
        public static string CalculateSha256(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                return BytesToHex(hash).ToUpperInvariant();
            }
        }

        private static string BytesToHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
