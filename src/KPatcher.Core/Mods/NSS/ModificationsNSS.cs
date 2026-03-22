using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Compiler;
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

        public new string Action { get; set; } = "Compile";
        public new bool SkipIfNotReplace { get; set; } = true;
        [CanBeNull]
        public string NwnnsscompPath { get; set; }
        [CanBeNull]
        public string TempScriptFolder { get; set; }

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
            string sourceText = Encoding.GetEncoding("windows-1252").GetString(source);
            var mutableSource = new MutableString(sourceText);
            Apply(mutableSource, memory, logger, game);
            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                "ModificationsNSS.PatchResource: after token Apply nssCharLength={0}", mutableSource.Value.Length));

            // Compile the modified NSS source to NCS bytecode
            if (Action.Equals("Compile", StringComparison.OrdinalIgnoreCase))
            {
                string tempFolder = TempScriptFolder is null ? Path.GetTempPath() : TempScriptFolder;
                Directory.CreateDirectory(tempFolder);
                string tempScriptFile = Path.Combine(tempFolder, SourceFile);
                File.WriteAllText(tempScriptFile, mutableSource.Value, Encoding.GetEncoding("windows-1252"));
                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: wrote temp NSS path={0} tempFolder={1} isWindows={2}",
                    tempScriptFile, tempFolder, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));

                // Try built-in compiler first
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                bool builtInSucceeded = false;
                byte[] compiledBytes = null;

                try
                {
                    global::KPatcher.Core.Formats.NCS.NCS ncs = NCSAuto.CompileNss(
                        mutableSource.Value,
                        game,
                        null,
                        null,
                        new List<string> { tempFolder });
                    compiledBytes = NCSAuto.BytesNcs(ncs);
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: built-in compile ok sourceFile={0} ncsBytes={1}", SourceFile, compiledBytes.Length));
                    return compiledBytes;
                }
                catch (EntryPointError e)
                {
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: EntryPointError from built-in compile sourceFile={0} message={1}", SourceFile, e.Message));
                    logger.AddNote(e.Message);
                    return true;
                }
                catch (Exception e)
                {
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: built-in compile exception sourceFile={0} type={1} message={2}",
                        SourceFile, e.GetType().FullName, e.Message));
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.BuiltInCompilationFailedFormat, SourceFile, e.Message));
                }

                // If built-in failed and on Windows, try external compiler
                if (!builtInSucceeded && isWindows)
                {
                    bool nwnnsscompExists = !string.IsNullOrEmpty(NwnnsscompPath) && File.Exists(NwnnsscompPath);
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: external path gate nwnnsscompExists={0} path={1}", nwnnsscompExists, NwnnsscompPath ?? ""));
                    if (!nwnnsscompExists)
                    {
                        logger.AddError(TSLPatcherMessages.NwnnsscompNotFoundInTslPatchData);
                    }
                    else
                    {
                        logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ErrorOccurredWhileCompilingTryingExternalFormat, SourceFile));
                    }

                    if (nwnnsscompExists)
                    {
                        try
                        {
                            var externalCompiler = new ExternalNCSCompiler(NwnnsscompPath);
                            KnownExternalCompilers detectedCompiler;
                            try
                            {
                                detectedCompiler = externalCompiler.GetInfo();
                            }
                            catch (ArgumentException)
                            {
                                detectedCompiler = KnownExternalCompilers.KPATCHER;
                            }

                            if (detectedCompiler != KnownExternalCompilers.KPATCHER)
                            {
                                logger.AddWarning(
                                    "The nwnnsscomp.exe in the tslpatchdata folder is not the expected KPatcher version.\n" +
                                    $"KPatcher has detected that the provided nwnnsscomp.exe is the '{detectedCompiler}' version.\n" +
                                    "KPatcher will compile regardless, but this may not yield the expected result.");
                            }

                            compiledBytes = CompileWithExternal(tempScriptFile, externalCompiler, logger, game);
                            if (compiledBytes != null)
                            {
                                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                    "ModificationsNSS.PatchResource: external compile ok sourceFile={0} ncsBytes={1}", SourceFile, compiledBytes.Length));
                                return compiledBytes;
                            }

                            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                "ModificationsNSS.PatchResource: external compile returned null sourceFile={0}", SourceFile));
                        }
                        catch (Exception e)
                        {
                            logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                                "ModificationsNSS.PatchResource: external compile exception sourceFile={0} type={1} message={2}",
                                SourceFile, e.GetType().FullName, e.Message));
                            logger.AddError(e.Message);
                        }
                    }
                }
                else if (!isWindows)
                {
                    logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                        "ModificationsNSS.PatchResource: non-Windows host; skipping external nwnnsscomp sourceFile={0}", SourceFile));
                    logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.PatchingFromUnixCompilingFormat, SourceFile));
                }

                // Return compiled bytes if built-in succeeded, otherwise return source
                if (compiledBytes != null)
                {
                    return compiledBytes;
                }

                logger.AddDiagnostic(string.Format(CultureInfo.InvariantCulture,
                    "ModificationsNSS.PatchResource: falling back to NSS bytes sourceFile={0} nssCharLength={1}", SourceFile, mutableSource.Value.Length));
                logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotCompileReturningUncompiledFormat, SourceFile));
                return Encoding.GetEncoding("windows-1252").GetBytes(mutableSource.Value);
            }

            // If not compiling, just return the modified source
            byte[] nssOut = Encoding.GetEncoding("windows-1252").GetBytes(mutableSource.Value);
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

        private byte[] CompileWithExternal(
            string tempScriptFile,
            ExternalNCSCompiler nwnnsscompiler,
            PatchLogger logger,
            Game game)
        {
            string tempDir = Path.GetTempPath();
            string tempCompiledFilepath = Path.Combine(tempDir, "temp_script.ncs");

            try
            {
                (string stdout, string stderr) = nwnnsscompiler.CompileScriptWithOutput(tempScriptFile, tempCompiledFilepath, game, 5);
                bool isIncludeFile = stdout.Contains("File is an include file, ignored");
                if (isIncludeFile)
                {
                    return null;
                }

                if (stdout.Trim().Length > 0)
                {
                    foreach (string line in stdout.Split('\n'))
                    {
                        if (line.Trim().Length > 0)
                        {
                            logger.AddVerbose(line);
                        }
                    }
                }

                if (stderr.Trim().Length > 0)
                {
                    foreach (string line in stderr.Split('\n'))
                    {
                        if (line.Trim().Length > 0)
                        {
                            logger.AddError($"nwnnsscomp error: {line}");
                        }
                    }
                }

                if (File.Exists(tempCompiledFilepath))
                {
                    return File.ReadAllBytes(tempCompiledFilepath);
                }

                return null;
            }
            catch (ExternalNCSCompiler.EntryPointException)
            {
                return null;
            }
        }
    }
}
