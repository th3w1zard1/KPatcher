using System;
using System.IO;

namespace KPatcher.Core.Common
{
    /// <summary>
    /// Resolves config file paths with optional language suffix for localized config files
    /// (e.g. changes.de.ini, namespaces.fr.ini). Used by the UI layer with the current language code.
    /// See docs/LOCALIZED_CONFIG_FILES.md.
    /// </summary>
    public static class LocalizedConfigResolver
    {
        /// <summary>
        /// Resolves a config file with optional language suffix: tries &lt;base&gt;.&lt;lang&gt;.yaml,
        /// &lt;base&gt;.&lt;lang&gt;.ini (if tryYaml), then &lt;base&gt;.yaml, &lt;base&gt;.ini (if tryYaml).
        /// YAML takes priority over INI when both exist.
        /// </summary>
        /// <param name="directory">Directory to look in (case-aware on Unix).</param>
        /// <param name="baseName">Base filename without extension (e.g. "namespaces", "changes").</param>
        /// <param name="lang">Two-letter language code (e.g. "de", "en").</param>
        /// <param name="tryYaml">If true, also try .yaml variants; if false, only .ini.</param>
        /// <returns>Full path and chosen filename, or (null, null) if none exist.</returns>
        public static (string FullPath, string FileName) Resolve(CaseAwarePath directory, string baseName, string lang, bool tryYaml)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                return (null, null);
            }

            string[] candidates = tryYaml
                ? new[]
                {
                    $"{baseName}.{lang}.yaml",
                    $"{baseName}.{lang}.ini",
                    $"{baseName}.yaml",
                    $"{baseName}.ini"
                }
                : new[]
                {
                    $"{baseName}.{lang}.ini",
                    $"{baseName}.ini"
                };

            foreach (string fileName in candidates)
            {
                var path = directory.Combine(fileName);
                if (path.IsFile())
                {
                    return (path.GetResolvedPath(), fileName);
                }
            }

            return (null, null);
        }
    }
}
