using System;
using System.IO;
using System.Text;
using FluentAssertions;
using IniParser.Model;
using KPatcher.Core.Logger;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    /// <summary>
    /// Tests for ConfigReader INI comment parsing, specifically for comments that look like section headers.
    /// This test reproduces the exact bug where a comment line like ";[K1] Legends - Ajunta Pall's Blade v1.0.2b"
    /// causes a ParsingException.
    /// </summary>
    public class ConfigReaderCommentParsingTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _iniFilePath;

        public ConfigReaderCommentParsingTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _iniFilePath = Path.Combine(_tempDir, "main_install.ini");
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
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void FromFilePath_WithCommentLineLookingLikeSectionHeader_ShouldNotThrow()
        {
            // Arrange - Create INI file with the exact problematic content from the bug report
            // The error shows: "Couldn't parse the line: '[K1] Legends - Ajunta Pall's Blade v1.0.2b'"
            // while parsing line number 1 with value ';[K1] Legends - Ajunta Pall's Blade v1.0.2b'
            string iniContent = @";[K1] Legends - Ajunta Pall's Blade v1.0.2b

[Settings]
ModName=Test Mod
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();

            // Act & Assert - Should not throw an exception
            // Before the fix, this would throw InvalidOperationException with inner ParsingException
            Action act = () => ConfigReader.FromFilePath(_iniFilePath, logger);
            act.Should().NotThrow("because comment lines starting with ; should be ignored");
        }

        [Fact]
        public void FromFilePath_WithCommentLineLookingLikeSectionHeader_ShouldParseSuccessfully()
        {
            // Arrange - Create INI file with comment that looks like section header
            string iniContent = @";[K1] Legends - Ajunta Pall's Blade v1.0.2b

[Settings]
ModName=Test Mod
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();

            // Act
            ConfigReader reader = ConfigReader.FromFilePath(_iniFilePath, logger);

            // Assert - Should successfully parse and create ConfigReader
            reader.Should().NotBeNull();
        }

        [Fact]
        public void FromFilePath_WithMultipleCommentLinesLookingLikeSectionHeaders_ShouldNotThrow()
        {
            // Arrange - Multiple comment lines that look like section headers
            string iniContent = @";[K1] Legends - Ajunta Pall's Blade v1.0.2b
;[K2] Another mod name
;[TSL] Some other mod

[Settings]
ModName=Test Mod
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();

            // Act & Assert
            Action act = () => ConfigReader.FromFilePath(_iniFilePath, logger);
            act.Should().NotThrow("because all comment lines should be ignored");
        }

        [Fact]
        public void FromFilePath_WithCommentLineContainingBrackets_ShouldNotThrow()
        {
            // Arrange - Comment line with brackets that could be mistaken for section header
            string iniContent = @"; This is a comment with [brackets] in it
;[K1] This looks like a section but is a comment

[Settings]
ModName=Test Mod
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();

            // Act & Assert
            Action act = () => ConfigReader.FromFilePath(_iniFilePath, logger);
            act.Should().NotThrow("because comment lines should be completely ignored");
        }

        [Fact]
        public void FromFilePath_WithCommentAfterSectionHeader_ShouldNotThrow()
        {
            // Arrange - Valid section header with comment after it
            string iniContent = @"[Settings] ; This is a comment after a section header
ModName=Test Mod
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();

            // Act & Assert
            Action act = () => ConfigReader.FromFilePath(_iniFilePath, logger);
            act.Should().NotThrow("because inline comments should be handled correctly");
        }

        [Fact]
        public void ParseIniText_WithUnicodeBomBeforeSettingsSection_ParsesSuccessfully()
        {
            string iniContent = "\uFEFF[Settings]\r\nModName=BOM Test\r\n";

            IniData ini = ConfigReader.ParseIniText(iniContent, caseInsensitive: false);

            ini.Sections.Should().Contain(s => s.SectionName.Equals("Settings", StringComparison.OrdinalIgnoreCase));
            ini["Settings"]["ModName"].Should().Be("BOM Test");
        }

        [Fact]
        public void FromFilePath_WithUtf8FileBomBeforeSettingsSection_ParsesSuccessfully()
        {
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            string iniContent = "[Settings]\r\nModName=File BOM\r\n";
            File.WriteAllText(_iniFilePath, iniContent, utf8WithBom);

            var logger = new PatchLogger();
            ConfigReader reader = ConfigReader.FromFilePath(_iniFilePath, logger);

            reader.Should().NotBeNull();
        }

        [Fact]
        public void ParseIniText_MatchesModInstallerStyleUtf8Decode_WithBomBytes_ParsesSuccessfully()
        {
            string iniContent = "[Settings]\r\nModName=Byte BOM\r\n";
            byte[] utf8Bom = Encoding.UTF8.GetPreamble();
            byte[] body = Encoding.UTF8.GetBytes(iniContent);
            var combined = new byte[utf8Bom.Length + body.Length];
            Buffer.BlockCopy(utf8Bom, 0, combined, 0, utf8Bom.Length);
            Buffer.BlockCopy(body, 0, combined, utf8Bom.Length, body.Length);
            string iniText = Encoding.UTF8.GetString(combined);

            IniData ini = ConfigReader.ParseIniText(iniText, caseInsensitive: false);

            ini["Settings"]["ModName"].Should().Be("Byte BOM");
        }
    }
}

