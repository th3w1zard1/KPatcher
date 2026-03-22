using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    /// <summary>
    /// Tests for ConfigReader YAML support: loading .yaml and writing equivalent .yaml when loading .ini.
    /// </summary>
    public class ConfigReaderYamlTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigReaderYamlTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); }
                catch { /* ignore */ }
            }
        }

        [Fact]
        public void FromFilePath_LoadsYaml_AndProducesSameConfigAsIni()
        {
            string iniPath = Path.Combine(_tempDir, "changes.ini");
            string yamlPath = Path.Combine(_tempDir, "changes.yaml");
            // ConfigReader reads window title from WindowCaption
            string iniContent = @"[Settings]
WindowCaption=Test Mod
GameNumber=2

[InstallList]
Override=Override

[Override]
File1=test.mdl
";
            File.WriteAllText(iniPath, iniContent);

            var logger = new PatchLogger();
            ConfigReader iniReader = ConfigReader.FromFilePath(iniPath, logger);
            iniReader.Load(iniReader.Config);
            PatcherConfig iniConfig = iniReader.Config;

            // Write equivalent YAML (as we do when loading INI)
            iniReader.WriteEquivalentYaml(yamlPath);
            File.Exists(yamlPath).Should().BeTrue();

            // Load from YAML
            ConfigReader yamlReader = ConfigReader.FromFilePath(yamlPath, logger);
            yamlReader.Load(yamlReader.Config);
            PatcherConfig yamlConfig = yamlReader.Config;

            yamlConfig.WindowTitle.Should().Be(iniConfig.WindowTitle);
            yamlConfig.GameNumber.Should().Be(iniConfig.GameNumber);
            yamlConfig.InstallList.Count.Should().Be(iniConfig.InstallList.Count);
        }

        [Fact]
        public void WriteEquivalentYaml_CreatesValidYaml_LoadableViaFromFilePath()
        {
            string iniPath = Path.Combine(_tempDir, "changes.ini");
            string yamlPath = Path.Combine(_tempDir, "changes.yaml");
            // ConfigReader reads window title from WindowCaption, not WindowTitle
            File.WriteAllText(iniPath, @"[Settings]
WindowCaption=YAML Roundtrip Test
[InstallList]
Override=Override
[Override]
File1=file.mdl
");
            var logger = new PatchLogger();
            ConfigReader reader = ConfigReader.FromFilePath(iniPath, logger);
            reader.Load(reader.Config);
            reader.WriteEquivalentYaml(yamlPath);

            ConfigReader fromYaml = ConfigReader.FromFilePath(yamlPath, logger);
            fromYaml.Load(fromYaml.Config);
            fromYaml.Config.WindowTitle.Should().Be("YAML Roundtrip Test");
            fromYaml.Config.InstallList.Should().HaveCount(1);
        }
    }
}
