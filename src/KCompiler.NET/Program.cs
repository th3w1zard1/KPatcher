using System;
using System.Diagnostics;
using System.Text;
using KCompiler;
using KCompiler.Cli;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KCompiler.Net
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string prevCid = Environment.GetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName);
            if (string.IsNullOrEmpty(prevCid))
            {
                Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, Guid.NewGuid().ToString("N"));
            }

            try
            {
                using (ILoggerFactory factory = CreateLoggerFactory())
                {
                    ToolHostLogging.LogHostStartupDebug(factory.CreateLogger("kcompiler.boot"), "kcompiler", args);
                    ILogger log = factory.CreateLogger("kcompiler");
                    log.LogDebug(
                        "Tool=kcompiler Phase=verb_entry Verb=compile CorrelationId={CorrelationId}",
                        ToolCorrelation.ReadOptional() ?? "");
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
                                log.LogWarning("Tool=kcompiler Phase=cli.parse Message={Message}", parsed.ErrorMessage.TrimEnd());
                                Console.Error.WriteLine(parsed.ErrorMessage.TrimEnd());
                            }

                            return 1;
                        }

                        var swHost = Stopwatch.StartNew();
                        ManagedNwnnsscomp.CompileFile(
                            parsed.SourcePath,
                            parsed.OutputPath,
                            parsed.Game,
                            parsed.Debug,
                            parsed.NwscriptPath,
                            log);
                        swHost.Stop();
                        log.LogInformation(
                            "Tool=kcompiler Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode=0",
                            ToolCorrelation.ReadOptional() ?? "",
                            swHost.ElapsedMilliseconds);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(
                            ex,
                            "Tool=kcompiler unhandled Detail={Detail}",
                            ToolExceptionFormatter.Format(ex, ToolExceptionFormatter.IncludeStackTraces));
                        Console.Error.WriteLine("Error: " + ToolCliStderr.FormatExceptionOneLiner(ex));
                        return 1;
                    }
                }
            }
            finally
            {
                if (string.IsNullOrEmpty(prevCid))
                {
                    Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, null);
                }
            }
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            return LoggerFactory.Create(b =>
            {
                b.SetMinimumLevel(ToolLogLevel.DefaultMinimumFromEnvironment());
                ToolHostLogging.AddFileSinkIfConfigured(b);
                ToolHostLogging.AddSimpleConsoleToStderr(b);
            });
        }
    }
}
