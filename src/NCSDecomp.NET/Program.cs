// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Diagnostics;
using System.Text;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using NCSDecomp.Core;

namespace NCSDecomp.NET
{
    /// <summary>
    /// CLI entry point for NCS decompilation. Port of DeNCS NCSDecompCLI.
    /// Usage: -i &lt;input.ncs&gt; -o &lt;output.nss&gt; -g k1|k2
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string prevCid = Environment.GetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName);
            if (string.IsNullOrEmpty(prevCid))
            {
                Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, Guid.NewGuid().ToString("N"));
            }

            try
            {
                using (ILoggerFactory factory = LoggerFactory.Create(b =>
                {
                    b.SetMinimumLevel(ToolLogLevel.DefaultMinimumFromEnvironment());
                    ToolHostLogging.AddFileSinkIfConfigured(b);
                    ToolHostLogging.AddSimpleConsoleToStderr(b);
                }))
                {
                    ToolHostLogging.LogHostStartupDebug(factory.CreateLogger("NCSDecompCLI.boot"), "NCSDecompCLI", args);
                    ILogger log = factory.CreateLogger("NCSDecompCLI");
                    log.LogDebug(
                        "Tool=NCSDecompCLI Phase=verb_entry Verb=ncsdecomp CorrelationId={CorrelationId}",
                        ToolCorrelation.ReadOptional() ?? "");
                    try
                    {
                        var swHost = Stopwatch.StartNew();
                        int code = NcsDecompCli.Run(args, log);
                        swHost.Stop();
                        if (code == 0)
                        {
                            log.LogDebug(
                                "Tool=NCSDecompCLI Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode=0",
                                ToolCorrelation.ReadOptional() ?? "",
                                swHost.ElapsedMilliseconds);
                        }
                        else
                        {
                            log.LogWarning(
                                "Tool=NCSDecompCLI Operation=cli_complete Phase=host CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode={ExitCode} Message=non-zero exit from NcsDecompCli",
                                ToolCorrelation.ReadOptional() ?? "",
                                swHost.ElapsedMilliseconds,
                                code);
                        }

                        return code;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(
                            ex,
                            "Tool=NCSDecompCLI unhandled Detail={Detail}",
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
    }
}
