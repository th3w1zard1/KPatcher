// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS NwnnsscompConfig.java — argv templates from SHA256-detected toolchain.

using System;
using System.Collections.Generic;
using System.IO;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Builds compile/decompile argument lists for a fingerprinted nwnnsscomp.exe or ncsdis.exe.
    /// Optional: KPatcher may shell out on Windows; NCSDecomp.NET stays managed-only by policy.
    /// </summary>
    public sealed class NwnnsscompConfig
    {
        private readonly string sha256Hash;
        private readonly string sourceFile;
        private readonly string outputFile;
        private readonly string outputDir;
        private readonly string outputName;
        private readonly bool isK2;
        private readonly KnownExternalCompiler chosenCompiler;

        /// <exception cref="ArgumentException">Compiler SHA256 not in <see cref="KnownExternalCompilers"/>.</exception>
        /// <exception cref="IOException">Compiler file cannot be read.</exception>
        public NwnnsscompConfig(string compilerPath, string sourceFilePath, string outputFilePath, bool isK2)
        {
            if (string.IsNullOrEmpty(compilerPath))
            {
                throw new ArgumentNullException(nameof(compilerPath));
            }

            if (string.IsNullOrEmpty(sourceFilePath))
            {
                throw new ArgumentNullException(nameof(sourceFilePath));
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentNullException(nameof(outputFilePath));
            }

            sourceFile = Path.GetFullPath(sourceFilePath);
            outputFile = Path.GetFullPath(outputFilePath);
            outputDir = Path.GetDirectoryName(outputFile);
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = string.Empty;
            }

            outputName = Path.GetFileName(outputFile);
            this.isK2 = isK2;

            sha256Hash = HashUtil.CalculateSha256(compilerPath);
            chosenCompiler = KnownExternalCompilers.FromSha256(sha256Hash);
            if (chosenCompiler == null)
            {
                throw new ArgumentException(
                    "Unknown compiler version with SHA256 hash: " + sha256Hash +
                    ". Use a known nwnnsscomp.exe or ncsdis.exe build.");
            }
        }

        public KnownExternalCompiler ChosenCompiler
        {
            get { return chosenCompiler; }
        }

        public string Sha256Hash
        {
            get { return sha256Hash; }
        }

        public string[] GetCompileArgs(string executable)
        {
            return GetCompileArgs(executable, null);
        }

        public string[] GetCompileArgs(string executable, IList<string> includeDirectories)
        {
            if (string.IsNullOrEmpty(executable))
            {
                throw new ArgumentNullException(nameof(executable));
            }

            var includeArgs = new List<string>();
            if (includeDirectories != null && includeDirectories.Count > 0)
            {
                bool supportsIncludeFlag = chosenCompiler != KnownExternalCompilers.KotorTool &&
                    chosenCompiler != KnownExternalCompilers.KotorScriptingTool;
                if (supportsIncludeFlag)
                {
                    for (int i = 0; i < includeDirectories.Count; i++)
                    {
                        string dir = includeDirectories[i];
                        if (string.IsNullOrEmpty(dir))
                        {
                            continue;
                        }

                        if (Directory.Exists(dir))
                        {
                            includeArgs.Add("-i");
                            includeArgs.Add(Path.GetFullPath(dir));
                        }
                    }
                }
            }

            string[] template = chosenCompiler.GetCompileArgs();
            var args = new List<string>();
            for (int i = 0; i < template.Length; i++)
            {
                string arg = template[i];
                if (arg == "{includes}")
                {
                    args.AddRange(includeArgs);
                }
                else
                {
                    args.Add(FormatArg(arg));
                }
            }

            string[] result = new string[args.Count + 1];
            result[0] = executable;
            for (int i = 0; i < args.Count; i++)
            {
                result[i + 1] = args[i];
            }

            return result;
        }

        public string[] GetDecompileArgs(string executable)
        {
            if (string.IsNullOrEmpty(executable))
            {
                throw new ArgumentNullException(nameof(executable));
            }

            if (!chosenCompiler.SupportsDecompilation)
            {
                throw new InvalidOperationException(
                    "Compiler '" + chosenCompiler.Name + "' does not support decompilation.");
            }

            if (chosenCompiler.IsNcsdis)
            {
                return new[] { executable, sourceFile, outputFile };
            }

            return FormatArgs(chosenCompiler.GetDecompileArgs(), executable);
        }

        private string FormatArg(string arg)
        {
            return arg
                .Replace("{source}", sourceFile)
                .Replace("{output}", outputFile)
                .Replace("{output_dir}", outputDir)
                .Replace("{output_name}", outputName)
                .Replace("{game_value}", isK2 ? "2" : "1");
        }

        private string[] FormatArgs(string[] argsList, string executable)
        {
            var formatted = new List<string>();
            for (int i = 0; i < argsList.Length; i++)
            {
                string replaced = argsList[i]
                    .Replace("{source}", sourceFile)
                    .Replace("{output}", outputFile)
                    .Replace("{output_dir}", outputDir)
                    .Replace("{output_name}", outputName)
                    .Replace("{game_value}", isK2 ? "2" : "1")
                    .Replace("{includes}", string.Empty);
                if (replaced.Length > 0)
                {
                    formatted.Add(replaced);
                }
            }

            string[] result = new string[formatted.Count + 1];
            result[0] = executable;
            for (int i = 0; i < formatted.Count; i++)
            {
                result[i + 1] = formatted[i];
            }

            return result;
        }
    }
}
