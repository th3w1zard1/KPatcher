using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using Xunit;

namespace KPatcher.Core.Tests.Logger
{
    public class InstallFlightRecorderTests : IDisposable
    {
        private readonly string _tempDir;

        public InstallFlightRecorderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "KPatcher_InstallFlightRecorderTests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        [Fact]
        public void Constructor_WritesHeaderAndSeededDiagnostics()
        {
            var logger = new PatchLogger();
            logger.AddDiagnostic("ctor diagnostic");
            logger.AddWarning("ctor warning");

            using (var recorder = new InstallFlightRecorder(_tempDir, @"C:\Game", Game.TSL, logger))
            {
                recorder.MarkOutcome(InstallFlightRecorderOutcome.Success);
            }

            string content = File.ReadAllText(Path.Combine(_tempDir, "installrecord.txt"));
            content.Should().Contain("KPatcher Install Flight Record");
            content.Should().Contain("ctor diagnostic");
            content.Should().Contain("ctor warning");
            content.Should().Contain("Outcome: Success");
        }

        [Fact]
        public void Constructor_ThrowsWhenModDirectoryIsMissing()
        {
            string missingDir = Path.Combine(_tempDir, "missing");
            var logger = new PatchLogger();

            Action act = () => new InstallFlightRecorder(missingDir, @"C:\Game", Game.TSL, logger);

            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void Dispose_WritesTerminalStateAndStreamsNewLogs()
        {
            var logger = new PatchLogger();

            using (var recorder = new InstallFlightRecorder(_tempDir, @"C:\Game", Game.TSL, logger))
            {
                logger.AddNote("later note");
                recorder.MarkOutcome(InstallFlightRecorderOutcome.Cancelled, "cancelled by user");
            }

            string content = File.ReadAllText(Path.Combine(_tempDir, "installrecord.txt"));
            content.Should().Contain("later note");
            content.Should().Contain("Outcome: Cancelled");
            content.Should().Contain("cancelled by user");
            content.Should().Contain("Terminal State");
        }
    }
}
