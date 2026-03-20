using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using Xunit;

namespace KPatcher.Core.Tests.Logger
{
    public class InstallLogWriterTests : IDisposable
    {
        private readonly string _tempDir;

        public InstallLogWriterTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "KPatcher_InstallLogWriterTests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
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
        public void Constructor_CreatesTxtByDefault()
        {
            using (var writer = new InstallLogWriter(_tempDir, useRtf: false))
            {
                writer.WriteInfo("test");
                writer.Flush();
            }
            string txtPath = Path.Combine(_tempDir, "installlog.txt");
            File.Exists(txtPath).Should().BeTrue();
            File.ReadAllText(txtPath).Should().Contain("test");
        }

        [Fact]
        public void Constructor_UseRtf_CreatesRtfFile()
        {
            using (var writer = new InstallLogWriter(_tempDir, useRtf: true))
            {
                writer.WriteInfo("test");
                writer.Flush();
            }
            string rtfPath = Path.Combine(_tempDir, "installlog.rtf");
            File.Exists(rtfPath).Should().BeTrue();
            File.ReadAllText(rtfPath).Should().Contain("test");
        }

        [Fact]
        public void WriteError_ContainsExactPrefix_ForKOTORModSync()
        {
            using (var writer = new InstallLogWriter(_tempDir, useRtf: false))
            {
                writer.WriteError("Something went wrong");
                writer.Flush();
            }
            string content = File.ReadAllText(Path.Combine(_tempDir, "installlog.txt"));
            content.Should().Contain("Error: Something went wrong");
        }

        [Fact]
        public void WriteWarning_ContainsWarningPrefix()
        {
            using (var writer = new InstallLogWriter(_tempDir, useRtf: false))
            {
                writer.WriteWarning("Possible issue");
                writer.Flush();
            }
            string content = File.ReadAllText(Path.Combine(_tempDir, "installlog.txt"));
            content.Should().Contain("Warning: Possible issue");
        }

        [Fact]
        public void WriteHeader_WritesMetadata()
        {
            using (var writer = new InstallLogWriter(_tempDir, useRtf: false))
            {
                writer.WriteHeader(_tempDir, @"C:\Game", Game.TSL);
                writer.Flush();
            }
            string content = File.ReadAllText(Path.Combine(_tempDir, "installlog.txt"));
            content.Should().Contain("KPatcher Installation Log");
            content.Should().Contain("Mod Path:");
            content.Should().Contain("Game Path:");
            content.Should().Contain("Game Detected: K2"); // Game.TSL is alias for K2
        }

        [Fact]
        public void Constructor_ThrowsOnNullModPath()
        {
            Action act = () => new InstallLogWriter(null);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
