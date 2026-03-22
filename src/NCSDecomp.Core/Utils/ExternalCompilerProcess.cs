// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NCSDecomp.Core.Utils
{
    /// <summary>Result of running an external nwnnsscomp / ncsdis process.</summary>
    public sealed class ExternalCompilerRunResult
    {
        public ExternalCompilerRunResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }

        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }

    /// <summary>Runs external toolchain with redirected I/O (optional timeout).</summary>
    public static class ExternalCompilerProcess
    {
        /// <param name="argv">argv[0] = executable path.</param>
        /// <param name="workingDirectory">Process working directory.</param>
        /// <param name="environment">Optional extra/override env vars.</param>
        /// <param name="timeoutMilliseconds">Null = wait indefinitely.</param>
        /// <param name="logger">Optional structured log (defaults to no-op).</param>
        public static ExternalCompilerRunResult Run(
            string[] argv,
            string workingDirectory,
            IDictionary<string, string> environment,
            int? timeoutMilliseconds,
            ILogger logger = null)
        {
            ILogger log = logger ?? NullLogger.Instance;
            if (argv == null || argv.Length == 0)
            {
                throw new ArgumentException("argv must include executable path.", nameof(argv));
            }

            string cid = ToolCorrelation.ReadOptional();
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=ExternalCompiler Phase=start CorrelationId={CorrelationId} Exe={Exe} ArgCount={Count} Cwd={Cwd} TimeoutMs={Timeout}",
                    cid ?? "",
                    ToolPathRedaction.FormatPath(argv[0]),
                    argv.Length,
                    ToolPathRedaction.FormatPath(workingDirectory ?? Environment.CurrentDirectory),
                    timeoutMilliseconds?.ToString() ?? "none");
            }

            var sw = Stopwatch.StartNew();
            var psi = new ProcessStartInfo
            {
                FileName = argv[0],
                WorkingDirectory = string.IsNullOrEmpty(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            for (int i = 1; i < argv.Length; i++)
            {
                psi.ArgumentList.Add(argv[i] ?? string.Empty);
            }

            if (environment != null)
            {
                foreach (var kv in environment)
                {
                    if (kv.Key != null)
                    {
                        psi.Environment[kv.Key] = kv.Value ?? string.Empty;
                    }
                }
            }

            using (var proc = Process.Start(psi))
            {
                if (proc == null)
                {
                    log.LogError(
                        "Tool=ExternalCompiler CorrelationId={CorrelationId} Message=Failed to start process Exe={Exe}",
                        cid ?? "",
                        ToolPathRedaction.FormatPath(argv[0]));
                    throw new IOException("Failed to start process: " + argv[0]);
                }

                Task<string> outTask = Task.Run(() => proc.StandardOutput.ReadToEnd());
                Task<string> errTask = Task.Run(() => proc.StandardError.ReadToEnd());

                if (timeoutMilliseconds.HasValue)
                {
                    if (!proc.WaitForExit(timeoutMilliseconds.Value))
                    {
                        try
                        {
                            proc.Kill(true);
                        }
                        catch (Exception killEx)
                        {
                            if (log.IsEnabled(LogLevel.Debug))
                            {
                                log.LogDebug(
                                    killEx,
                                    "Tool=ExternalCompiler CorrelationId={CorrelationId} Message=Kill after timeout threw",
                                    cid ?? "");
                            }
                        }

                        sw.Stop();
                        log.LogWarning(
                            "Tool=ExternalCompiler CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} Message=timeout; process killed TimeoutMs={Timeout}",
                            cid ?? "",
                            sw.ElapsedMilliseconds,
                            timeoutMilliseconds);
                        throw new TimeoutException("External compiler exceeded timeout (" + timeoutMilliseconds + " ms).");
                    }
                }
                else
                {
                    proc.WaitForExit();
                }

                string stdout = outTask.GetAwaiter().GetResult();
                string stderr = errTask.GetAwaiter().GetResult();
                sw.Stop();
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug(
                        "Tool=ExternalCompiler Phase=done CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} ExitCode={Code} OutLen={OutLen} ErrLen={ErrLen}",
                        cid ?? "",
                        sw.ElapsedMilliseconds,
                        proc.ExitCode,
                        stdout.Length,
                        stderr.Length);
                }

                if (proc.ExitCode != 0 && log.IsEnabled(LogLevel.Warning))
                {
                    string errPreview = (stderr ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
                    if (errPreview.Length > 400)
                    {
                        errPreview = errPreview.Substring(0, 400) + "...";
                    }

                    log.LogWarning(
                        "Tool=ExternalCompiler Phase=done CorrelationId={CorrelationId} ExitCode={Code} Exe={Exe} Message=non-zero exit StderrPreview={Preview}",
                        cid ?? "",
                        proc.ExitCode,
                        ToolPathRedaction.FormatPath(argv[0]),
                        string.IsNullOrEmpty(errPreview) ? "(empty)" : errPreview);
                }

                return new ExternalCompilerRunResult(proc.ExitCode, stdout, stderr);
            }
        }
    }
}
