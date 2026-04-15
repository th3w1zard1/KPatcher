using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KCompiler.Diagnostics;
using KPatcher.Core.Common;
using Microsoft.Extensions.Logging;

namespace KCompiler.Cli
{
    /// <summary>
    /// Parses command lines compatible with common nwnnsscomp.exe variants (KOTOR Tool, KPatcher, v1, knsscomp).
    /// </summary>
    public sealed class NwnnsscompParseResult
    {
        public bool Success { get; set; }
        public bool IsHelp { get; set; }
        public string ErrorMessage { get; set; }
        public string SourcePath { get; set; }
        public string OutputPath { get; set; }
        public Game Game { get; set; } = Game.K1;
        public bool Debug { get; set; }
        public string NwscriptPath { get; set; }
        /// <summary>True when -d (decompile) was requested. Not implemented in managed code; parser sets error.</summary>
        public bool Decompile { get; set; }
    }

    public static class NwnnsscompCliParser
    {
        public static NwnnsscompParseResult Parse(string[] args)
        {
            return Parse(args, null);
        }

        /// <param name="log">Optional MEL logger; Debug lines use redacted paths and short failure reasons.</param>
        public static NwnnsscompParseResult Parse(string[] args, ILogger log)
        {
            var r = new NwnnsscompParseResult();
            string cid = ToolCorrelation.ReadOptional() ?? "";
            if (args == null)
            {
                log?.LogWarning(
                    "Tool=kcompiler Phase={Phase} CorrelationId={CorrelationId} Message=argv reference was null; treating as empty",
                    CompilePhaseNames.CliParse,
                    cid);
                args = Array.Empty<string>();
            }

            if (log != null && log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=kcompiler Phase={Phase} CorrelationId={CorrelationId} ArgCount={ArgCount}",
                    CompilePhaseNames.CliParse,
                    cid,
                    args.Length);
            }

            if (args.Length == 0)
            {
                r.ErrorMessage = "No arguments. Use: kcompiler -c [options] <source.nss> [-o <out.ncs>]";
                LogParseDebugFail(log, cid, "no_args", null, r);
                return r;
            }

            var list = new List<string>(args);
            if (list.Contains("-h") || list.Contains("--help") || list.Contains("/?"))
            {
                r.Success = true;
                r.IsHelp = true;
                r.ErrorMessage = HelpText();
                LogParseDebugHelp(log, cid);
                return r;
            }

            int cIdx = list.IndexOf("-c");
            int dIdx = list.IndexOf("-d");
            if (cIdx >= 0 && dIdx >= 0)
            {
                r.ErrorMessage = "Cannot use -c (compile) and -d (decompile) together. " + HelpText();
                LogParseDebugFail(log, cid, "mode_conflict_c_and_d", null, r);
                return r;
            }

            if (cIdx < 0 && dIdx < 0)
            {
                r.ErrorMessage = "Compile mode requires -c (decompile -d is not implemented). " + HelpText();
                LogParseDebugFail(log, cid, "no_compile_mode", null, r);
                return r;
            }

            if (dIdx >= 0)
            {
                r.Decompile = true;
                list.RemoveAt(dIdx);
                if (cIdx > dIdx)
                {
                    cIdx--;
                }
            }

            if (cIdx >= 0)
            {
                list.RemoveAt(cIdx);
            }

            string outputDir = null;
            string outputFile = null;
            Game game = Game.K1;
            bool debug = false;
            string nwscript = null;

            for (int i = 0; i < list.Count; i++)
            {
                string a = list[i];
                if (a == "-g" || a == "-game")
                {
                    if (i + 1 >= list.Count)
                    {
                        r.ErrorMessage = "-g requires a value (1 = K1, 2 = TSL).";
                        LogParseDebugFail(log, cid, "g_missing_value", null, r);
                        return r;
                    }

                    string gv = list[i + 1].Trim();
                    list.RemoveAt(i + 1);
                    list.RemoveAt(i);
                    i--;
                    if (gv == "1")
                    {
                        game = Game.K1;
                    }
                    else if (gv == "2")
                    {
                        game = Game.K2;
                    }
                    else
                    {
                        r.ErrorMessage = $"-g must be 1 or 2, got '{gv}'.";
                        LogParseDebugFail(log, cid, "g_invalid", gv, r);
                        return r;
                    }

                    continue;
                }

                if (a == "--outputdir")
                {
                    if (i + 1 >= list.Count)
                    {
                        r.ErrorMessage = "--outputdir requires a directory.";
                        LogParseDebugFail(log, cid, "outputdir_missing", null, r);
                        return r;
                    }

                    outputDir = list[i + 1];
                    list.RemoveAt(i + 1);
                    list.RemoveAt(i);
                    i--;
                    continue;
                }

                if (a == "-o")
                {
                    if (i + 1 >= list.Count)
                    {
                        r.ErrorMessage = "-o requires an output file name or path.";
                        LogParseDebugFail(log, cid, "o_missing", null, r);
                        return r;
                    }

                    outputFile = list[i + 1];
                    list.RemoveAt(i + 1);
                    list.RemoveAt(i);
                    i--;
                    continue;
                }

                if (a == "--debug")
                {
                    debug = true;
                    list.RemoveAt(i);
                    i--;
                    continue;
                }

                if (a == "--nwscript")
                {
                    if (i + 1 >= list.Count)
                    {
                        r.ErrorMessage = "--nwscript requires a path.";
                        LogParseDebugFail(log, cid, "nwscript_missing", null, r);
                        return r;
                    }

                    nwscript = list[i + 1];
                    list.RemoveAt(i + 1);
                    list.RemoveAt(i);
                    i--;
                    continue;
                }

                if (a.StartsWith("-", StringComparison.Ordinal))
                {
                    r.ErrorMessage = $"Unknown option '{a}'. " + HelpText();
                    LogParseDebugFail(log, cid, "unknown_option", a, r);
                    return r;
                }
            }

