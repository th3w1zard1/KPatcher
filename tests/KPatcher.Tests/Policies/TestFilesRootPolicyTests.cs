using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace KPatcher.Core.Tests.Policies
{
    /// <summary>
    /// Guard rail: committed fixture roots should not reintroduce top-level binary samples.
    /// Complex scenario families live in dedicated directories until their migrations complete.
    /// </summary>
    public sealed class TestFilesRootPolicyTests
    {
        [Fact]
        public void Test_files_root_contains_only_migrating_fixture_directories()
        {
            var root = FindTestFilesDirectory();
            Directory.Exists(root).Should().BeTrue("test_files root should exist next to KPatcher.Tests.csproj");

            var files = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            files.Should().BeEmpty("top-level test_files fixtures should be generated or embedded, not committed as standalone files");

            var directories = Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            directories.Should().BeEquivalentTo(
                new[]
                {
                    "exhaustive_pattern_inlines",
                    "integration_tslpatcher_archive_corpus",
                    "integration_tslpatcher_mods",
                },
                options => options.WithStrictOrdering(),
                "remaining disk-backed scenario families should stay isolated in dedicated directories until their migrations land");
        }

        private static string FindTestFilesDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "test_files");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "KPatcher.Tests.csproj")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate test_files starting from " + AppContext.BaseDirectory);
        }
    }
}