// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS Settings.java — persisted preferences (Java Properties-compatible keys/values).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NCSDecomp.Core.Diagnostics;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Loads/saves <c>config/ncsdecomp.conf</c> (legacy <c>dencs.conf</c>) next to the host app.
    /// Property names match the Java GUI for cross-tool config sharing.
    /// </summary>
    public sealed class NcsDecompSettings
    {
        public const string ConfigFileName = "ncsdecomp.conf";
        public const string LegacyConfigFileName = "dencs.conf";

        // Java Settings.java keys (verbatim)
        public const string KeyOutputDirectory = "Output Directory";
        public const string KeyOpenDirectory = "Open Directory";
        public const string KeyGameVariant = "Game Variant";
        public const string KeyPreferSwitches = "Prefer Switches";
        public const string KeyStrictSignatures = "Strict Signatures";
        public const string KeyOverwriteFiles = "Overwrite Files";
        public const string KeyEncoding = "Encoding";
        public const string KeyFileExtension = "File Extension";
        public const string KeyFilenamePrefix = "Filename Prefix";
        public const string KeyFilenameSuffix = "Filename Suffix";
        public const string KeyLinkScrollBars = "Link Scroll Bars";
        public const string KeyK1NwscriptPath = "K1 nwscript Path";
        public const string KeyK2NwscriptPath = "K2 nwscript Path";
        public const string KeyNwnnsscompFolderPath = "nwnnsscomp Folder Path";
        public const string KeyNwnnsscompFilename = "nwnnsscomp Filename";
        public const string KeyNwnnsscompPath = "nwnnsscomp Path";
        public const string KeyPreferNcsdis = "Prefer ncsdis";
        public const string KeyNcsdisPath = "ncsdis Path";

        public string OutputDirectory { get; set; }
        public string OpenDirectory { get; set; }
        public string GameVariant { get; set; }
        public bool PreferSwitches { get; set; }
        public bool StrictSignatures { get; set; }
        public bool OverwriteFiles { get; set; }
        public string EncodingName { get; set; }
        public string FileExtension { get; set; }
        public string FilenamePrefix { get; set; }
        public string FilenameSuffix { get; set; }
        public bool LinkScrollBars { get; set; }
        public string K1NwscriptPath { get; set; }
        public string K2NwscriptPath { get; set; }

        /// <summary>Directory containing nwnnsscomp (Java <c>nwnnsscomp Folder Path</c>).</summary>
        public string NwnnsscompFolderPath { get; set; }

        /// <summary>Compiler executable file name (Java <c>nwnnsscomp Filename</c>).</summary>
        public string NwnnsscompFilename { get; set; }

        /// <summary>Legacy single property: full path to compiler (Java <c>nwnnsscomp Path</c>).</summary>
        public string NwnnsscompPath { get; set; }

        public bool PreferNcsdis { get; set; }

        public string NcsdisPath { get; set; }

        /// <summary>Resolved compiler exe from folder+filename or legacy path, if the file exists.</summary>
        public string GetResolvedCompilerPath()
        {
            return CompilerUtil.GetCompilerPathFromSettings(this);
        }

        /// <summary>Resolved ncsdis.exe path if set and existing.</summary>
        public string GetResolvedNcsdisPath()
        {
            if (string.IsNullOrWhiteSpace(NcsdisPath))
            {
                return null;
            }

            string p = NcsdisPath.Trim();
            return File.Exists(p) ? Path.GetFullPath(p) : null;
        }

        /// <summary>Directory containing the loaded or last-saved config file, or null.</summary>
        public string ConfigLoadedFromPath { get; private set; }

        /// <summary>Best-effort host directory (entry assembly dir, else <see cref="AppContext.BaseDirectory"/>).</summary>
        public static string GetDefaultAppBaseDirectory()
        {
            try
            {
                var entry = System.Reflection.Assembly.GetEntryAssembly();
                if (entry != null && !string.IsNullOrEmpty(entry.Location))
                {
                    string dir = Path.GetDirectoryName(entry.Location);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        return dir;
                    }
                }
            }
            catch
            {
                // ignore
            }

            string baseDir = AppContext.BaseDirectory;
            return string.IsNullOrEmpty(baseDir) ? Environment.CurrentDirectory : baseDir;
        }

        public static string GetConfigDirectory(string appBaseDirectory)
        {
            return Path.Combine(appBaseDirectory ?? GetDefaultAppBaseDirectory(), "config");
        }

        public static NcsDecompSettings CreateDefaults(string appBaseDirectory)
        {
            if (string.IsNullOrEmpty(appBaseDirectory))
            {
                appBaseDirectory = GetDefaultAppBaseDirectory();
            }

            return new NcsDecompSettings
            {
                OutputDirectory = Path.Combine(appBaseDirectory, "ncsdecomp_converted"),
                OpenDirectory = appBaseDirectory,
                GameVariant = "k1",
                PreferSwitches = false,
                StrictSignatures = false,
                OverwriteFiles = false,
                EncodingName = "Windows-1252",
                FileExtension = ".nss",
                FilenamePrefix = string.Empty,
                FilenameSuffix = string.Empty,
                LinkScrollBars = false,
                K1NwscriptPath = string.Empty,
                K2NwscriptPath = string.Empty,
                NwnnsscompFolderPath = Path.Combine(appBaseDirectory, "tools"),
                NwnnsscompFilename = DefaultNwnnsscompFilename(appBaseDirectory),
                NwnnsscompPath = string.Empty,
                PreferNcsdis = false,
                NcsdisPath = string.Empty
            };
        }

        private static string DefaultNwnnsscompFilename(string appBaseDirectory)
        {
            string tools = Path.Combine(appBaseDirectory, "tools");
            for (int i = 0; i < CompilerUtil.CompilerNames.Length; i++)
            {
                string candidate = Path.Combine(tools, CompilerUtil.CompilerNames[i]);
                if (File.Exists(candidate))
                {
                    return CompilerUtil.CompilerNames[i];
                }
            }

            return "nwnnsscomp_kscript.exe";
        }

        /// <summary>Load from <c>config/ncsdecomp.conf</c> or legacy <c>dencs.conf</c>. On failure, returns defaults.</summary>
        public static NcsDecompSettings Load(string appBaseDirectory, bool createDefaultConfigIfMissing, ILogger log = null)
        {
            ILogger logger = log ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional() ?? string.Empty;
            var swLoad = Stopwatch.StartNew();
            if (string.IsNullOrEmpty(appBaseDirectory))
            {
                appBaseDirectory = GetDefaultAppBaseDirectory();
            }

            string configDir = GetConfigDirectory(appBaseDirectory);
            try
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Message=config directory create failed Path={Path}",
                    DecompPhaseNames.ConfigLoad,
                    cid,
                    ToolPathRedaction.FormatPath(configDir));
            }

            string primary = Path.Combine(configDir, ConfigFileName);
            string legacy = Path.Combine(configDir, LegacyConfigFileName);
            string pathToLoad = File.Exists(primary) ? primary : (File.Exists(legacy) ? legacy : null);

            NcsDecompSettings defaults = CreateDefaults(appBaseDirectory);
            if (pathToLoad == null)
            {
                if (createDefaultConfigIfMissing)
                {
                    try
                    {
                        defaults.Save(appBaseDirectory, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Message=initial Save failed (non-fatal)",
                            DecompPhaseNames.ConfigLoad,
                            cid);
                    }
                }

                defaults.ConfigLoadedFromPath = null;
                defaults.ApplyToRuntime();
                swLoad.Stop();
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Source=defaults ElapsedMs={ElapsedMs} ConfigDir={Dir}",
                        DecompPhaseNames.ConfigLoad,
                        cid,
                        swLoad.ElapsedMilliseconds,
                        ToolPathRedaction.FormatPath(configDir));
                }

                return defaults;
            }

            NcsDecompSettings s = CreateDefaults(appBaseDirectory);
            s.ConfigLoadedFromPath = pathToLoad;
            try
            {
                Dictionary<string, string> map = ReadPropertiesFile(pathToLoad);
                s.OutputDirectory = GetMap(map, KeyOutputDirectory, s.OutputDirectory);
                s.OpenDirectory = GetMap(map, KeyOpenDirectory, s.OpenDirectory);
                s.GameVariant = GetMap(map, KeyGameVariant, s.GameVariant);
                s.PreferSwitches = ParseBool(GetMap(map, KeyPreferSwitches, "false"), false);
                s.StrictSignatures = ParseBool(GetMap(map, KeyStrictSignatures, "false"), false);
                s.OverwriteFiles = ParseBool(GetMap(map, KeyOverwriteFiles, "false"), false);
                s.EncodingName = GetMap(map, KeyEncoding, s.EncodingName);
                s.FileExtension = GetMap(map, KeyFileExtension, s.FileExtension);
                s.FilenamePrefix = GetMap(map, KeyFilenamePrefix, s.FilenamePrefix);
                s.FilenameSuffix = GetMap(map, KeyFilenameSuffix, s.FilenameSuffix);
                s.LinkScrollBars = ParseBool(GetMap(map, KeyLinkScrollBars, "false"), false);
                s.K1NwscriptPath = GetMap(map, KeyK1NwscriptPath, string.Empty);
                s.K2NwscriptPath = GetMap(map, KeyK2NwscriptPath, string.Empty);
                s.PreferNcsdis = ParseBool(GetMap(map, KeyPreferNcsdis, "false"), false);
                s.NcsdisPath = GetMap(map, KeyNcsdisPath, string.Empty);

                string folderPath = GetMap(map, KeyNwnnsscompFolderPath, string.Empty);
                string nwnFilename = GetMap(map, KeyNwnnsscompFilename, string.Empty);
                string legacyPath = GetMap(map, KeyNwnnsscompPath, string.Empty);
                if (!string.IsNullOrWhiteSpace(folderPath) && !string.IsNullOrWhiteSpace(nwnFilename))
                {
                    s.NwnnsscompFolderPath = folderPath.Trim();
                    s.NwnnsscompFilename = nwnFilename.Trim();
                    s.NwnnsscompPath = string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(legacyPath))
                {
                    legacyPath = legacyPath.Trim();
                    s.NwnnsscompPath = legacyPath;
                    if (File.Exists(legacyPath) && legacyPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        s.NwnnsscompFolderPath = Path.GetDirectoryName(legacyPath) ?? string.Empty;
                        s.NwnnsscompFilename = Path.GetFileName(legacyPath);
                    }
                    else if (Directory.Exists(legacyPath))
                    {
                        s.NwnnsscompFolderPath = legacyPath;
                        s.NwnnsscompFilename = DefaultNwnnsscompFilename(appBaseDirectory);
                    }
                    else
                    {
                        s.NwnnsscompFolderPath = folderPath;
                        s.NwnnsscompFilename = string.IsNullOrWhiteSpace(nwnFilename)
                            ? DefaultNwnnsscompFilename(appBaseDirectory)
                            : nwnFilename.Trim();
                    }
                }
                else
                {
                    s.NwnnsscompFolderPath = string.IsNullOrWhiteSpace(folderPath)
                        ? Path.Combine(appBaseDirectory, "tools")
                        : folderPath.Trim();
                    s.NwnnsscompFilename = string.IsNullOrWhiteSpace(nwnFilename)
                        ? DefaultNwnnsscompFilename(appBaseDirectory)
                        : nwnFilename.Trim();
                    s.NwnnsscompPath = string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Message=config parse failed; using defaults Path={Path}",
                    DecompPhaseNames.ConfigLoad,
                    cid,
                    ToolPathRedaction.FormatPath(pathToLoad));
                s = CreateDefaults(appBaseDirectory);
                s.ConfigLoadedFromPath = pathToLoad;
            }

            s.ApplyToRuntime();
            swLoad.Stop();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                bool isLegacy = pathToLoad != null && pathToLoad.EndsWith(LegacyConfigFileName, StringComparison.OrdinalIgnoreCase);
                logger.LogDebug(
                    "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Source=file ElapsedMs={ElapsedMs} Path={Path} Legacy={Legacy}",
                    DecompPhaseNames.ConfigLoad,
                    cid,
                    swLoad.ElapsedMilliseconds,
                    ToolPathRedaction.FormatPath(pathToLoad),
                    isLegacy);
            }

            return s;
        }

        public void Save(string appBaseDirectory, ILogger log = null)
        {
            ILogger logger = log ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional() ?? string.Empty;
            if (string.IsNullOrEmpty(appBaseDirectory))
            {
                appBaseDirectory = GetDefaultAppBaseDirectory();
            }

            string configDir = GetConfigDirectory(appBaseDirectory);
            Directory.CreateDirectory(configDir);
            string path = Path.Combine(configDir, ConfigFileName);
            var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
            map[KeyOutputDirectory] = OutputDirectory ?? string.Empty;
            map[KeyOpenDirectory] = OpenDirectory ?? string.Empty;
            map[KeyGameVariant] = GameVariant ?? "k1";
            map[KeyPreferSwitches] = PreferSwitches ? "true" : "false";
            map[KeyStrictSignatures] = StrictSignatures ? "true" : "false";
            map[KeyOverwriteFiles] = OverwriteFiles ? "true" : "false";
            map[KeyEncoding] = EncodingName ?? "Windows-1252";
            map[KeyFileExtension] = FileExtension ?? ".nss";
            map[KeyFilenamePrefix] = FilenamePrefix ?? string.Empty;
            map[KeyFilenameSuffix] = FilenameSuffix ?? string.Empty;
            map[KeyLinkScrollBars] = LinkScrollBars ? "true" : "false";
            if (!string.IsNullOrWhiteSpace(K1NwscriptPath))
            {
                map[KeyK1NwscriptPath] = K1NwscriptPath;
            }

            if (!string.IsNullOrWhiteSpace(K2NwscriptPath))
            {
                map[KeyK2NwscriptPath] = K2NwscriptPath;
            }

            map[KeyPreferNcsdis] = PreferNcsdis ? "true" : "false";
            if (!string.IsNullOrWhiteSpace(NcsdisPath))
            {
                map[KeyNcsdisPath] = NcsdisPath;
            }

            if (!string.IsNullOrWhiteSpace(NwnnsscompFolderPath))
            {
                map[KeyNwnnsscompFolderPath] = NwnnsscompFolderPath;
            }

            if (!string.IsNullOrWhiteSpace(NwnnsscompFilename))
            {
                map[KeyNwnnsscompFilename] = NwnnsscompFilename;
            }

            string resolved = GetResolvedCompilerPath();
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                map[KeyNwnnsscompPath] = resolved;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# NCSDecomp Configuration");
            sb.AppendLine("# Keys match Java DeNCS Settings / ncsdecomp.conf");
            foreach (var kv in map)
            {
                sb.Append(EscapePropertyKey(kv.Key));
                sb.Append('=');
                sb.AppendLine(EscapePropertyValue(kv.Value));
            }

            try
            {
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                ConfigLoadedFromPath = path;
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Path={Path} Bytes={Bytes}",
                        DecompPhaseNames.ConfigSave,
                        cid,
                        ToolPathRedaction.FormatPath(path),
                        sb.Length);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Tool=NcsDecompSettings Phase={Phase} CorrelationId={CorrelationId} Path={Path} Message=config write failed",
                    DecompPhaseNames.ConfigSave,
                    cid,
                    ToolPathRedaction.FormatPath(path));
                throw;
            }
        }

        public void ApplyToRuntime()
        {
            string g = (GameVariant ?? "k1").ToLowerInvariant();
            FileDecompilerOptions.IsK2Selected = g == "k2" || g == "tsl" || g == "2";
            FileDecompilerOptions.PreferSwitches = PreferSwitches;
            FileDecompilerOptions.StrictSignatures = StrictSignatures;
        }

        public void CaptureFromRuntime()
        {
            GameVariant = FileDecompilerOptions.IsK2Selected ? "k2" : "k1";
            PreferSwitches = FileDecompilerOptions.PreferSwitches;
            StrictSignatures = FileDecompilerOptions.StrictSignatures;
        }

        private static string GetMap(Dictionary<string, string> map, string key, string defaultValue)
        {
            if (map == null)
            {
                return defaultValue;
            }

            string v;
            if (map.TryGetValue(key, out v))
            {
                return v ?? defaultValue;
            }

            return defaultValue;
        }

        private static bool ParseBool(string s, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return defaultValue;
            }

            s = s.Trim().ToLowerInvariant();
            if (s == "true" || s == "yes" || s == "1")
            {
                return true;
            }

            if (s == "false" || s == "no" || s == "0")
            {
                return false;
            }

            bool b;
            if (bool.TryParse(s, out b))
            {
                return b;
            }

            return defaultValue;
        }

        private static Dictionary<string, string> ReadPropertiesFile(string path)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadAllLines(path, new UTF8Encoding(false)))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == '!')
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string val = UnescapePropertyValue(line.Substring(eq + 1).Trim());
                d[key] = val;
            }

            return d;
        }

        private static string EscapePropertyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return key.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string EscapePropertyValue(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20 || c > 0x7e)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }

        private static string UnescapePropertyValue(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (i + 4 < s.Length)
                            {
                                string hex = s.Substring(i + 1, 4);
                                int code;
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) &&
                                    code >= 0 && code <= 0xFFFF)
                                {
                                    sb.Append((char)code);
                                    i += 4;
                                }
                                else
                                {
                                    sb.Append('u');
                                }
                            }
                            else
                            {
                                sb.Append('u');
                            }

                            break;
                        default:
                            sb.Append(s[i]);
                            break;
                    }
                }
                else
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }
    }
}
