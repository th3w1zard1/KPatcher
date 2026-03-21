// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
        public static ExternalCompilerRunResult Run(
            string[] argv,
            string workingDirectory,
            IDictionary<string, string> environment,
            int? timeoutMilliseconds)
        {
            if (argv == null || argv.Length == 0)
            {
                throw new ArgumentException("argv must include executable path.", nameof(argv));
            }

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
                        catch
                        {
                            // ignore
                        }

                        throw new TimeoutException("External compiler exceeded timeout (" + timeoutMilliseconds + " ms).");
                    }
                }
                else
                {
                    proc.WaitForExit();
                }

                string stdout = outTask.GetAwaiter().GetResult();
                string stderr = errTask.GetAwaiter().GetResult();
                return new ExternalCompilerRunResult(proc.ExitCode, stdout, stderr);
            }
        }
    }
}
