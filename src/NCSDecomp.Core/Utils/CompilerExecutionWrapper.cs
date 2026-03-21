// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS CompilerExecutionWrapper.java — env prep + argv for optional external compilers.
// Product policy: no registry spoofing; KOTOR Tool–style HKLM hacks are not used (see CreateRegistrySpoofer).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NCSDecomp.Core;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Prepares working directory, nwscript.nss beside the compiler, include copies, and argv for external compile.
    /// Call <see cref="PrepareExecutionEnvironment"/>, then <see cref="GetCompileArgs"/> / <see cref="ExecuteCompile"/>, then <see cref="Cleanup"/>.
    /// </summary>
    public sealed class CompilerExecutionWrapper
    {
        private readonly string compilerFile;
        private readonly string sourceFile;
        private readonly string outputFile;
        private readonly bool isK2;
        private readonly KnownExternalCompiler compiler;
        private readonly NwnnsscompConfig config;
        private readonly Dictionary<string, string> envOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> copiedIncludeFiles = new List<string>();
        private readonly List<string> copiedNwscriptFiles = new List<string>();
        private string originalNwscriptBackup;
        private string copiedSourceFile;
        private string actualSourceFile;

        /// <exception cref="ArgumentException">Unknown compiler fingerprint.</exception>
        /// <exception cref="IOException">Hash/read failure.</exception>
        public CompilerExecutionWrapper(string compilerPath, string sourcePath, string outputPath, bool isK2)
        {
            if (string.IsNullOrEmpty(compilerPath))
            {
                throw new ArgumentNullException(nameof(compilerPath));
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            compilerFile = Path.GetFullPath(compilerPath);
            sourceFile = Path.GetFullPath(sourcePath);
            outputFile = Path.GetFullPath(outputPath);
            this.isK2 = isK2;
            config = new NwnnsscompConfig(compilerFile, sourceFile, outputFile, isK2);
            compiler = config.ChosenCompiler;
            BuildEnvironmentOverrides();
        }

        public KnownExternalCompiler Compiler
        {
            get { return compiler; }
        }

        public NwnnsscompConfig Config
        {
            get { return config; }
        }

        public IReadOnlyDictionary<string, string> EnvironmentOverrides
        {
            get { return envOverrides; }
        }

        /// <summary>
        /// Always returns <see cref="NoOpRegistrySpoofer"/>. DeNCS/Java used HKLM spoofing for legacy BioWare-era compilers;
        /// KPatcher relies on in-process managed compilation for NSS→NCS and does not ship registry spoofing as a product feature.
        /// </summary>
        public IRegistrySpoofer CreateRegistrySpoofer() => new NoOpRegistrySpoofer();

        public void PrepareExecutionEnvironment(IList<string> includeDirs)
        {
            PrepareNwscriptFile();

            PrepareIncludeFiles(includeDirs);
            actualSourceFile = sourceFile;
        }

        public string GetWorkingDirectory()
        {
            if (compiler == KnownExternalCompilers.KotorTool ||
                compiler == KnownExternalCompilers.KotorScriptingTool ||
                !SupportsGameFlag())
            {
                string compilerDir = Path.GetDirectoryName(compilerFile);
                if (!string.IsNullOrEmpty(compilerDir) && Directory.Exists(compilerDir))
                {
                    return compilerDir;
                }
            }

            string sourceDir = Path.GetDirectoryName(sourceFile);
            if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
            {
                return sourceDir;
            }

            string compilerDir2 = Path.GetDirectoryName(compilerFile);
            if (!string.IsNullOrEmpty(compilerDir2) && Directory.Exists(compilerDir2))
            {
                return compilerDir2;
            }

            return CompilerUtil.GetNCSDecompDirectory();
        }

        public string[] GetCompileArgs(IList<string> includeDirs)
        {
            if (actualSourceFile != null &&
                !string.Equals(actualSourceFile, sourceFile, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var spoofedConfig = new NwnnsscompConfig(compilerFile, actualSourceFile, outputFile, isK2);
                    return spoofedConfig.GetCompileArgs(compilerFile, includeDirs);
                }
                catch (Exception)
                {
                    return config.GetCompileArgs(compilerFile, includeDirs);
                }
            }

            return config.GetCompileArgs(compilerFile, includeDirs);
        }

        /// <summary>Runs the compiler after <see cref="PrepareExecutionEnvironment"/>.</summary>
        public ExternalCompilerRunResult ExecuteCompile(IList<string> includeDirs, int? timeoutMilliseconds)
        {
            string[] argv = GetCompileArgs(includeDirs);
            return ExternalCompilerProcess.Run(argv, GetWorkingDirectory(), envOverrides, timeoutMilliseconds);
        }

        public void Cleanup()
        {
            if (!string.IsNullOrEmpty(copiedSourceFile) && File.Exists(copiedSourceFile))
            {
                try
                {
                    File.Delete(copiedSourceFile);
                }
                catch
                {
                    // ignore
                }

                copiedSourceFile = null;
            }

            for (int i = 0; i < copiedIncludeFiles.Count; i++)
            {
                try
                {
                    if (File.Exists(copiedIncludeFiles[i]))
                    {
                        File.Delete(copiedIncludeFiles[i]);
                    }
                }
                catch
                {
                    // ignore
                }
            }

            copiedIncludeFiles.Clear();

            if (!string.IsNullOrEmpty(originalNwscriptBackup) && File.Exists(originalNwscriptBackup))
            {
                try
                {
                    string compilerDir = Path.GetDirectoryName(compilerFile);
                    if (!string.IsNullOrEmpty(compilerDir))
                    {
                        string compilerNwscript = Path.Combine(compilerDir, "nwscript.nss");
                        if (File.Exists(compilerNwscript))
                        {
                            File.Copy(originalNwscriptBackup, compilerNwscript, true);
                        }
                    }

                    File.Delete(originalNwscriptBackup);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void PrepareIncludeFiles(IList<string> includeDirs)
        {
            if (includeDirs == null || includeDirs.Count == 0)
            {
                return;
            }

            bool supportsIncludeFlag = compiler != KnownExternalCompilers.KotorTool &&
                                       compiler != KnownExternalCompilers.KotorScriptingTool;
            if (supportsIncludeFlag)
            {
                return;
            }

            string sourceDir = Path.GetDirectoryName(sourceFile);
            if (string.IsNullOrEmpty(sourceDir))
            {
                return;
            }

            Directory.CreateDirectory(sourceDir);

            HashSet<string> needed = ExtractIncludeFiles(sourceFile);
            foreach (string includeName in needed)
            {
                string destFile = Path.Combine(sourceDir, includeName);
                if (File.Exists(destFile))
                {
                    continue;
                }

                for (int i = 0; i < includeDirs.Count; i++)
                {
                    if (string.IsNullOrEmpty(includeDirs[i]) || !Directory.Exists(includeDirs[i]))
                    {
                        continue;
                    }

                    string srcInc = Path.Combine(includeDirs[i], includeName);
                    if (File.Exists(srcInc))
                    {
                        File.Copy(srcInc, destFile, true);
                        copiedIncludeFiles.Add(destFile);
                        break;
                    }
                }
            }
        }

        private void PrepareNwscriptFile()
        {
            string compilerDir = Path.GetDirectoryName(compilerFile);
            if (string.IsNullOrEmpty(compilerDir))
            {
                return;
            }

            Directory.CreateDirectory(compilerDir);

            string compilerNwscript = Path.Combine(compilerDir, "nwscript.nss");
            string nwscriptSource = DetermineNwscriptSource();
            if (string.IsNullOrEmpty(nwscriptSource) || !File.Exists(nwscriptSource))
            {
                return;
            }

            bool needsUpdate = true;
            if (File.Exists(compilerNwscript))
            {
                try
                {
                    if (string.Equals(
                            Path.GetFullPath(nwscriptSource),
                            Path.GetFullPath(compilerNwscript),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        needsUpdate = false;
                    }
                }
                catch
                {
                    needsUpdate = true;
                }
            }

            if (!needsUpdate)
            {
                return;
            }

            if (File.Exists(compilerNwscript))
            {
                string backup = Path.Combine(compilerDir, "nwscript.nss.backup");
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }

                File.Copy(compilerNwscript, backup, true);
                originalNwscriptBackup = backup;
            }

            File.Copy(nwscriptSource, compilerNwscript, true);
            copiedNwscriptFiles.Add(compilerNwscript);
        }

        private string DetermineNwscriptSource()
        {
            if (isK2)
            {
                return CompilerUtil.ResolveToolsFile("tsl_nwscript.nss");
            }

            if (CheckNeedsAscNwscript(sourceFile))
            {
                string asc = CompilerUtil.ResolveToolsFile("k1_asc_donotuse_nwscript.nss");
                if (File.Exists(asc))
                {
                    return asc;
                }
            }

            return CompilerUtil.ResolveToolsFile("k1_nwscript.nss");
        }

        private static bool CheckNeedsAscNwscript(string nssFile)
        {
            try
            {
                if (!File.Exists(nssFile))
                {
                    return false;
                }

                string content = File.ReadAllText(nssFile, Encoding.UTF8);
                var pattern = new Regex(
                    @"ActionStartConversation\s*\(([^,)]*,\s*){10}[^)]*\)",
                    RegexOptions.Multiline | RegexOptions.CultureInvariant);
                return pattern.IsMatch(content);
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<string> ExtractIncludeFiles(string path)
        {
            var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string content = File.ReadAllText(path, Encoding.UTF8);
                var pattern = new Regex("#include\\s+[\"<]([^\">]+)[\">]", RegexOptions.CultureInvariant);
                foreach (Match m in pattern.Matches(content))
                {
                    if (!m.Success)
                    {
                        continue;
                    }

                    string includeName = m.Groups[1].Value;
                    if (includeName.Length == 0)
                    {
                        continue;
                    }

                    if (!includeName.EndsWith(".nss", StringComparison.OrdinalIgnoreCase) &&
                        !includeName.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
                    {
                        includeName = includeName + ".nss";
                    }

                    includes.Add(includeName);
                }
            }
            catch
            {
                // ignore
            }

            return includes;
        }

        private void BuildEnvironmentOverrides()
        {
            string toolsDir = CompilerUtil.GetToolsDirectory();
            bool needsRootOverride = compiler == KnownExternalCompilers.KotorTool ||
                                     compiler == KnownExternalCompilers.KotorScriptingTool ||
                                     !SupportsGameFlag();
            if (!needsRootOverride)
            {
                return;
            }

            string resolvedRoot = Path.GetFullPath(toolsDir);
            envOverrides["NWN_ROOT"] = resolvedRoot;
            envOverrides["NWNDir"] = resolvedRoot;
            envOverrides["KOTOR_ROOT"] = resolvedRoot;
        }

        private bool SupportsGameFlag()
        {
            string[] args = compiler.GetCompileArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg != null && (arg.IndexOf("{game_value}", StringComparison.Ordinal) >= 0 ||
                                    arg == "-g" ||
                                    arg.StartsWith("-g", StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
