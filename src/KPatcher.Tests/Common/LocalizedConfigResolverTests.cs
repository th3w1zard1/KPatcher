using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using Xunit;

namespace KPatcher.Core.Tests.Common
{
    /// <summary>
    /// Tests for localized config file resolution (changes.&lt;lang&gt;.ini, namespaces.&lt;lang&gt;.ini, etc.).
    /// See docs/LOCALIZED_CONFIG_FILES.md.
    /// </summary>
    public class LocalizedConfigResolverTests
    {
        [Fact]
        public void Resolve_WhenOnlyBaseIniExists_ReturnsBaseIni()
        {
            using (var dir = new TemporaryDirectory())
            {
                string changesIni = Path.Combine(dir.Path, "changes.ini");
                File.WriteAllText(changesIni, "[Config]");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("changes.ini");
                File.Exists(fullPath).Should().BeTrue();
                fullPath.Should().EndWith("changes.ini");
            }
        }

        [Fact]
        public void Resolve_WhenBaseYamlAndIniExist_PrefersYaml()
        {
            using (var dir = new TemporaryDirectory())
            {
                string changesIni = Path.Combine(dir.Path, "changes.ini");
                string changesYaml = Path.Combine(dir.Path, "changes.yaml");
                File.WriteAllText(changesIni, "[Config]");
                File.WriteAllText(changesYaml, "Config:");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("changes.yaml");
                fullPath.Should().EndWith("changes.yaml");
            }
        }

        [Fact]
        public void Resolve_WhenLocalizedIniExists_PrefersLocalizedIni()
        {
            using (var dir = new TemporaryDirectory())
            {
                string changesIni = Path.Combine(dir.Path, "changes.ini");
                string changesDeIni = Path.Combine(dir.Path, "changes.de.ini");
                File.WriteAllText(changesIni, "[Config]");
                File.WriteAllText(changesDeIni, "[Config]");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("changes.de.ini");
                File.Exists(fullPath).Should().BeTrue();
                fullPath.Should().EndWith("changes.de.ini");
            }
        }

        [Fact]
        public void Resolve_WhenLocalizedYamlAndIniExist_PrefersYaml()
        {
            using (var dir = new TemporaryDirectory())
            {
                string changesDeIni = Path.Combine(dir.Path, "changes.de.ini");
                string changesDeYaml = Path.Combine(dir.Path, "changes.de.yaml");
                File.WriteAllText(changesDeIni, "[Config]");
                File.WriteAllText(changesDeYaml, "Config:");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("changes.de.yaml");
                fullPath.Should().EndWith("changes.de.yaml");
            }
        }

        [Fact]
        public void Resolve_WhenTryYamlFalse_OnlyTriesIni()
        {
            using (var dir = new TemporaryDirectory())
            {
                string namespacesIni = Path.Combine(dir.Path, "namespaces.ini");
                File.WriteAllText(namespacesIni, "[Namespaces]");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "namespaces", "fr", tryYaml: false);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("namespaces.ini");
            }
        }

        [Fact]
        public void Resolve_WhenNamespacesYamlAndIniExist_PrefersYaml()
        {
            using (var dir = new TemporaryDirectory())
            {
                string namespacesIni = Path.Combine(dir.Path, "namespaces.ini");
                string namespacesYaml = Path.Combine(dir.Path, "namespaces.yaml");
                File.WriteAllText(namespacesIni, "[Namespaces]\nNamespace1=Default\n[Default]\nIniName=changes.ini\nInfoName=info.rtf");
                File.WriteAllText(namespacesYaml, "Namespaces:\n  - Key: Namespace1\n    Value: Default\nDefault:\n  - Key: IniName\n    Value: changes.ini\n  - Key: InfoName\n    Value: info.rtf\n");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "namespaces", "fr", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("namespaces.yaml");
                fullPath.Should().EndWith("namespaces.yaml");
            }
        }

        [Fact]
        public void Resolve_WhenNoFileExists_ReturnsNull()
        {
            using (var dir = new TemporaryDirectory())
            {
                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().BeNull();
                fileName.Should().BeNull();
            }
        }

        [Fact]
        public void Resolve_OrderIsLocalizedYamlThenLocalizedIniThenBaseYamlThenBaseIni()
        {
            using (var dir = new TemporaryDirectory())
            {
                string changesYaml = Path.Combine(dir.Path, "changes.yaml");
                File.WriteAllText(changesYaml, "Config:");

                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "changes", "de", tryYaml: true);

                fullPath.Should().NotBeNull();
                fileName.Should().Be("changes.yaml");
            }
        }

        [Fact]
        public void Resolve_WhenBaseNameEmpty_ReturnsNull()
        {
            using (var dir = new TemporaryDirectory())
            {
                var directory = new CaseAwarePath(dir.Path);
                var (fullPath, fileName) = LocalizedConfigResolver.Resolve(directory, "", "de", tryYaml: true);

                fullPath.Should().BeNull();
                fileName.Should().BeNull();
            }
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public string Path { get; }

            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KPatcher_LocalizedConfig_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(Path);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
