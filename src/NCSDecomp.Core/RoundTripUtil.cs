// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS RoundTripUtil.java — managed NCS→NSS helpers (Java used FileDecompiler + temp files).

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using KCompiler;
using KCompiler.Diagnostics;
using KPatcher.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NCSDecomp.Core.Diagnostics;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Result of <see cref="RoundTripUtil.CompareManagedRecompileToOriginalDecoderText"/> (managed KCompiler recompile vs original NCS).
    /// </summary>
    public sealed class ManagedRoundTripCompareResult
    {
        public ManagedRoundTripCompareResult(bool compileSucceeded, bool decoderOutputsMatch, string summary)
        {
            CompileSucceeded = compileSucceeded;
            DecoderOutputsMatch = decoderOutputsMatch;
            Summary = summary ?? string.Empty;
        }

        /// <summary>True if <see cref="KCompiler.ManagedNwnnsscomp.CompileSourceToBytes"/> produced NCS bytes.</summary>
        public bool CompileSucceeded { get; }

        /// <summary>True if decoder token text for original and recompiled NCS are identical (ignores 8 vs 13-byte file header differences).</summary>
        public bool DecoderOutputsMatch { get; }

        /// <summary>Human-readable status for logs or UI.</summary>
        public string Summary { get; }
    }

    /// <summary>
    /// Shared round-trip helpers: decompile NCS to NSS with the same game flag semantics as DeNCS Java / HoloPatcher
    /// (<c>k1</c>, <c>k2</c>, <c>tsl</c>, <c>2</c> for TSL). Matches responsibilities of BioWare.NET and KOTORModSync
    /// <c>RoundTripUtil</c> (path-based decompile), plus <see cref="CompareManagedRecompileToOriginalDecoderText"/> for
    /// managed KCompiler recompile vs decoder token stream (no external <c>nwnnsscomp</c>).
    /// </summary>
    public static class RoundTripUtil
    {
        /// <summary>
        /// Decompile <paramref name="ncsFilePath"/> to NSS source. <paramref name="gameFlag"/> is <c>k1</c>, <c>k2</c>, <c>tsl</c>, or <c>2</c>.
        /// </summary>
        /// <returns>Null if the NCS file is missing; otherwise decompiled text.</returns>
        /// <exception cref="DecompilerException">Actions load or decompile failure.</exception>
        public static string DecompileNcsToNss(string ncsFilePath, string gameFlag, string k1NwscriptPath = null, string k2NwscriptPath = null, ILogger log = null)
        {
            ILogger loggerEarly = log ?? NullLogger.Instance;
            string cidEarly = ToolCorrelation.ReadOptional() ?? string.Empty;
            if (string.IsNullOrEmpty(ncsFilePath) || !File.Exists(ncsFilePath))
            {
                if (loggerEarly.IsEnabled(LogLevel.Debug))
                {
                    loggerEarly.LogDebug(
                        "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Result=skipped Message=ncs missing or empty path Path={Path}",
                        DecompPhaseNames.IoReadNcs,
                        cidEarly,
                        string.IsNullOrEmpty(ncsFilePath) ? "(null)" : ToolPathRedaction.FormatPath(ncsFilePath));
                }

                return null;
            }

            bool wasK2 = FileDecompilerOptions.IsK2Selected;
            try
            {
                bool k2 = ParseK2(gameFlag);
                FileDecompilerOptions.IsK2Selected = k2;
                ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath, log);
                var decompiler = new FileDecompiler(actions, log);
                var sw = Stopwatch.StartNew();
                byte[] bytes = File.ReadAllBytes(ncsFilePath);
                sw.Stop();
                ILogger logger = log ?? NullLogger.Instance;
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Path={Path} ElapsedMs={ElapsedMs} Bytes={Bytes}",
                        DecompPhaseNames.IoReadNcs,
                        ToolCorrelation.ReadOptional() ?? string.Empty,
                        ToolPathRedaction.FormatPath(ncsFilePath),
                        sw.ElapsedMilliseconds,
                        bytes.Length);
                }

                return decompiler.DecompileToNss(bytes);
            }
            catch (FileNotFoundException ex)
            {
                ILogger lx = log ?? NullLogger.Instance;
                lx.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=actions or embedded resource missing before decompile",
                    DecompPhaseNames.ActionsLoad,
                    ToolCorrelation.ReadOptional() ?? string.Empty);
                throw new DecompilerException("Failed to load actions data: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                ILogger lx = log ?? NullLogger.Instance;
                lx.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=decompile path failed",
                    DecompPhaseNames.DecompDecode,
                    ToolCorrelation.ReadOptional() ?? string.Empty);
                throw new DecompilerException("Failed to decompile: " + ex.Message, ex);
            }
            finally
            {
                FileDecompilerOptions.IsK2Selected = wasK2;
            }
        }

        /// <summary>
        /// Decompile NCS to an NSS file (creates parent directories).
        /// </summary>
        /// <exception cref="DecompilerException">Missing input, failed decompile, or no output written.</exception>
        public static void DecompileNcsToNssFile(
            string ncsFilePath,
            string nssOutputPath,
            string gameFlag,
            Encoding charset,
            string k1NwscriptPath = null,
            string k2NwscriptPath = null,
            ILogger log = null)
        {
            ILogger loggerFile = log ?? NullLogger.Instance;
            string cidFile = ToolCorrelation.ReadOptional() ?? string.Empty;
            if (string.IsNullOrEmpty(ncsFilePath) || !File.Exists(ncsFilePath))
            {
                if (loggerFile.IsEnabled(LogLevel.Debug))
                {
                    loggerFile.LogDebug(
                        "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Result=fail Message=NCS missing Path={Path}",
                        DecompPhaseNames.IoReadNcs,
                        cidFile,
                        string.IsNullOrEmpty(ncsFilePath) ? "(null)" : ToolPathRedaction.FormatPath(ncsFilePath));
                }

                throw new DecompilerException("NCS file does not exist: " + (ncsFilePath ?? "null"));
            }

            if (charset == null)
            {
                charset = new UTF8Encoding(false);
            }

            bool wasK2 = FileDecompilerOptions.IsK2Selected;
            try
            {
                bool k2 = ParseK2(gameFlag);
                FileDecompilerOptions.IsK2Selected = k2;

                string parent = Path.GetDirectoryName(nssOutputPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath, log);
                var decompiler = new FileDecompiler(actions, log);
                byte[] bytes = File.ReadAllBytes(ncsFilePath);
                string nss = decompiler.DecompileToNss(bytes);
                File.WriteAllText(nssOutputPath, nss, charset);
                ILogger logger = log ?? NullLogger.Instance;
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} NssPath={Path} Chars={Chars}",
                        DecompPhaseNames.IoWriteNss,
                        ToolCorrelation.ReadOptional() ?? string.Empty,
                        ToolPathRedaction.FormatPath(nssOutputPath),
                        nss.Length);
                }

                if (!File.Exists(nssOutputPath))
                {
                    throw new DecompilerException("Decompile did not produce output file: " + nssOutputPath);
                }
            }
            catch (DecompilerException)
            {
                throw;
            }
            catch (FileNotFoundException ex)
            {
                loggerFile.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=actions or embedded resource missing (file decompile)",
                    DecompPhaseNames.ActionsLoad,
                    cidFile);
                throw new DecompilerException("Failed to load actions data: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                loggerFile.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=decompile to nss file failed",
                    DecompPhaseNames.DecompDecode,
                    cidFile);
                throw new DecompilerException("Failed to decompile file: " + ex.Message, ex);
            }
            finally
            {
                FileDecompilerOptions.IsK2Selected = wasK2;
            }
        }

        /// <summary>
        /// After external compile, the NCS usually sits beside the NSS with the same base name — decompile that NCS.
        /// </summary>
        public static string GetRoundTripDecompiledCode(string savedNssFilePath, string gameFlag, string k1NwscriptPath = null, string k2NwscriptPath = null, ILogger log = null)
        {
            ILogger logger = log ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional() ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(savedNssFilePath) || !File.Exists(savedNssFilePath))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=NSS path missing or not found Path={Path}",
                            DecompPhaseNames.RoundTripGetSiblingNcs,
                            cid,
                            string.IsNullOrEmpty(savedNssFilePath) ? "(null)" : ToolPathRedaction.FormatPath(savedNssFilePath));
                    }

                    return null;
                }

                string dir = Path.GetDirectoryName(savedNssFilePath);
                string baseName = Path.GetFileNameWithoutExtension(savedNssFilePath);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=invalid NSS path structure",
                            DecompPhaseNames.RoundTripGetSiblingNcs,
                            cid);
                    }

                    return null;
                }

                string recompiledNcs = Path.Combine(dir, baseName + ".ncs");
                if (!File.Exists(recompiledNcs))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=no sibling NCS beside NSS Path={Path}",
                            DecompPhaseNames.RoundTripGetSiblingNcs,
                            cid,
                            ToolPathRedaction.FormatPath(recompiledNcs));
                    }

                    return null;
                }

                return DecompileNcsToNss(recompiledNcs, gameFlag, k1NwscriptPath, k2NwscriptPath, logger);
            }
            catch (DecompilerException ex)
            {
                logger.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=decompile sibling NCS failed",
                    DecompPhaseNames.RoundTripGetSiblingNcs,
                    cid);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=get round-trip decompiled code failed",
                    DecompPhaseNames.RoundTripGetSiblingNcs,
                    cid);
                return null;
            }
        }

        /// <summary>Overload using paths from <see cref="NcsDecompSettings"/>.</summary>
        public static string DecompileNcsToNss(string ncsFilePath, string gameFlag, NcsDecompSettings settings, ILogger log = null)
        {
            if (settings == null)
            {
                return DecompileNcsToNss(ncsFilePath, gameFlag, null, null, log);
            }

            return DecompileNcsToNss(ncsFilePath, gameFlag, settings.K1NwscriptPath, settings.K2NwscriptPath, log);
        }

        /// <summary>
        /// Recompile <paramref name="nss"/> with the managed compiler and compare
        /// <see cref="NcsParsePipeline.DecodeToTokenStream"/> output to that of <paramref name="originalNcs"/>.
        /// File layouts (8- vs 13-byte NCS headers) can differ while decoder text still matches.
        /// </summary>
        /// <remarks>
        /// <paramref name="k1NwscriptPath"/> / <paramref name="k2NwscriptPath"/> are passed to both
        /// <see cref="ActionsData.LoadForGame"/> and <see cref="ManagedNwnnsscomp.CompileSourceToBytes"/>.
        /// BioWare-style <c>k1_nwscript.nss</c> on disk is valid for DeNCS action tables but may not parse as KCompiler
        /// nwscript; use <see langword="null"/> paths here and on-disk nwscript only for
        /// <see cref="DecompileNcsToNss(string,string,string,string)"/> (see <c>NcsDecompNetStyleRoundTripTests</c>).
        /// </remarks>
        public static ManagedRoundTripCompareResult CompareManagedRecompileToOriginalDecoderText(
            byte[] originalNcs,
            string nss,
            bool k2,
            string k1NwscriptPath = null,
            string k2NwscriptPath = null,
            ILogger log = null)
        {
            ILogger logger = log ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional() ?? string.Empty;
            if (originalNcs == null || originalNcs.Length == 0)
            {
                logger.LogDebug(
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=no original bytes",
                    DecompPhaseNames.RoundTripCompile,
                    cid);
                return new ManagedRoundTripCompareResult(false, false, "No original NCS bytes.");
            }

            if (string.IsNullOrEmpty(nss))
            {
                logger.LogDebug(
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} Message=no nss text",
                    DecompPhaseNames.RoundTripCompile,
                    cid);
                return new ManagedRoundTripCompareResult(false, false, "No NSS text to recompile.");
            }

            ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath, logger);
            Game game = k2 ? Game.K2 : Game.K1;
            string nwscriptPath = k2 ? k2NwscriptPath : k1NwscriptPath;

            byte[] recompiled;
            var swCompile = Stopwatch.StartNew();
            try
            {
                recompiled = ManagedNwnnsscomp.CompileSourceToBytes(nss, game, null, false, nwscriptPath, logger);
            }
            catch (Exception ex)
            {
                swCompile.Stop();
                logger.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Message=managed compile failed",
                    DecompPhaseNames.RoundTripCompile,
                    cid,
                    swCompile.ElapsedMilliseconds);
                return new ManagedRoundTripCompareResult(false, false, "Managed compile failed: " + ex.Message);
            }

            swCompile.Stop();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} OutBytes={Bytes}",
                    DecompPhaseNames.RoundTripCompile,
                    cid,
                    swCompile.ElapsedMilliseconds,
                    recompiled.Length);
            }

            string decOrig;
            string decRound;
            var swDec = Stopwatch.StartNew();
            try
            {
                decOrig = NcsParsePipeline.DecodeToTokenStream(originalNcs, actions);
                decRound = NcsParsePipeline.DecodeToTokenStream(recompiled, actions);
            }
            catch (Exception ex)
            {
                swDec.Stop();
                logger.LogWarning(
                    ex,
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Message=decode for compare failed",
                    DecompPhaseNames.RoundTripDecode,
                    cid,
                    swDec.ElapsedMilliseconds);
                return new ManagedRoundTripCompareResult(true, false, "Decode for compare failed: " + ex.Message);
            }

            swDec.Stop();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} OrigChars={Orig} RoundChars={Round}",
                    DecompPhaseNames.RoundTripDecode,
                    cid,
                    swDec.ElapsedMilliseconds,
                    decOrig?.Length ?? 0,
                    decRound?.Length ?? 0);
            }

            if (decOrig == decRound)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} DecoderMatch=true",
                        DecompPhaseNames.RoundTripDecode,
                        cid);
                }

                return new ManagedRoundTripCompareResult(true, true, "Decoder token streams match (managed round-trip).");
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Tool=RoundTripUtil Phase={Phase} CorrelationId={CorrelationId} DecoderMatch=false OrigLen={Orig} RoundLen={Round}",
                    DecompPhaseNames.RoundTripDecode,
                    cid,
                    decOrig?.Length ?? 0,
                    decRound?.Length ?? 0);
            }

            return new ManagedRoundTripCompareResult(true, false, BuildDecoderTextDiffSummary(decOrig, decRound));
        }

        private static string BuildDecoderTextDiffSummary(string a, string b)
        {
            if (a == null)
            {
                a = string.Empty;
            }

            if (b == null)
            {
                b = string.Empty;
            }

            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int i = 0;
            for (; i < minLen; i++)
            {
                if (a[i] != b[i])
                {
                    break;
                }
            }

            var sb = new StringBuilder();
            if (i < minLen)
            {
                sb.Append("Decoder outputs differ at char ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                sb.Append(" (lengths ");
                sb.Append(a.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(" vs ");
                sb.Append(b.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append("). ");
                AppendSnippet(sb, a, i);
                sb.Append(" | ");
                AppendSnippet(sb, b, i);
            }
            else if (a.Length != b.Length)
            {
                sb.Append("Decoder outputs match for ");
                sb.Append(minLen.ToString(CultureInfo.InvariantCulture));
                sb.Append(" chars but lengths differ (");
                sb.Append(a.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(" vs ");
                sb.Append(b.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(").");
            }
            else
            {
                sb.Append("Decoder outputs differ (unexpected).");
            }

            return sb.ToString();
        }

        private static void AppendSnippet(StringBuilder sb, string s, int index)
        {
            int start = index > 48 ? index - 48 : 0;
            int maxLen = 96;
            int take = s.Length - start;
            if (take > maxLen)
            {
                take = maxLen;
            }

            string part = s.Substring(start, take).Replace("\r", "\\r").Replace("\n", "\\n");
            sb.Append('[');
            sb.Append(part);
            if (start + take < s.Length)
            {
                sb.Append('…');
            }

            sb.Append(']');
        }

        private static bool ParseK2(string gameFlag)
        {
            if (string.IsNullOrEmpty(gameFlag))
            {
                return false;
            }

            string g = gameFlag.Trim().ToLowerInvariant();
            return g == "k2" || g == "tsl" || g == "2";
        }

        private static ActionsData LoadActions(bool k2, string k1Path, string k2Path, ILogger log = null)
        {
            return ActionsData.LoadForGame(k2, k1Path, k2Path, log);
        }
    }
}