            // Remaining tokens: positionals
            var pos = new List<string>();
            foreach (string x in list)
            {
                if (!string.IsNullOrWhiteSpace(x))
                {
                    pos.Add(x);
                }
            }

            string source = null;
            string output = null;

            if (!string.IsNullOrEmpty(outputDir) && !string.IsNullOrEmpty(outputFile))
            {
                output = Path.Combine(outputDir, outputFile);
                if (pos.Count >= 1)
                {
                    source = pos[0];
                }
            }
            else if (!string.IsNullOrEmpty(outputDir) && pos.Count >= 1)
            {
                source = pos[0];
                string baseName = Path.GetFileNameWithoutExtension(source) + ".ncs";
                output = Path.Combine(outputDir, baseName);
            }
            else if (!string.IsNullOrEmpty(outputFile))
            {
                output = outputFile;
                if (pos.Count >= 1)
                {
                    source = pos[0];
                }
            }
            else if (pos.Count >= 2)
            {
                // v1 style: -c source.nss out.ncs
                source = pos[0];
                output = pos[1];
            }
            else if (pos.Count == 1)
            {
                source = pos[0];
                output = Path.ChangeExtension(source, ".ncs");
            }

            if (r.Decompile)
            {
                r.ErrorMessage = "Decompile (-d) is not implemented. Use -c to compile.";
                LogParseDebugFail(log, cid, "decompile_not_implemented", null, r);
                return r;
            }

            if (string.IsNullOrEmpty(source))
            {
                r.ErrorMessage = "Could not determine source .nss path. " + HelpText();
                LogParseDebugFail(log, cid, "no_source", null, r);
                return r;
            }

            if (string.IsNullOrEmpty(output))
            {
                r.ErrorMessage = "Could not determine output .ncs path. " + HelpText();
                LogParseDebugFail(log, cid, "no_output", null, r);
                return r;
            }

            r.Success = true;
            r.SourcePath = Path.GetFullPath(source);
            r.OutputPath = Path.GetFullPath(output);
            r.Game = game;
            r.Debug = debug;
            r.NwscriptPath = nwscript;
            LogParseDebugOk(log, cid, r);
            return r;
        }

        private static void LogParseDebugHelp(ILogger log, string correlationId)
        {
            if (log == null || !log.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            log.LogDebug(
                "Tool=kcompiler Phase={Phase} CorrelationId={CorrelationId} Result=help",
                CompilePhaseNames.CliParse,
                correlationId);
        }

        private static void LogParseDebugFail(ILogger log, string correlationId, string reason, string token, NwnnsscompParseResult r)
        {
            if (log == null || !log.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            log.LogDebug(
                "Tool=kcompiler Phase={Phase} CorrelationId={CorrelationId} Result=fail Reason={Reason} Token={Token} UserHint={Hint}",
                CompilePhaseNames.CliParse,
                correlationId,
                reason ?? "",
                token ?? "",
                TruncateOneLine(r.ErrorMessage, 200));
        }

        private static void LogParseDebugOk(ILogger log, string correlationId, NwnnsscompParseResult r)
        {
            if (log == null || !log.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            log.LogDebug(
                "Tool=kcompiler Phase={Phase} CorrelationId={CorrelationId} Result=ok Game={Game} Debug={Debug} Source={Source} Output={Output} Nwscript={Nwscript}",
                CompilePhaseNames.CliParse,
                correlationId,
                r.Game,
                r.Debug,
                ToolPathRedaction.FormatPath(r.SourcePath),
                ToolPathRedaction.FormatPath(r.OutputPath),
                string.IsNullOrEmpty(r.NwscriptPath) ? "(default)" : ToolPathRedaction.FormatPath(r.NwscriptPath));
        }

        private static string TruncateOneLine(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        private static string HelpText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("KCompiler.NET — managed NSS->NCS compiler (nwnnsscomp-compatible).");
            sb.AppendLine();
            sb.AppendLine("Compile (examples):");
            sb.AppendLine("  kcompiler -c script.nss -o script.ncs");
            sb.AppendLine("  kcompiler -c script.nss script.ncs");
            sb.AppendLine("  kcompiler -c --outputdir ./out -o script.ncs -g 2 ./src/script.nss");
            sb.AppendLine();
            sb.AppendLine("Options:");
            sb.AppendLine("  -c         Compile (default).");
            sb.AppendLine("  -d         Decompile (not implemented; nwnnsscomp.exe compatibility).");
            sb.AppendLine("  -g 1|2     Game (1=KOTOR, 2=TSL). Default 1.");
            sb.AppendLine("  --outputdir <dir>   Output directory (KOTOR Tool style).");
            sb.AppendLine("  -o <path>  Output .ncs path or filename.");
            sb.AppendLine("  --debug    Enable compiler debug output.");
            sb.AppendLine("  --nwscript <path>   Optional nwscript.nss for definitions.");
            return sb.ToString();
        }
    }
}
