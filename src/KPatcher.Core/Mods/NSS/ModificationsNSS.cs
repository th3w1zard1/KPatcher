using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using KCompiler;
using KCompiler.Cli;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Compiler;
using NssCompileError = KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.NSS
{

    /// <summary>
    /// Mutable string wrapper for token replacement in NSS files.
    /// </summary>
    public class MutableString
    {
        public string Value { get; set; }

        public MutableString(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Container for NSS (script source) modifications.
    /// </summary>
    public class ModificationsNSS : PatcherModifications
    {
        public new const string DEFAULT_DESTINATION = "Override";
        public static string DefaultDestination => DEFAULT_DESTINATION;

        private static Encoding Windows1252Encoding
        {
            get
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding("windows-1252");
            }
        }

        private sealed class ManagedCompileOptions
        {
            public Game Game { get; set; }

            public bool Debug { get; set; }

            [CanBeNull]
            public string NwscriptPath { get; set; }
        }

        public new string Action { get; set; } = "Compile";
        public new bool SkipIfNotReplace { get; set; } = true;
        [CanBeNull]
        public string TempScriptFolder { get; set; }
        public string ScriptCompilerFlags { get; set; } = string.Empty;

        [CanBeNull]
        public string CompilerWorkingDirectory { get; set; }

        [CanBeNull]
        internal Func<string, Game, IReadOnlyList<string>, bool, string, byte[]> CompileSourceToBytesOverride { get; set; }

        public ModificationsNSS(string filename, bool replaceFile = false)
            : base(filename, replaceFile)
        {
            SaveAs = Path.ChangeExtension(filename, ".ncs");
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            string label = SaveAs ?? SourceFile ?? "";
            if (source is null)
            {
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: null source for {0}; returning sentinel true", label));
                logger.AddError(PatcherResources.InvalidNssSourceProvided);
                return true;
            }

            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsNSS.PatchResource: sourceFile={0} saveAs={1} sourceBytes={2} action={3} game={4} skipIfNotReplace={5}",
                SourceFile, SaveAs, source.Length, Action, game, SkipIfNotReplace));

            // Decode the NSS source bytes
            string sourceText = Windows1252Encoding.GetString(source);
            var mutableSource = new MutableString(sourceText);
            Apply(mutableSource, memory, logger, game);
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsNSS.PatchResource: after token Apply nssCharLength={0}", mutableSource.Value.Length));

            if (Action.Equals("Compile", StringComparison.OrdinalIgnoreCase)
                && IsVendoredIncludeFile(sourceText))
            {
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: vendor include heuristic matched sourceFile={0}; skipping compile output", SourceFile));
                return true;
            }

            // Compile the modified NSS source to NCS bytecode
            if (Action.Equals("Compile", StringComparison.OrdinalIgnoreCase))
            {
                string tempFolder = TempScriptFolder is null ? Path.GetTempPath() : TempScriptFolder;
                Directory.CreateDirectory(tempFolder);
                string relativeSourceFolder = string.IsNullOrWhiteSpace(SourceFolder) ? "." : SourceFolder;
                string tempScriptFile = Path.Combine(tempFolder, relativeSourceFolder, SourceFile);
                string tempScriptDirectory = Path.GetDirectoryName(tempScriptFile);
                string tempNcsFile = Path.Combine(tempFolder, relativeSourceFolder, SaveAs ?? Path.ChangeExtension(SourceFile, ".ncs"));
                if (!string.IsNullOrEmpty(tempScriptDirectory))
                {
                    Directory.CreateDirectory(tempScriptDirectory);
                }
                File.WriteAllText(tempScriptFile, mutableSource.Value, Windows1252Encoding);
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: wrote temp NSS path={0} tempFolder={1}",
                    tempScriptFile, tempFolder));

                byte[] compiledBytes = null;
                ManagedCompileOptions compileOptions;
                string compileOptionFailureFeedback;

                try
                {
                    if (!TryResolveManagedCompileOptions(tempScriptFile, tempNcsFile, game, logger, out compileOptions, out compileOptionFailureFeedback))
                    {
                        LogCompilerFeedback(logger, compileOptionFailureFeedback);
                        logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "ModificationsNSS.PatchResource: compile option resolution rejected sourceFile={0} feedback={1}",
                            SourceFile,
                            compileOptionFailureFeedback ?? "(none)"));
                        logger.AddError(string.Format(
                            CultureInfo.CurrentCulture,
                            PatcherResources.CompileListCompiledNotFoundFormat,
                            SourceFile));
                        return true;
                    }
                }
                catch (Exception e)
                {
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: compile option resolution failed sourceFile={0} type={1} message={2}",
                        SourceFile, e.GetType().FullName, e.Message));
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.BuiltInCompilationFailedFormat, SourceFile, e.Message));
                    return true;
                }

                try
                {
                    Func<string, Game, IReadOnlyList<string>, bool, string, byte[]> compileSourceToBytes = CompileSourceToBytesOverride;
                    compiledBytes = compileSourceToBytes != null
                        ? compileSourceToBytes(
                            mutableSource.Value,
                            compileOptions.Game,
                            BuildLibraryLookupPaths(tempFolder, tempScriptDirectory),
                            compileOptions.Debug,
                            compileOptions.NwscriptPath)
                        : ManagedNwnnsscomp.CompileSourceToBytes(
                            mutableSource.Value,
                            compileOptions.Game,
                            BuildLibraryLookupPaths(tempFolder, tempScriptDirectory),
                            compileOptions.Debug,
                            compileOptions.NwscriptPath);
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: built-in compile ok sourceFile={0} ncsBytes={1}", SourceFile, compiledBytes.Length));
                    return compiledBytes;
                }
                catch (Exception e) when (e is CompileError || e is NssCompileError)
                {
                    LogCompilerFeedback(logger, e.Message);
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: built-in compile error sourceFile={0} type={1} message={2}",
                        SourceFile, e.GetType().FullName, e.Message));
                }
                catch (Exception e)
                {
                    if (TryHandleCompilerUnavailable(logger, e))
                    {
                        logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "ModificationsNSS.PatchResource: built-in compiler unavailable sourceFile={0} type={1} message={2}",
                            SourceFile, e.GetType().FullName, e.Message));
                        return true;
                    }

                    if (ShouldTreatAsCompilerFeedback(e))
                    {
                        LogCompilerFeedback(logger, e.Message);
                        logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "ModificationsNSS.PatchResource: built-in compile feedback exception sourceFile={0} type={1} message={2}",
                            SourceFile, e.GetType().FullName, e.Message));
                    }
                    else
                    {
                        logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                            "ModificationsNSS.PatchResource: built-in compile exception sourceFile={0} type={1} message={2}",
                            SourceFile, e.GetType().FullName, e.Message));
                        logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.BuiltInCompilationFailedFormat, SourceFile, e.Message));
                        return true;
                    }
                }

                if (compiledBytes != null)
                {
                    return compiledBytes;
                }

                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: no compiled bytecode produced for sourceFile={0}; returning sentinel true to skip write", SourceFile));
                logger.AddError(string.Format(
                    CultureInfo.CurrentCulture,
                    PatcherResources.CompileListCompiledNotFoundFormat,
                    SourceFile));
                return true;
            }

            // If not compiling, just return the modified source
            byte[] nssOut = Windows1252Encoding.GetBytes(mutableSource.Value);
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsNSS.PatchResource: non-Compile action returning NSS bytes sourceFile={0} outBytes={1}", SourceFile, nssOut.Length));
            return nssOut;
        }

        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            if (mutableData is MutableString nssSource)
            {
                IterateAndReplaceTokens2DA("2DAMEMORY", memory.Memory2DA, nssSource, logger);
                IterateAndReplaceTokensStr("StrRef", memory.MemoryStr, nssSource, logger);
            }
            else
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedMutableStringButGotFormat, mutableData.GetType().Name));
            }
        }

        private void IterateAndReplaceTokens2DA(string tokenName, Dictionary<int, string> memoryDict, MutableString nssSource, PatchLogger logger)
        {
            string searchPattern = $@"#{tokenName}\d+#";
            Match match = Regex.Match(nssSource.Value, searchPattern);

            while (match.Success)
            {
                int start = match.Index;
                int end = start + match.Length;

                // Extract the token ID from the match (e.g., #2DAMEMORY5# -> 5)
                string tokenIdStr = nssSource.Value.Substring(start + tokenName.Length + 1, end - start - tokenName.Length - 2);
                int tokenId = int.Parse(tokenIdStr);

                if (!memoryDict.ContainsKey(tokenId))
                {
                    throw new KeyError($"{tokenName}{tokenId} was not defined before use in '{SourceFile}'");
                }

                string replacementValue = memoryDict[tokenId];
                logger.AddVerbose($"{SourceFile}: Replacing '#{tokenName}{tokenId}#' with '{replacementValue}'");
                nssSource.Value = nssSource.Value.Substring(0, start) + replacementValue + nssSource.Value.Substring(end);

                match = Regex.Match(nssSource.Value, searchPattern);
            }
        }

        private void IterateAndReplaceTokensStr(string tokenName, Dictionary<int, int> memoryDict, MutableString nssSource, PatchLogger logger)
        {
            string searchPattern = $@"#{tokenName}\d+#";
            Match match = Regex.Match(nssSource.Value, searchPattern);

            while (match.Success)
            {
                int start = match.Index;
                int end = start + match.Length;

                // Extract the token ID from the match (e.g., #2DAMEMORY5# -> 5)
                string tokenIdStr = nssSource.Value.Substring(start + tokenName.Length + 1, end - start - tokenName.Length - 2);
                int tokenId = int.Parse(tokenIdStr);

                if (!memoryDict.ContainsKey(tokenId))
                {
                    throw new KeyError($"{tokenName}{tokenId} was not defined before use in '{SourceFile}'");
                }

                int replacementValue = memoryDict[tokenId];
                logger.AddVerbose($"{SourceFile}: Replacing '#{tokenName}{tokenId}#' with '{replacementValue}'");
                nssSource.Value = nssSource.Value.Substring(0, start) + replacementValue.ToString() + nssSource.Value.Substring(end);

                match = Regex.Match(nssSource.Value, searchPattern);
            }
        }

        private bool TryResolveManagedCompileOptions(
            string tempScriptFile,
            string tempNcsFile,
            Game defaultGame,
            PatchLogger logger,
            out ManagedCompileOptions options,
            out string failureFeedback)
        {
            options = new ManagedCompileOptions
            {
                Game = defaultGame,
                Debug = false,
                NwscriptPath = null
            };
            failureFeedback = null;

            if (string.IsNullOrWhiteSpace(ScriptCompilerFlags))
            {
                return true;
            }

            var args = new List<string>(NwnnsscompCliParser.SplitCommandLine(ScriptCompilerFlags));
            args.Add("-c");
            args.Add(tempScriptFile);
            args.Add("-o");
            args.Add(tempNcsFile);

            string workingDirectory = string.IsNullOrWhiteSpace(CompilerWorkingDirectory)
                ? Path.GetDirectoryName(tempScriptFile) ?? Directory.GetCurrentDirectory()
                : CompilerWorkingDirectory;
            NwnnsscompParseResult parseResult = NwnnsscompCliParser.Parse(args.ToArray(), workingDirectory, null);
            if (!parseResult.Success || parseResult.IsHelp)
            {
                failureFeedback = parseResult.ErrorMessage ?? "unknown parser failure";
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.TryResolveManagedCompileOptions: parse failed sourceFile={0} flags={1} feedback={2} workingDirectory={3}",
                    SourceFile,
                    ScriptCompilerFlags,
                    failureFeedback,
                    workingDirectory));
                return false;
            }

            options.Game = parseResult.GameExplicitlySet ? parseResult.Game : defaultGame;
            options.Debug = parseResult.Debug;
            options.NwscriptPath = parseResult.NwscriptPath;

            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsNSS.ResolveManagedCompileOptions: sourceFile={0} flags={1} game={2} debug={3} nwscript={4} workingDirectory={5}",
                SourceFile,
                ScriptCompilerFlags,
                options.Game,
                options.Debug,
                options.NwscriptPath ?? "(default)",
                workingDirectory));

            return true;
        }

        private static bool IsVendoredIncludeFile(string sourceText)
        {
            string lowerSource = (sourceText ?? string.Empty).ToLowerInvariant();
            return lowerSource.IndexOf("void main()", StringComparison.Ordinal) == -1
                && lowerSource.IndexOf("void main ()", StringComparison.Ordinal) == -1
                && lowerSource.IndexOf("int startingconditional()", StringComparison.Ordinal) == -1
                && lowerSource.IndexOf("int startingconditional ()", StringComparison.Ordinal) == -1;
        }

        private static List<string> BuildLibraryLookupPaths(string tempFolder, string tempScriptDirectory)
        {
            var lookupPaths = new List<string>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Match vendored nwnnsscomp invocation more closely: search the script's
            // own folder first, then the staged tslpatchdata root, without recursively
            // scanning sibling directories for implicit includes.
            AddLookupPath(tempScriptDirectory, lookupPaths, seenPaths);
            AddLookupPath(tempFolder, lookupPaths, seenPaths);

            return lookupPaths;
        }

        internal static bool TryHandleCompilerUnavailable(PatchLogger logger, Exception exception)
        {
            if (!(exception is BadImageFormatException
                || exception is DllNotFoundException
                || exception is FileLoadException
                || exception is MissingMethodException
                || exception is TypeLoadException))
            {
                return false;
            }

            LogCompilerUnavailable(logger, exception);
            return true;
        }

        private static bool ShouldTreatAsCompilerFeedback(Exception exception)
        {
            if (exception is InvalidOperationException
                && exception.Message.IndexOf("Failed to parse nwscript.nss file:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (exception is FileNotFoundException
                && exception.Message.IndexOf("nwscript.nss file not found:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private static void LogCompilerUnavailable(PatchLogger logger, Exception exception)
        {
            if (logger == null || exception == null)
            {
                return;
            }

            string message = string.Format(
                CultureInfo.CurrentCulture,
                PatcherResources.CompileListCompilerUnavailableFormat,
                exception.Message);
            if (logger.Errors.Any(log => string.Equals(log.Message, message, StringComparison.Ordinal)))
            {
                return;
            }

            logger.AddError(message);
        }

        private static void LogCompilerFeedback(PatchLogger logger, string feedback)
        {
            if (logger == null || string.IsNullOrWhiteSpace(feedback))
            {
                return;
            }

            string normalized = feedback.Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (string line in normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                logger.AddVerbose(string.Format(
                    CultureInfo.CurrentCulture,
                    PatcherResources.CompileListCompilerOutputFormat,
                    line));
            }
        }

        private static void AddLookupPath(string candidate, List<string> lookupPaths, HashSet<string> seenPaths)
        {
            if (string.IsNullOrEmpty(candidate) || !Directory.Exists(candidate))
            {
                return;
            }

            string fullPath = Path.GetFullPath(candidate);
            if (seenPaths.Add(fullPath))
            {
                lookupPaths.Add(fullPath);
            }
        }
    }
}
