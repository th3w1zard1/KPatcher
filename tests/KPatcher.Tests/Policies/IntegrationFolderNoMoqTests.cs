using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace KPatcher.Core.Tests.Policies
{
    /// <summary>
    /// Guard rail: <c>Integration/</c> tests use real I/O and fixtures, not mocking frameworks.
    /// </summary>
    public sealed class IntegrationFolderNoMoqTests
    {
        [Fact]
        public void Integration_folder_sources_do_not_reference_moq_or_mock_generics()
        {
            var root = FindTestProjectDirectory();
            var integration = Path.Combine(root, "Integration");
            Directory.Exists(integration).Should().BeTrue("Integration folder should exist next to KPatcher.Tests.csproj");

            var offenders = Directory.EnumerateFiles(integration, "*.cs", SearchOption.AllDirectories)
                .SelectMany(path => File.ReadLines(path).Select((line, i) => (path, line, i + 1)))
                .Where(t =>
                {
                    var trimmed = t.line.TrimStart();
                    if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("/*", StringComparison.Ordinal))
                        return false;
                    return trimmed.IndexOf("using Moq", StringComparison.Ordinal) >= 0
                           || trimmed.IndexOf("Mock<", StringComparison.Ordinal) >= 0
                           || trimmed.IndexOf("Moq.", StringComparison.Ordinal) >= 0;
                })
                .Select(t => $"{t.path}:{t.Item3}: {t.line.Trim()}")
                .ToList();

                offenders.Should().BeEmpty(
                    "Integration tests should not use Moq or Mock<>; use temp directories and actual ModInstaller / readers instead.");
        }

        private static string FindTestProjectDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var csproj = Path.Combine(dir.FullName, "KPatcher.Tests.csproj");
                if (File.Exists(csproj))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate KPatcher.Tests.csproj starting from " + AppContext.BaseDirectory);
        }
    }
}
