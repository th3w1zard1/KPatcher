using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace KCompiler.Diagnostics
{
    /// <summary>Shared CLI / tool host logging: optional file sink + simple console on stderr (so CI can capture MEL alongside tool errors).</summary>
    public static class ToolHostLogging
    {
        /// <summary>Env <c>KPATCHER_TOOL_LOG_FILE</c>: append structured lines to this path (UTF-8).</summary>
        public static void AddFileSinkIfConfigured(ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            string path = Environment.GetEnvironmentVariable("KPATCHER_TOOL_LOG_FILE");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                builder.AddProvider(new SimpleFileLoggerProvider(path.Trim()));
            }
            catch
            {
                // Invalid path: hosts still run; user fixes env.
            }
        }

        /// <summary>Simple one-line console formatter; all levels go to stderr so stdout stays clean for pipe-friendly tool output.</summary>
        public static void AddSimpleConsoleToStderr(ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // SimpleConsoleFormatter is internal; route all levels to stderr via options + public AddSimpleConsole.
            builder.Services.Configure<ConsoleLoggerOptions>(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            builder.AddSimpleConsole(o =>
            {
                o.TimestampFormat = "HH:mm:ss ";
                o.SingleLine = true;
            });
        }

        /// <summary>
        /// One <see cref="LogLevel.Debug"/> line after the host <see cref="ILoggerFactory"/> is built:
        /// process id, arg count, file sink, env log level, correlation id, redacted base directory.
        /// </summary>
        public static void LogHostStartupDebug(ILogger log, string toolDisplayName, string[] args)
        {
            if (log == null || !log.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            string logFile = Environment.GetEnvironmentVariable("KPATCHER_TOOL_LOG_FILE");
            log.LogDebug(
                "Tool={Tool} Phase=host.startup ProcessId={Pid} ArgCount={ArgCount} LogFileSink={HasFileSink} KPATCHER_TOOL_LOG_LEVEL={LogLevelEnv} CorrelationId={CorrelationId} BaseDir={BaseDir}",
                toolDisplayName ?? "",
                Environment.ProcessId,
                args?.Length ?? 0,
                !string.IsNullOrWhiteSpace(logFile),
                Environment.GetEnvironmentVariable("KPATCHER_TOOL_LOG_LEVEL") ?? "(unset)",
                ToolCorrelation.ReadOptional() ?? "(unset)",
                ToolPathRedaction.FormatPath(AppContext.BaseDirectory));
        }
    }
}
