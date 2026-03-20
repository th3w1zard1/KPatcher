using System;
using System.Globalization;
using System.IO;

namespace KPatcher.UI
{
    /// <summary>
    /// Persists and reads the user's UI language preference so it can be applied at startup and when changed from the Language menu.
    /// Also provides the two-letter language code used for localized config file resolution (e.g. changes.de.ini, namespaces.fr.ini).
    /// </summary>
    public static class LanguageSettings
    {
        private static readonly string[] SupportedCodes = { "en", "es", "de", "fr", "ru", "pl" };

        private static string GetLanguageFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "KPatcher");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "language.txt");
        }

        /// <summary>
        /// Gets the saved language code (e.g. "en", "es"), or null if none saved or invalid.
        /// </summary>
        public static string GetSavedLanguage()
        {
            try
            {
                string path = GetLanguageFilePath();
                if (!File.Exists(path))
                {
                    return null;
                }
                string code = File.ReadAllText(path)?.Trim()?.ToLowerInvariant();
                if (string.IsNullOrEmpty(code) || Array.IndexOf(SupportedCodes, code) < 0)
                {
                    return null;
                }
                return code;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves the language code (e.g. "en", "es"). Must be one of the supported codes.
        /// </summary>
        public static void SaveLanguage(string twoLetterCode)
        {
            if (twoLetterCode == null || Array.IndexOf(SupportedCodes, twoLetterCode.ToLowerInvariant()) < 0)
            {
                return;
            }
            try
            {
                File.WriteAllText(GetLanguageFilePath(), twoLetterCode.ToLowerInvariant());
            }
            catch
            {
                // Best effort; do not crash
            }
        }

        public static string[] GetSupportedCodes() => (string[])SupportedCodes.Clone();

        /// <summary>
        /// Gets the two-letter language code used for resolving localized config files
        /// (e.g. changes.de.ini, namespaces.fr.ini). Uses saved preference, then current UI culture,
        /// normalized to a supported code or "en".
        /// </summary>
        public static string GetConfigLanguageCode()
        {
            string saved = GetSavedLanguage();
            if (!string.IsNullOrEmpty(saved))
            {
                return saved;
            }
            string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName?.ToLowerInvariant() ?? "en";
            return Array.IndexOf(SupportedCodes, twoLetter) >= 0 ? twoLetter : "en";
        }
    }
}
