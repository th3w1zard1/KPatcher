using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Logger;

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
        public static (string FullPath, string FileName) Resolve(
            CaseAwarePath directory,
            string baseName,
            string lang,
            bool tryYaml,
            [CanBeNull] PatchLogger logger = null)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                logger?.AddDiagnostic("LocalizedConfigResolver.Resolve: empty baseName; returning (null,null)");
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

            logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "LocalizedConfigResolver.Resolve: dir={0} baseName={1} lang={2} tryYaml={3} candidateCount={4}",
                directory.GetResolvedPath(), baseName, lang, tryYaml, candidates.Length));

            foreach (string fileName in candidates)
            {
                var path = directory.Combine(fileName);
                bool hit = path.IsFile();
                logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "LocalizedConfigResolver.Resolve: candidate={0} isFile={1}", fileName, hit));
                if (hit)
                {
                    string full = path.GetResolvedPath();
                    logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "LocalizedConfigResolver.Resolve: selected path={0}", full));
                    return (full, fileName);
                }
            }

            logger?.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "LocalizedConfigResolver.Resolve: no file matched in dir={0} for baseName={1}", directory.GetResolvedPath(), baseName));
            return (null, null);
        }
    }
}
