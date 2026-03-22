using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using KCompiler.Diagnostics;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KCompiler
{
    /// <summary>
    /// Cross-platform managed NSS→NCS compilation (no nwnnsscomp.exe).
    /// Use from KPatcher, tools, or the KCompiler.NET CLI.
    /// </summary>
    public static class ManagedNwnnsscomp
    {
        /// <summary>Compile NSS file to NCS (game: 1 = K1, 2 = TSL).</summary>
        public static void CompileFile(
            string sourcePath,
            string outputPath,
            int gameNumber,
            bool debug = false,
            string nwscriptPath = null,
            ILogger logger = null)
        {
            CompileFile(sourcePath, outputPath, GameNumberToGame(gameNumber), debug, nwscriptPath, logger);
        }

        /// <summary>Compile NSS file to NCS; defaults to KOTOR 1.</summary>
        public static void CompileFile(
            string sourcePath,
            string outputPath,
            bool debug = false,
            string nwscriptPath = null,
            ILogger logger = null)
        {
            CompileFile(sourcePath, outputPath, Game.K1, debug, nwscriptPath, logger);
        }

        /// <summary>Compile NSS file to NCS on disk using the same pipeline as KPatcher's inbuilt compiler.</summary>
        public static void CompileFile(
            string sourcePath,
            string outputPath,
            Game game,
            bool debug = false,
            string nwscriptPath = null,
            ILogger logger = null)
        {
            ILogger log = logger ?? NullLogger.Instance;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Source path is required.", nameof(sourcePath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("NSS source not found.", sourcePath);
            }

            string cid = ToolCorrelation.ReadOptional();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Operation=compile Phase={Phase} CorrelationId={CorrelationId} Game={Game} Source={Source} Output={Output} Debug={Debug} Nwscript={Nwscript}",
                    CompilePhaseNames.IoReadNss,
                    cid ?? "",
                    game,
                    ToolPathRedaction.FormatPath(sourcePath),
                    ToolPathRedaction.FormatPath(outputPath),
                    debug,
                    string.IsNullOrEmpty(nwscriptPath) ? "(default)" : ToolPathRedaction.FormatPath(nwscriptPath));
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var swRead = Stopwatch.StartNew();
            byte[] nssData = File.ReadAllBytes(sourcePath);
            swRead.Stop();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Phase={Phase} ElapsedMs={ElapsedMs} Bytes={Bytes}",
                    CompilePhaseNames.IoReadNss,
                    swRead.ElapsedMilliseconds,
                    nssData.Length);
            }

            string nssContents = DecodeBytesWithFallbacks(nssData, log);
            string parentDir = Path.GetDirectoryName(sourcePath);
            var libraryLookup = parentDir != null ? new List<string> { parentDir } : new List<string>();

            var swCompile = Stopwatch.StartNew();
            NCS ncs;
            try
            {
                ncs = NCSAuto.CompileNss(
                    nssContents,
                    game,
                    library: null,
                    optimizers: null,
                    libraryLookup: libraryLookup,
                    errorlog: null,
                    debug: debug,
                    nwscriptPath: nwscriptPath);
            }
            catch (Exception ex)
            {
                swCompile.Stop();
                log.LogError(
                    ex,
                    "Tool=kcompiler Operation=compile Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Detail={Detail}",
                    CompilePhaseNames.CompileParse,
                    cid ?? "",
                    swCompile.ElapsedMilliseconds,
                    ToolExceptionFormatter.Format(ex, includeStack: false));
                throw;
            }

            swCompile.Stop();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Phase={Phase} ElapsedMs={ElapsedMs} Instructions={Count}",
                    CompilePhaseNames.CompileCodegen,
                    swCompile.ElapsedMilliseconds,
                    ncs?.Instructions?.Count ?? 0);
            }

            string outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            var swWrite = Stopwatch.StartNew();
            NCSAuto.WriteNcs(ncs, outputPath);
            swWrite.Stop();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Operation=compile Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Output={Output}",
                    CompilePhaseNames.IoWriteNcs,
                    cid ?? "",
                    swWrite.ElapsedMilliseconds,
                    ToolPathRedaction.FormatPath(outputPath));
                long totalMs = swRead.ElapsedMilliseconds + swCompile.ElapsedMilliseconds + swWrite.ElapsedMilliseconds;
                log.LogDebug(
                    "Tool=kcompiler Operation=compile Phase=compile.pipeline_summary CorrelationId={CorrelationId} ReadMs={ReadMs} CompileMs={CompileMs} WriteMs={WriteMs} TotalMs={TotalMs}",
                    cid ?? "",
                    swRead.ElapsedMilliseconds,
                    swCompile.ElapsedMilliseconds,
                    swWrite.ElapsedMilliseconds,
                    totalMs);
            }
        }

        /// <summary>Compile raw NSS source to NCS bytes (game: 1 = K1, 2 = TSL).</summary>
        public static byte[] CompileSourceToBytes(
            string nssContents,
            int gameNumber,
            IReadOnlyList<string> libraryLookupPaths = null,
            bool debug = false,
            string nwscriptPath = null,
            ILogger logger = null)
        {
            return CompileSourceToBytes(nssContents, GameNumberToGame(gameNumber), libraryLookupPaths, debug, nwscriptPath, logger);
        }

        /// <summary>Compile raw NSS source to NCS bytes.</summary>
        public static byte[] CompileSourceToBytes(
            string nssContents,
            Game game,
            IReadOnlyList<string> libraryLookupPaths = null,
            bool debug = false,
            string nwscriptPath = null,
            ILogger logger = null)
        {
            ILogger log = logger ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Operation=compile_source Phase={Phase} CorrelationId={CorrelationId} Game={Game} Chars={Chars} LookupDirs={LookupCount}",
                    CompilePhaseNames.CompileParse,
                    cid ?? "",
                    game,
                    nssContents?.Length ?? 0,
                    libraryLookupPaths?.Count ?? 0);
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var lookup = libraryLookupPaths != null
                ? new List<string>(libraryLookupPaths)
                : new List<string>();
            var sw = Stopwatch.StartNew();
            try
            {
                NCS ncs = NCSAuto.CompileNss(
                    nssContents,
                    game,
                    library: null,
                    optimizers: null,
                    libraryLookup: lookup,
                    errorlog: null,
                    debug: debug,
                    nwscriptPath: nwscriptPath);
                byte[] outBytes = NCSAuto.BytesNcs(ncs);
                sw.Stop();
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=kcompiler Phase={Phase} ElapsedMs={ElapsedMs} OutBytes={Bytes}",
                        CompilePhaseNames.Optimize,
                        sw.ElapsedMilliseconds,
                        outBytes.Length);
                    log.LogDebug(
                        "Tool=kcompiler Operation=compile_source Phase=compile_source.pipeline_summary CorrelationId={CorrelationId} CompileMs={CompileMs} OutBytes={Bytes} Game={Game}",
                        cid ?? "",
                        sw.ElapsedMilliseconds,
                        outBytes.Length,
                        game);
                }

                return outBytes;
            }
            catch (Exception ex)
            {
                sw.Stop();
                log.LogError(
                    ex,
                    "Tool=kcompiler Operation=compile_source CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Detail={Detail}",
                    cid ?? "",
                    sw.ElapsedMilliseconds,
                    ToolExceptionFormatter.Format(ex, includeStack: false));
                throw;
            }
        }

        private static Game GameNumberToGame(int gameNumber)
        {
            if (gameNumber == 2)
            {
                return Game.K2;
            }

            return Game.K1;
        }

        private static string DecodeBytesWithFallbacks(byte[] data, ILogger log)
        {
            try
            {
                string result = Encoding.UTF8.GetString(data);
                byte[] reencoded = Encoding.UTF8.GetBytes(result);
                if (reencoded.Length == data.Length)
                {
                    bool same = true;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (reencoded[i] != data[i])
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same)
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace(ex, "Tool=kcompiler Phase=nss.decode Message=UTF-8 decode threw; trying fallbacks");
                }
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Tool=kcompiler Phase=nss.decode Message=Using windows-1252 or ASCII after UTF-8 not used as lossless source text");
            }

            try
            {
                string s = Encoding.GetEncoding("windows-1252").GetString(data);
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Tool=kcompiler NSS decode used windows-1252 fallback");
                }

                return s;
            }
            catch
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Tool=kcompiler NSS decode used ASCII fallback");
                }

                return Encoding.ASCII.GetString(data);
            }
        }
    }
}
