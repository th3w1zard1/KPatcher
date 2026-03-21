// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS CompilerUtil.java — app root, tools/, compiler discovery.

using System;
using System.Collections.Generic;
using System.IO;
using NCSDecomp.Core;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Resolves NCSDecomp install root, <c>tools/</c>, and external compiler executables.
    /// </summary>
    public static class CompilerUtil
    {
        /// <summary>Standard compiler filenames in search order (DeNCS <c>COMPILER_NAMES</c>).</summary>
        /// <remarks>
        /// KPatcher vendors additional BioWare-era binaries under <c>vendor/DeNCS/tools</c>; incompatible_* are last-resort.
        /// </remarks>
        public static readonly string[] CompilerNames =
        {
            "nwnnsscomp.exe",
            "nwnnsscomp_kscript.exe",
            "nwnnsscomp_ktool.exe",
            "nwnnsscomp_incompatible_tslpatcher.exe",
            "nwnnsscomp_incompatible_v1.exe",
            "ncsdis.exe"
        };

        private static string cachedNcsDecompDirectory;

        /// <summary>Clears cached app root (for tests).</summary>
        public static void ClearNCSDecompDirectoryCache()
        {
            cachedNcsDecompDirectory = null;
        }

        /// <summary>Application root (where <c>config/</c> and <c>tools/</c> live).</summary>
        public static string GetNCSDecompDirectory()
        {
            if (!string.IsNullOrEmpty(cachedNcsDecompDirectory))
            {
                return cachedNcsDecompDirectory;
            }

            cachedNcsDecompDirectory = DetectNCSDecompDirectory();
            return cachedNcsDecompDirectory;
        }

        private static string DetectNCSDecompDirectory()
        {
            string start = NcsDecompSettings.GetDefaultAppBaseDirectory();
            string dir = start;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                if (Directory.Exists(Path.Combine(dir, "tools")) ||
                    Directory.Exists(Path.Combine(dir, "config")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Environment.CurrentDirectory;
            if (Directory.Exists(Path.Combine(cwd, "tools")) ||
                Directory.Exists(Path.Combine(cwd, "config")))
            {
                return cwd;
            }

            return string.IsNullOrEmpty(start) ? cwd : start;
        }

        /// <summary>Returns a copy of <see cref="CompilerNames"/>.</summary>
        public static string[] GetCompilerNames()
        {
            return (string[])CompilerNames.Clone();
        }

        /// <summary>Preferred <c>tools</c> directory (may not exist yet).</summary>
        public static string GetToolsDirectory()
        {
            string appDir = GetNCSDecompDirectory();
            string appTools = Path.Combine(appDir, "tools");
            if (Directory.Exists(appTools))
            {
                return appTools;
            }

            string cwd = Environment.CurrentDirectory;
            if (!string.Equals(appDir, cwd, StringComparison.OrdinalIgnoreCase))
            {
                string cwdTools = Path.Combine(cwd, "tools");
                if (Directory.Exists(cwdTools))
                {
                    return cwdTools;
                }
            }

            return appTools;
        }

        /// <summary>First existing path among app/tools, cwd/tools, app, cwd (DeNCS <c>resolveToolsFile</c>).</summary>
        public static string ResolveToolsFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return Path.GetFullPath(GetToolsDirectory());
            }

            string appDir = GetNCSDecompDirectory();
            string cwd = Environment.CurrentDirectory;
            string[] candidates =
            {
                Path.Combine(appDir, "tools", filename),
                Path.Combine(cwd, "tools", filename),
                Path.Combine(appDir, filename),
                Path.Combine(cwd, filename)
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return Path.GetFullPath(candidates[i]);
                }
            }

            return Path.GetFullPath(Path.Combine(appDir, "tools", filename));
        }

        /// <summary>Resolve <c>config/filename</c> under app or cwd.</summary>
        public static string ResolveConfigFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return Path.GetFullPath(Path.Combine(GetNCSDecompDirectory(), "config"));
            }

            string appDir = GetNCSDecompDirectory();
            string cwd = Environment.CurrentDirectory;
            string a = Path.Combine(appDir, "config", filename);
            string b = Path.Combine(cwd, "config", filename);
            if (File.Exists(a))
            {
                return Path.GetFullPath(a);
            }

            if (File.Exists(b))
            {
                return Path.GetFullPath(b);
            }

            return Path.GetFullPath(a);
        }

        /// <summary>
        /// Walks upward from common host locations and returns each distinct
        /// <c>vendor/DeNCS/tools</c> directory (KPatcher repo layout) so tests and CLIs find vendored compilers without <c>NWNNSCOMP_PATH</c>.
        /// </summary>
        private static IEnumerable<string> EnumerateVendorDeNcsToolsDirectories()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seeds = new List<string>();

            void addSeed(string s)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    try
                    {
                        seeds.Add(Path.GetFullPath(s));
                    }
                    catch
                    {
                        // ignore invalid paths
                    }
                }
            }

            addSeed(NcsDecompSettings.GetDefaultAppBaseDirectory());
            addSeed(Environment.CurrentDirectory);
            try
            {
                string loc = typeof(CompilerUtil).Assembly.Location;
                if (!string.IsNullOrEmpty(loc))
                {
                    addSeed(Path.GetDirectoryName(loc));
                }
            }
            catch
            {
                // ignore
            }

            for (int s = 0; s < seeds.Count; s++)
            {
                string dir = seeds[s];
                for (int depth = 0; depth < 16 && !string.IsNullOrEmpty(dir); depth++)
                {
                    string tools = Path.Combine(dir, "vendor", "DeNCS", "tools");
                    if (Directory.Exists(tools))
                    {
                        string full = Path.GetFullPath(tools);
                        if (seen.Add(full))
                        {
                            yield return full;
                        }
                    }

                    dir = Path.GetDirectoryName(dir);
                }
            }
        }

        /// <summary>First compiler found in <see cref="EnumerateVendorDeNcsToolsDirectories"/>.</summary>
        private static string TryResolveCompilerInVendorDeNcsTools()
        {
            foreach (string toolsDir in EnumerateVendorDeNcsToolsDirectories())
            {
                for (int i = 0; i < CompilerNames.Length; i++)
                {
                    string candidate = Path.Combine(toolsDir, CompilerNames[i]);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            return null;
        }

        /// <summary>Combine folder + filename (no existence check).</summary>
        public static string ResolveCompilerPath(string folderPath, string filename)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }

            folderPath = folderPath.Trim();
            filename = filename.Trim();
            if (File.Exists(folderPath) && (folderPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                            folderPath.EndsWith(".EXE", StringComparison.OrdinalIgnoreCase)))
            {
                string parent = Path.GetDirectoryName(folderPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    folderPath = parent;
                }
                else
                {
                    return null;
                }
            }

            return Path.GetFullPath(Path.Combine(folderPath, filename));
        }

        /// <summary>CLI-style discovery: explicit path, then app <c>tools/</c>, then cwd (DeNCS <c>resolveCompilerPathWithFallbacks</c>).</summary>
        public static string ResolveCompilerPathWithFallbacks(string cliPath)
        {
            if (!string.IsNullOrWhiteSpace(cliPath))
            {
                cliPath = cliPath.Trim();
                if (File.Exists(cliPath))
                {
                    return Path.GetFullPath(cliPath);
                }

                if (Directory.Exists(cliPath))
                {
                    for (int i = 0; i < CompilerNames.Length; i++)
                    {
                        string candidate = Path.Combine(cliPath, CompilerNames[i]);
                        if (File.Exists(candidate))
                        {
                            return Path.GetFullPath(candidate);
                        }
                    }
                }
            }

            string ncsDir = GetNCSDecompDirectory();
            string ncsTools = Path.Combine(ncsDir, "tools");
            for (int i = 0; i < CompilerNames.Length; i++)
            {
                string candidate = Path.Combine(ncsTools, CompilerNames[i]);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            for (int i = 0; i < CompilerNames.Length; i++)
            {
                string candidate = Path.Combine(ncsDir, CompilerNames[i]);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            string cwd = Environment.CurrentDirectory;
            if (!string.Equals(ncsDir, cwd, StringComparison.OrdinalIgnoreCase))
            {
                string cwdTools = Path.Combine(cwd, "tools");
                for (int i = 0; i < CompilerNames.Length; i++)
                {
                    string candidate = Path.Combine(cwdTools, CompilerNames[i]);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }

                for (int i = 0; i < CompilerNames.Length; i++)
                {
                    string candidate = Path.Combine(cwd, CompilerNames[i]);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            string vendor = TryResolveCompilerInVendorDeNcsTools();
            if (!string.IsNullOrEmpty(vendor))
            {
                return vendor;
            }

            return null;
        }

        /// <summary>Result of <see cref="FindCompilerFileWithResult"/>.</summary>
        public sealed class CompilerResolutionResult
        {
            public CompilerResolutionResult(string filePath, bool isFallback, string source)
            {
                FilePath = filePath ?? string.Empty;
                IsFallback = isFallback;
                Source = source ?? string.Empty;
            }

            public string FilePath { get; }
            public bool IsFallback { get; }
            public string Source { get; }
        }

        /// <summary>Configured directory/path first, then same fallbacks as Java Settings preview.</summary>
        public static CompilerResolutionResult FindCompilerFileWithResult(string configuredPath)
        {
            string[] names = CompilerNames;
            string trimmed = configuredPath != null ? configuredPath.Trim() : string.Empty;
            if (trimmed.Length > 0)
            {
                if (Directory.Exists(trimmed))
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        string candidate = Path.Combine(trimmed, names[i]);
                        if (File.Exists(candidate))
                        {
                            return new CompilerResolutionResult(candidate, false, "Configured directory: " + trimmed);
                        }
                    }
                }
                else if (File.Exists(trimmed))
                {
                    return new CompilerResolutionResult(Path.GetFullPath(trimmed), false, "Configured path: " + trimmed);
                }
                else
                {
                    string parent = Path.GetDirectoryName(trimmed);
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            string candidate = Path.Combine(parent, names[i]);
                            if (File.Exists(candidate))
                            {
                                return new CompilerResolutionResult(candidate, true, "Fallback in configured directory: " + parent);
                            }
                        }
                    }
                }
            }

            string ncsDir = GetNCSDecompDirectory();
            string ncsTools = Path.Combine(ncsDir, "tools");
            for (int i = 0; i < names.Length; i++)
            {
                string candidate = Path.Combine(ncsTools, names[i]);
                if (File.Exists(candidate))
                {
                    return new CompilerResolutionResult(candidate, true, "Fallback: NCSDecomp tools/");
                }
            }

            for (int i = 0; i < names.Length; i++)
            {
                string candidate = Path.Combine(ncsDir, names[i]);
                if (File.Exists(candidate))
                {
                    return new CompilerResolutionResult(candidate, true, "Fallback: NCSDecomp directory");
                }
            }

            string cwd = Environment.CurrentDirectory;
            if (!string.Equals(ncsDir, cwd, StringComparison.OrdinalIgnoreCase))
            {
                string cwdTools = Path.Combine(cwd, "tools");
                for (int i = 0; i < names.Length; i++)
                {
                    string candidate = Path.Combine(cwdTools, names[i]);
                    if (File.Exists(candidate))
                    {
                        return new CompilerResolutionResult(candidate, true, "Fallback: CWD tools/");
                    }
                }

                for (int i = 0; i < names.Length; i++)
                {
                    string candidate = Path.Combine(cwd, names[i]);
                    if (File.Exists(candidate))
                    {
                        return new CompilerResolutionResult(candidate, true, "Fallback: current directory");
                    }
                }
            }

            string vendorExe = TryResolveCompilerInVendorDeNcsTools();
            if (!string.IsNullOrEmpty(vendorExe))
            {
                return new CompilerResolutionResult(vendorExe, true, "Fallback: vendor/DeNCS/tools");
            }

            return null;
        }

        /// <summary>
        /// Combines <see cref="NcsDecompSettings.NwnnsscompFolderPath"/> + <see cref="NcsDecompSettings.NwnnsscompFilename"/>,
        /// then legacy <see cref="NcsDecompSettings.NwnnsscompPath"/>, then <see cref="FindCompilerFileWithResult"/> on the folder.
        /// </summary>
        public static string GetCompilerPathFromSettings(NcsDecompSettings settings)
        {
            if (settings == null)
            {
                return null;
            }

            string combined = ResolveCompilerPath(settings.NwnnsscompFolderPath, settings.NwnnsscompFilename);
            if (!string.IsNullOrEmpty(combined) && File.Exists(combined))
            {
                return Path.GetFullPath(combined);
            }

            if (!string.IsNullOrWhiteSpace(settings.NwnnsscompPath))
            {
                string p = settings.NwnnsscompPath.Trim();
                if (File.Exists(p))
                {
                    return Path.GetFullPath(p);
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.NwnnsscompFolderPath))
            {
                CompilerResolutionResult found = FindCompilerFileWithResult(settings.NwnnsscompFolderPath.Trim());
                if (found != null && File.Exists(found.FilePath))
                {
                    return Path.GetFullPath(found.FilePath);
                }
            }

            return null;
        }
    }
}
