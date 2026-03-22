using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KCompiler.Diagnostics
{
    /// <summary>Append-only UTF-8 file sink for tool diagnostics. Enable with env <c>KPATCHER_TOOL_LOG_FILE</c>.</summary>
    public sealed class SimpleFileLoggerProvider : ILoggerProvider, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _sync = new object();

        public SimpleFileLoggerProvider(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Log file path is required.", nameof(path));
            }

            _writer = new StreamWriter(
                new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(false))
            {
                AutoFlush = true
            };
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName ?? string.Empty, _writer, _sync);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _writer?.Dispose();
            }
        }

        private sealed class FileLogger : ILogger
        {
            private readonly string _category;
            private readonly StreamWriter _writer;
            private readonly object _sync;

            public FileLogger(string category, StreamWriter writer, object sync)
            {
                _category = category;
                _writer = writer;
                _sync = sync;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                string message = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
                var sb = new StringBuilder();
                sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(' ')
                    .Append(logLevel)
                    .Append(' ')
                    .Append(_category)
                    .Append(": ")
                    .Append(message);
                if (exception != null)
                {
                    sb.AppendLine().Append(exception);
                }

                lock (_sync)
                {
                    _writer.WriteLine(sb.ToString());
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new NullScope();
            public void Dispose()
            {
            }
        }
    }
}
