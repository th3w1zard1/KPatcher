using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    public class SystemHelpersTests : IDisposable
    {
        private readonly string _tempDir;

        public SystemHelpersTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "KPatcher_SystemHelpersTests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    ClearReadOnly(_tempDir);
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        private static void ClearReadOnly(string path)
        {
            foreach (var f in new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories))
            {
                if (f.IsReadOnly)
                    f.IsReadOnly = false;
            }
            foreach (var d in new DirectoryInfo(path).GetDirectories("*", SearchOption.AllDirectories))
            {
                if ((d.Attributes & FileAttributes.ReadOnly) != 0)
                    d.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        [Fact]
        public void FixPermissions_EmptyDir_DoesNotThrow()
        {
            var logs = new List<string>();
            Action act = () => SystemHelpers.FixPermissions(_tempDir, logs.Add);
            act.Should().NotThrow();
        }

        [Fact]
        public void FixPermissions_ReadOnlyFile_ClearsReadOnlyOnWindows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return; // Skip: test targets Windows ReadOnly behavior
            }

            string filePath = Path.Combine(_tempDir, "readonly.txt");
            File.WriteAllText(filePath, "content");
            var fi = new FileInfo(filePath);
            fi.IsReadOnly = true;

            var logs = new List<string>();
            SystemHelpers.FixPermissions(_tempDir, logs.Add);

            new FileInfo(filePath).IsReadOnly.Should().BeFalse();
            logs.Should().Contain(s => s.Contains("readonly.txt") && s.Contains("Fixed"));
        }

        [Fact]
        public void FixCaseSensitivityRecursive_MixedCaseFiles_RenamesToLower()
        {
            string subDir = Path.Combine(_tempDir, "SubDir");
            Directory.CreateDirectory(subDir);
            string upperPath = Path.Combine(subDir, "File.TXT");
            File.WriteAllText(upperPath, "data");

            var logs = new List<string>();
            SystemHelpers.FixCaseSensitivityRecursive(_tempDir, logs.Add);

            // After rename, directory may be "subdir" and file "file.txt" (case varies by OS)
            string foundPath = Path.Combine(Directory.GetDirectories(_tempDir)[0], "file.txt");
            File.Exists(foundPath).Should().BeTrue();
            File.ReadAllText(foundPath).Should().Be("data");
            logs.Should().Contain(s => s.Contains("file.txt"));
        }

        [Fact]
        public void FixCaseSensitivityRecursive_MixedCaseDirectory_RenamesToLower()
        {
            string mixedDir = Path.Combine(_tempDir, "MixedCaseDir");
            Directory.CreateDirectory(mixedDir);
            File.WriteAllText(Path.Combine(mixedDir, "f.txt"), "x");

            var logs = new List<string>();
            SystemHelpers.FixCaseSensitivityRecursive(_tempDir, logs.Add);

            string lowerDir = Path.Combine(_tempDir, "mixedcasedir");
            Directory.Exists(lowerDir).Should().BeTrue();
            File.ReadAllText(Path.Combine(lowerDir, "f.txt")).Should().Be("x");
        }

        [Fact]
        public void FixCaseSensitivityRecursive_AlreadyLower_NoChange()
        {
            string lowerPath = Path.Combine(_tempDir, "already.txt");
            File.WriteAllText(lowerPath, "same");

            var logs = new List<string>();
            SystemHelpers.FixCaseSensitivityRecursive(_tempDir, logs.Add);

            File.ReadAllText(lowerPath).Should().Be("same");
        }
    }
}
