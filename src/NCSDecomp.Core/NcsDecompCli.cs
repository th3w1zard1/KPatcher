// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Diagnostics;
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
    /// Shared CLI implementation for NCS->NSS (used by NCSDecompCLI and umbrella tools).
    /// </summary>
    public static class NcsDecompCli
    {
        public static int Run(string[] args)
        {
            return Run(args, null);
        }

        public static int Run(string[] args, ILogger appLogger)
        {
            ILogger log = appLogger ?? NullLogger.Instance;
            string correlationId = ToolCorrelation.ReadOptional();
            if (args == null)
            {
                log.LogWarning(
                    "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Message=args was null; using empty argv",
                    DecompPhaseNames.CliParse,
                    correlationId ?? "");
                args = Array.Empty<string>();
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=NCSDecompCLI Operation=decomp Phase={Phase} CorrelationId={CorrelationId} ArgCount={Count}",
                    DecompPhaseNames.CliParse,
                    correlationId ?? "",
                    args.Length);
            }

            string inputPath = null;
            string outputPath = null;
            string game = "k1";
            bool verbose = false;
            bool noConfig = false;
            bool gameFromCli = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i" && i + 1 < args.Length) { inputPath = args[++i]; continue; }
                if (args[i] == "-o" && i + 1 < args.Length) { outputPath = args[++i]; continue; }
                if (args[i] == "-g" && i + 1 < args.Length) { game = args[++i]; gameFromCli = true; continue; }
                if (args[i] == "-v" || args[i] == "--verbose") { verbose = true; continue; }
                if (args[i] == "--no-config") { noConfig = true; continue; }
                if (args[i] == "--help" || args[i] == "-h")
                {
                    PrintHelp();
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug(
                            "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Result=help",
                            DecompPhaseNames.CliParse,
                            correlationId ?? "");
                    }

                    return 0;
                }
            }

            if (verbose && log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Verbose=true",
                    DecompPhaseNames.CliParse,
                    correlationId ?? "");
            }

            if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
            {
                log.LogWarning(
                    "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Message=Missing -i or -o",
                    DecompPhaseNames.CliParse,
                    correlationId ?? "");
                Console.Error.WriteLine("Error: -i <input.ncs> and -o <output.nss> are required. CorrelationId=" + (correlationId ?? "-"));
                PrintHelp();
                return 1;
            }

            if (!File.Exists(inputPath))
            {
                log.LogWarning(
                    "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Input={Input} Message=file not found",
                    DecompPhaseNames.IoReadNcs,
                    correlationId ?? "",
                    ToolPathRedaction.FormatPath(inputPath));
                Console.Error.WriteLine("Error: Input file not found: " + inputPath + " CorrelationId=" + (correlationId ?? "-"));
                return 1;
            }

            var logHuman = NcsDecompLogger.Default;
            try
            {
                if (verbose)
                {
                    logHuman.StartNcsDecompSection();
                }

                NcsDecompSettings settings = null;
                if (!noConfig)
                {
                    settings = NcsDecompSettings.Load(NcsDecompSettings.GetDefaultAppBaseDirectory(), true, log);
                    if (verbose && settings.ConfigLoadedFromPath != null)
                    {
                        logHuman.Info("Config: " + settings.ConfigLoadedFromPath);
                    }

                    if (log.IsEnabled(LogLevel.Debug) && settings != null)
                    {
                        log.LogDebug(
                            "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} ConfigPath={Path}",
                            DecompPhaseNames.CliParse,
                            correlationId ?? "",
                            settings.ConfigLoadedFromPath != null
                                ? ToolPathRedaction.FormatPath(settings.ConfigLoadedFromPath)
                                : "(none)");
                    }
                }
                else if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} NoConfig=true",
                        DecompPhaseNames.CliParse,
                        correlationId ?? "");
                }

                var swIo = Stopwatch.StartNew();
                byte[] ncsBytes = File.ReadAllBytes(inputPath);
                swIo.Stop();
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Bytes={Bytes} Input={Input}",
                        DecompPhaseNames.IoReadNcs,
                        correlationId ?? "",
                        swIo.ElapsedMilliseconds,
                        ncsBytes.Length,
                        ToolPathRedaction.FormatPath(inputPath));
                }

                bool k2;
                if (gameFromCli)
                {
                    k2 = string.Equals(game, "k2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(game, "tsl", StringComparison.OrdinalIgnoreCase);
                    FileDecompilerOptions.IsK2Selected = k2;
                }
                else if (settings != null)
                {
                    k2 = FileDecompilerOptions.IsK2Selected;
                }
                else
                {
                    k2 = string.Equals(game, "k2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(game, "tsl", StringComparison.OrdinalIgnoreCase);
                    FileDecompilerOptions.IsK2Selected = k2;
                }

                if (verbose)
                {
                    logHuman.Info("Input: " + inputPath + " (" + ncsBytes.Length + " bytes), game: " + (k2 ? "TSL" : "K1"));
                }

                ActionsData actions;
                try
                {
                    string k1p = settings != null ? settings.K1NwscriptPath : null;
                    string k2p = settings != null ? settings.K2NwscriptPath : null;
                    actions = ActionsData.LoadForGame(k2, k1p, k2p, log);
                }
                catch (FileNotFoundException ex)
                {
                    log.LogError(
                        ex,
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Message=actions table load failed Detail={Detail}",
                        DecompPhaseNames.DecompDecode,
                        correlationId ?? "",
                        ToolExceptionFormatter.Format(ex, includeStack: false));
                    Console.Error.WriteLine("Error: Embedded nwscript action table not found. Add k1_nwscript.nss / tsl_nwscript.nss under NCSDecomp.Core/Resources/. CorrelationId=" + (correlationId ?? "-"));
                    Console.Error.WriteLine(ToolCliStderr.FormatExceptionOneLiner(ex));
                    return 1;
                }

                var decompiler = new FileDecompiler(actions, log);
                var swDecomp = Stopwatch.StartNew();
                string nss = decompiler.DecompileToNss(ncsBytes);
                swDecomp.Stop();
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} NssChars={Chars}",
                        DecompPhaseNames.DecompPrint,
                        correlationId ?? "",
                        swDecomp.ElapsedMilliseconds,
                        nss.Length);
                }

                Encoding enc = ResolveOutputEncoding(settings, log, correlationId);
                var swWrite = Stopwatch.StartNew();
                File.WriteAllText(outputPath, nss, enc);
                swWrite.Stop();
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Output={Output} Encoding={Enc}",
                        DecompPhaseNames.IoWriteNss,
                        correlationId ?? "",
                        swWrite.ElapsedMilliseconds,
                        ToolPathRedaction.FormatPath(outputPath),
                        enc.WebName);
                }

                if (verbose)
                {
                    logHuman.Success("Wrote NSS: " + outputPath + " (" + nss.Length + " chars)");
                    logHuman.EndSection();
                }
                else
                {
                    log.LogInformation(
                        "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} Output={Output} Chars={Chars} Message=Wrote NSS",
                        DecompPhaseNames.IoWriteNss,
                        correlationId ?? "",
                        ToolPathRedaction.FormatPath(outputPath),
                        nss.Length);
                    Console.WriteLine("Wrote NSS: " + outputPath);
                }

                return 0;
            }
            catch (Exception ex)
            {
                log.LogError(
                    ex,
                    "Tool=NCSDecompCLI Operation=decomp Phase=error CorrelationId={CorrelationId} Detail={Detail}",
                    correlationId ?? "",
                    ToolExceptionFormatter.Format(ex, ToolExceptionFormatter.IncludeStackTraces));
                Console.Error.WriteLine("Error: " + ToolCliStderr.FormatExceptionOneLiner(ex));
                return 1;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("NCSDecomp.NET - KotOR NCS script decompiler (CLI)");
            Console.WriteLine("Usage: NCSDecompCLI -i <input.ncs> -o <output.nss> [-g k1|k2] [-v] [--no-config]");
            Console.WriteLine("  -i    Input .ncs file");
            Console.WriteLine("  -o    Output .nss path");
            Console.WriteLine("  -g    Game: k1 (KotOR 1) or k2 (TSL) — overrides Game Variant in config");
            Console.WriteLine("  -v    Verbose / colored progress on stderr");
            Console.WriteLine("  By default loads config/ncsdecomp.conf next to this exe (Java-compatible keys).");
            Console.WriteLine("  --no-config   Skip config file; use defaults except -g / embedded nwscript.");
        }

        private static Encoding ResolveOutputEncoding(NcsDecompSettings settings, ILogger log, string correlationId)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.EncodingName))
            {
                return new UTF8Encoding(false);
            }

            try
            {
                return Encoding.GetEncoding(settings.EncodingName);
            }
            catch (Exception ex)
            {
                log.LogWarning(
                    ex,
                    "Tool=NCSDecompCLI Phase={Phase} CorrelationId={CorrelationId} RequestedEncoding={Name} Message=Falling back to UTF-8 without BOM",
                    DecompPhaseNames.IoWriteNss,
                    correlationId ?? "",
                    settings.EncodingName);
                return new UTF8Encoding(false);
            }
        }
    }
}
