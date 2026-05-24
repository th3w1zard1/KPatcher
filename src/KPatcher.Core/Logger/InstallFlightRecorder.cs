using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Logger
{
    /// <summary>
    /// Writes a compact install flight record alongside the raw install log.
    /// </summary>
    public sealed class InstallFlightRecorder : IDisposable
    {
        private const string RecordFileName = "installrecord.txt";

        private readonly PatchLogger _logger;
        private readonly StreamWriter _writer;
        private readonly object _lockObject = new object();
        private readonly EventHandler<PatchLog> _logAddedHandler;
        private bool _disposed;
        private bool _writeDisabled;
        private InstallFlightRecorderOutcome _outcome = InstallFlightRecorderOutcome.Unknown;
        private string _detail;

        public string RecordPath { get; }

        public InstallFlightRecorder(string modPath, string gamePath, [CanBeNull] Game? game, PatchLogger logger)
        {
            if (string.IsNullOrEmpty(modPath))
            {
                throw new ArgumentNullException(nameof(modPath));
            }

            if (!Directory.Exists(modPath))
            {
                throw new DirectoryNotFoundException(string.Format(CultureInfo.CurrentCulture, PatcherResources.InstallFlightRecordDirectoryNotFound, modPath));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RecordPath = Path.Combine(modPath, RecordFileName);

            if (File.Exists(RecordPath))
            {
                try
                {
                    File.Delete(RecordPath);
                }
                catch
                {
                    // Ignore stale record cleanup errors; a new record may still be writable.
                }
            }

            _writer = new StreamWriter(RecordPath, append: false, Encoding.UTF8);
            WriteHeader(modPath, gamePath, game);
            WriteExistingLogs();

            _logAddedHandler = HandleLogAdded;
            _logger.LogAdded += _logAddedHandler;
        }

        public void MarkOutcome(InstallFlightRecorderOutcome outcome, [CanBeNull] string detail = null)
        {
            lock (_lockObject)
            {
                _outcome = outcome;
                _detail = detail;
            }
        }

        private void WriteHeader(string modPath, string gamePath, [CanBeNull] Game? game)
        {
            _writer.WriteLine("KPatcher Install Flight Record");
            _writer.WriteLine("===============================");
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.InstallationDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.ModPath, modPath));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.GamePath, gamePath));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.GameDetected, game.HasValue ? game.Value.ToString() : "Unknown"));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.InstallFlightRecordPath, RecordPath));
            _writer.WriteLine();
            _writer.Flush();
        }

        private void WriteExistingLogs()
        {
            foreach (PatchLog log in _logger.AllLogs)
            {
                WriteLogEntry(log);
            }

            _writer.Flush();
        }

        private void HandleLogAdded(object sender, PatchLog log)
        {
            lock (_lockObject)
            {
                if (_disposed || _writeDisabled)
                {
                    return;
                }

                try
                {
                    WriteLogEntry(log);
                    _writer.Flush();
                }
                catch
                {
                    _writeDisabled = true;
                }
            }
        }

        private void WriteLogEntry(PatchLog log)
        {
            if (log == null)
            {
                return;
            }

            _writer.WriteLine(log.FormattedMessage);
        }

        private void WriteTerminalSection()
        {
            _writer.WriteLine();
            _writer.WriteLine("Terminal State");
            _writer.WriteLine("--------------");
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Outcome: {0}", _outcome));

            if (!string.IsNullOrEmpty(_detail))
            {
                _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Detail: {0}", _detail));
            }

            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Diagnostics: {0}", _logger.Diagnostics.Count()));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Notes: {0}", _logger.Notes.Count()));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Warnings: {0}", _logger.Warnings.Count()));
            _writer.WriteLine(string.Format(CultureInfo.CurrentCulture, "Errors: {0}", _logger.Errors.Count()));
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    _logger.LogAdded -= _logAddedHandler;
                }
                catch
                {
                    // Ignore event detachment failures.
                }

                try
                {
                    if (!_writeDisabled)
                    {
                        WriteTerminalSection();
                        _writer.Flush();
                    }
                }
                catch
                {
                    // Best-effort recorder; installation should not fail because the sidecar could not finalize.
                }
                finally
                {
                    try
                    {
                        _writer.Dispose();
                    }
                    catch
                    {
                        // Ignore close failures.
                    }

                    _disposed = true;
                }
            }
        }
    }

    public enum InstallFlightRecorderOutcome
    {
        Unknown = 0,
        Success = 1,
        Failure = 2,
        Cancelled = 3
    }
}
