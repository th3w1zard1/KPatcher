using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using KCompiler;
using KCompiler.Cli;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using NCSDecomp.Core;

namespace KEditChanges.Net
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                using (ILoggerFactory lf = CreateLoggerFactory())
                {
                    lf.CreateLogger("keditchanges-cli.root").LogDebug(
                        "Tool=keditchanges-cli Phase=root_dispatch Result=help ArgCount={ArgCount}",
                        args.Length);
                }

                PrintRootHelp();
                return 0;
            }

            if (args[0] == "--version" || args[0] == "-V")
            {
                using (ILoggerFactory lf = CreateLoggerFactory())
                {
                    lf.CreateLogger("keditchanges-cli.root").LogDebug(
                        "Tool=keditchanges-cli Phase=root_dispatch Result=version ArgCount={ArgCount}",
                        args.Length);
                }

                PrintVersion();
                return 0;
            }

            string verb = args[0];
            string[] rest = new string[args.Length - 1];
            if (rest.Length > 0)
            {
                Array.Copy(args, 1, rest, 0, rest.Length);
            }

            switch (verb)
            {
                case "compile":
                case "kcompiler":
                    return RunWithCorrelation(() => RunKCompiler(rest));
                case "ncsdecomp":
                case "decomp":
                    return RunWithCorrelation(() => RunNcsDecomp(rest));
                case "info":
                {
                    using (ILoggerFactory lf = CreateLoggerFactory())
                    {
                        ILogger log = lf.CreateLogger("keditchanges-cli");
                        log.LogDebug(
                            "Tool=keditchanges-cli Phase=verb_entry Verb=info CorrelationId={CorrelationId}",
                            ToolCorrelation.ReadOptional() ?? "(unset)");
                        log.LogDebug(
                            "Tool=keditchanges-cli Operation=info Verb=info CorrelationId={CorrelationId}",
                            ToolCorrelation.ReadOptional() ?? "(unset)");
                        log.LogInformation(
                            "Tool=keditchanges-cli Operation=info Message={Message}",
                            KEditChanges.ChangeEditPlaceholder.Info);
                        Console.WriteLine(KEditChanges.ChangeEditPlaceholder.Info);
                    }

                    return 0;
                }
                default:
                {
                    using (ILoggerFactory lf = CreateLoggerFactory())
                    {
                        ILogger log = lf.CreateLogger("keditchanges-cli");
                        log.LogWarning("Tool=keditchanges-cli Phase=dispatch Message=unknown verb {Verb}", verb);
                    }

                    Console.Error.WriteLine("Error: Unknown command: " + verb);
                    PrintRootHelp();
                    return 1;
                }
            }
        }

        private static int RunWithCorrelation(Func<int> inner)
        {
            string prev = Environment.GetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName);
            string cid = Guid.NewGuid().ToString("N");
            Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, cid);
            var sw = Stopwatch.StartNew();
            try
            {
                int exit = inner();
                sw.Stop();
                using (ILoggerFactory lf = CreateLoggerFactory())
                {
                    ILogger hostLog = lf.CreateLogger("keditchanges-cli.host");
                    hostLog.LogDebug(
                        "Tool=keditchanges-cli Operation=verb_complete CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode={Exit}",
                        cid,
                        sw.ElapsedMilliseconds,
                        exit);
                    if (exit != 0)
                    {
                        hostLog.LogWarning(
                            "Tool=keditchanges-cli Operation=verb_complete CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode={Exit} Message=non-zero umbrella verb exit (see nested tool logs)",
                            cid,
                            sw.ElapsedMilliseconds,
                            exit);
                    }
                }

                return exit;
            }
            finally
            {
                if (string.IsNullOrEmpty(prev))
                {
                    Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, null);
                }
                else
                {
                    Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, prev);
                }
            }
        }

        private static void PrintRootHelp()
        {
            Console.WriteLine("keditchanges-cli — umbrella CLI (KEditChanges + KCompiler + NCS decompile)");
            Console.WriteLine();
            Console.WriteLine("Usage: keditchanges-cli <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  compile | kcompiler   NSS→NCS (same flags as standalone kcompiler / nwnnsscomp).");
            Console.WriteLine("  ncsdecomp | decomp    NCS→NSS (same flags as NCSDecompCLI: -i -o [-g] ...).");
            Console.WriteLine("  info                  Show KEditChanges library status.");
            Console.WriteLine("  -h, --help            Show this help.");
            Console.WriteLine("  -V, --version         Print tool version.");
        }

        private static void PrintVersion()
        {
            Assembly a = typeof(Program).Assembly;
            string informational = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                Console.WriteLine("keditchanges-cli " + informational);
                return;
            }

            Version v = a.GetName().Version;
            Console.WriteLine("keditchanges-cli " + (v != null ? v.ToString(3) : "0.0.0"));
        }

        private static int RunKCompiler(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            using (ILoggerFactory factory = CreateLoggerFactory())
            {
                ILogger log = factory.CreateLogger("keditchanges-cli.compile");
                try
                {
                    NwnnsscompParseResult parsed = NwnnsscompCliParser.Parse(args, log);
                    if (parsed.IsHelp)
                    {
                        Console.Out.WriteLine(parsed.ErrorMessage.TrimEnd());
                        return 0;
                    }

                    if (!parsed.Success)
                    {
                        if (!string.IsNullOrEmpty(parsed.ErrorMessage))
                        {
                            log.LogWarning("Tool=keditchanges-cli.compile Phase=cli.parse Message={Message}", parsed.ErrorMessage.TrimEnd());
                            Console.Error.WriteLine(parsed.ErrorMessage.TrimEnd());
                        }

                        return 1;
                    }

                    var swCompile = Stopwatch.StartNew();
                    ManagedNwnnsscomp.CompileFile(
                        parsed.SourcePath,
                        parsed.OutputPath,
                        parsed.Game,
                        parsed.Debug,
                        parsed.NwscriptPath,
                        log);
                    swCompile.Stop();
                    log.LogInformation(
                        "Tool=keditchanges-cli.compile Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode=0",
                        ToolCorrelation.ReadOptional() ?? "",
                        swCompile.ElapsedMilliseconds);
                    return 0;
                }
                catch (Exception ex)
                {
                    log.LogError(
                        ex,
                        "Tool=keditchanges-cli.compile unhandled Detail={Detail}",
                        ToolExceptionFormatter.Format(ex, ToolExceptionFormatter.IncludeStackTraces));
                    Console.Error.WriteLine("Error: " + ToolCliStderr.FormatExceptionOneLiner(ex));
                    return 1;
                }
            }
        }

        private static int RunNcsDecomp(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            using (ILoggerFactory factory = CreateLoggerFactory())
            {
                ILogger log = factory.CreateLogger("keditchanges-cli.ncsdecomp");
                log.LogDebug(
                    "Tool=keditchanges-cli Phase=verb_entry Verb=ncsdecomp CorrelationId={CorrelationId}",
                    ToolCorrelation.ReadOptional() ?? "");
                try
                {
                    var swDecomp = Stopwatch.StartNew();
                    int code = NcsDecompCli.Run(args, log);
                    swDecomp.Stop();
                    if (code == 0)
                    {
                        log.LogInformation(
                            "Tool=keditchanges-cli.ncsdecomp Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode=0",
                            ToolCorrelation.ReadOptional() ?? "",
                            swDecomp.ElapsedMilliseconds);
                    }
                    else
                    {
                        log.LogWarning(
                            "Tool=keditchanges-cli.ncsdecomp Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode={ExitCode} Message=non-zero exit from NcsDecompCli",
                            ToolCorrelation.ReadOptional() ?? "",
                            swDecomp.ElapsedMilliseconds,
                            code);
                    }

                    return code;
                }
                catch (Exception ex)
                {
                    log.LogError(
                        ex,
                        "Tool=keditchanges-cli.ncsdecomp unhandled Detail={Detail}",
                        ToolExceptionFormatter.Format(ex, ToolExceptionFormatter.IncludeStackTraces));
                    Console.Error.WriteLine("Error: " + ToolCliStderr.FormatExceptionOneLiner(ex));
                    return 1;
                }
            }
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            ILoggerFactory factory = LoggerFactory.Create(b =>
            {
                b.SetMinimumLevel(ToolLogLevel.DefaultMinimumFromEnvironment());
                ToolHostLogging.AddFileSinkIfConfigured(b);
                ToolHostLogging.AddSimpleConsoleToStderr(b);
            });
            ToolHostLogging.LogHostStartupDebug(factory.CreateLogger("keditchanges-cli.boot"), "keditchanges-cli", Environment.GetCommandLineArgs());
            return factory;
        }
    }
}
