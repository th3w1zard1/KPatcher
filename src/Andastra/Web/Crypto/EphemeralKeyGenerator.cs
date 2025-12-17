using System;
using System.Security.Cryptography;
using System.Text;

namespace Andastra.Web.Crypto
{
    /// <summary>
    /// Generates ephemeral keys based on client identity and time window.
    /// Keys are session-scoped and time-variant for security.
    /// </summary>
    public class EphemeralKeyGenerator
    {
        private readonly byte[] _masterSecret;
        private readonly int _timeWindowMinutes;

        /// <summary>
        /// Initializes a new instance of the EphemeralKeyGenerator.
        /// </summary>
        /// <param name="masterSecret">Master secret for key derivation (should be stored securely)</param>
        /// <param name="timeWindowMinutes">Time window in minutes for key validity (default: 5)</param>
        public EphemeralKeyGenerator(byte[] masterSecret, int timeWindowMinutes = 5)
        {
            _masterSecret = masterSecret ?? throw new ArgumentNullException(nameof(masterSecret));
            _timeWindowMinutes = timeWindowMinutes;
        }

        /// <summary>
        /// Generates an ephemeral key based on client identity.
        /// </summary>
        /// <param name="clientIp">Client IP address</param>
        /// <param name="userAgent">Client User-Agent string</param>
        /// <returns>32-byte ephemeral key</returns>
        public byte[] GenerateKey(string clientIp, string userAgent)
        {
            if (string.IsNullOrEmpty(clientIp))
                throw new ArgumentNullException(nameof(clientIp));
            if (string.IsNullOrEmpty(userAgent))
                throw new ArgumentNullException(nameof(userAgent));

            // Get current time window
            string timeWindow = GetCurrentTimeWindow();

            // Combine all factors
            string keyMaterial = $"{clientIp}|{userAgent}|{timeWindow}";

            // Derive key using HMAC-SHA256
            using (var hmac = new HMACSHA256(_masterSecret))
            {
                byte[] keyData = Encoding.UTF8.GetBytes(keyMaterial);
                return hmac.ComputeHash(keyData);
            }
        }

        /// <summary>
        /// Validates if a key is still valid for the given client.
        /// Checks current and previous time window to allow for clock skew.
        /// </summary>
        /// <param name="key">Key to validate</param>
        /// <param name="clientIp">Client IP address</param>
        /// <param name="userAgent">Client User-Agent string</param>
        /// <returns>True if key is valid</returns>
        public bool ValidateKey(byte[] key, string clientIp, string userAgent)
        {
            if (key == null || key.Length != 32)
                return false;

            // Check current time window
            byte[] currentKey = GenerateKey(clientIp, userAgent);
            if (ConstantTimeCompare(key, currentKey))
                return true;

            // Check previous time window (allow for clock skew)
            string previousWindow = GetPreviousTimeWindow();
            string keyMaterial = $"{clientIp}|{userAgent}|{previousWindow}";
            
            using (var hmac = new HMACSHA256(_masterSecret))
            {
                byte[] keyData = Encoding.UTF8.GetBytes(keyMaterial);
                byte[] previousKey = hmac.ComputeHash(keyData);
                if (ConstantTimeCompare(key, previousKey))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current time window as a string.
        /// Time is rounded to the nearest time window interval.
        /// </summary>
        private string GetCurrentTimeWindow()
        {
            DateTime now = DateTime.UtcNow;
            long totalMinutes = (long)now.Subtract(DateTime.UnixEpoch).TotalMinutes;
            long windowNumber = totalMinutes / _timeWindowMinutes;
            return windowNumber.ToString();
        }

        /// <summary>
        /// Gets the previous time window to allow for clock skew.
        /// </summary>
        private string GetPreviousTimeWindow()
        {
            DateTime now = DateTime.UtcNow;
            long totalMinutes = (long)now.Subtract(DateTime.UnixEpoch).TotalMinutes;
            long windowNumber = (totalMinutes / _timeWindowMinutes) - 1;
            return windowNumber.ToString();
        }

        /// <summary>
        /// Performs constant-time comparison of two byte arrays.
        /// Prevents timing attacks.
        /// </summary>
        private static bool ConstantTimeCompare(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        /// <summary>
        /// Creates a master secret from a password using PBKDF2.
        /// Should be called once during setup and the result stored securely.
        /// </summary>
        public static byte[] CreateMasterSecret(string password, byte[] salt, int iterations = 100000)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }

        /// <summary>
        /// Generates a random salt for PBKDF2.
        /// </summary>
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }
    }
}
