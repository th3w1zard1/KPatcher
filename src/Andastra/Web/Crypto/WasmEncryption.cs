using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Andastra.Web.Crypto
{
    /// <summary>
    /// Provides encryption and decryption capabilities for WASM binaries.
    /// Uses AES-256-GCM for authenticated encryption.
    /// </summary>
    public class WasmEncryption
    {
        private const int KeySize = 32; // 256 bits
        private const int NonceSize = 12; // 96 bits for GCM
        private const int TagSize = 16; // 128 bits authentication tag

        /// <summary>
        /// Encrypts a WASM binary file.
        /// </summary>
        /// <param name="inputPath">Path to the unencrypted WASM file</param>
        /// <param name="outputPath">Path where encrypted WASM will be written</param>
        /// <param name="key">32-byte encryption key</param>
        public static void EncryptWasmFile(string inputPath, string outputPath, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

            byte[] plaintext = File.ReadAllBytes(inputPath);
            byte[] encrypted = EncryptData(plaintext, key);
            File.WriteAllBytes(outputPath, encrypted);
        }

        /// <summary>
        /// Decrypts a WASM binary file.
        /// </summary>
        /// <param name="inputPath">Path to the encrypted WASM file</param>
        /// <param name="outputPath">Path where decrypted WASM will be written</param>
        /// <param name="key">32-byte decryption key</param>
        public static void DecryptWasmFile(string inputPath, string outputPath, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

            byte[] ciphertext = File.ReadAllBytes(inputPath);
            byte[] decrypted = DecryptData(ciphertext, key);
            File.WriteAllBytes(outputPath, decrypted);
        }

        /// <summary>
        /// Encrypts data using AES-256-GCM.
        /// Returns: [nonce (12 bytes)][tag (16 bytes)][ciphertext]
        /// </summary>
        public static byte[] EncryptData(byte[] plaintext, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

            // Generate random nonce
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Allocate output buffer
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            // Encrypt with AES-GCM
            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Combine nonce + tag + ciphertext
            byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

            return result;
        }

        /// <summary>
        /// Decrypts data using AES-256-GCM.
        /// Input format: [nonce (12 bytes)][tag (16 bytes)][ciphertext]
        /// </summary>
        public static byte[] DecryptData(byte[] encrypted, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

            if (encrypted.Length < NonceSize + TagSize)
                throw new ArgumentException("Invalid encrypted data");

            // Extract components
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] ciphertext = new byte[encrypted.Length - NonceSize - TagSize];

            Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encrypted, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encrypted, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

            // Decrypt
            byte[] plaintext = new byte[ciphertext.Length];
            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }

        /// <summary>
        /// Generates a random encryption key.
        /// </summary>
        public static byte[] GenerateKey()
        {
            byte[] key = new byte[KeySize];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        /// <summary>
        /// Generates a key from a master key and additional data.
        /// Used for deriving session-specific keys.
        /// </summary>
        public static byte[] DeriveKey(byte[] masterKey, string additionalData)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(additionalData);
                byte[] combined = new byte[masterKey.Length + data.Length];
                Buffer.BlockCopy(masterKey, 0, combined, 0, masterKey.Length);
                Buffer.BlockCopy(data, 0, combined, masterKey.Length, data.Length);
                return sha256.ComputeHash(combined);
            }
        }
    }
}
