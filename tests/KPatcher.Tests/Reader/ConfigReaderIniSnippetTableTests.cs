using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    /// <summary>
    /// Table-driven <see cref="ConfigReader"/> coverage from small committed-style INI bodies.
    /// </summary>
    public sealed class ConfigReaderIniSnippetTableTests
    {
        public static IEnumerable<object[]> LogLevelCases()
        {
            yield return new object[] { "[Settings]\nLogLevel=0\n", LogLevel.Nothing };
            yield return new object[] { "[Settings]\nLogLevel=4\n", LogLevel.Full };
        }

        [Theory]
        [MemberData(nameof(LogLevelCases))]
        public void Load_maps_loglevel(string iniBody, LogLevel expected)
        {
            PatcherConfig cfg = LoadConfig(iniBody);
            cfg.LogLevel.Should().Be(expected);
        }

        [Theory]
        [InlineData("[Settings]\nWindowCaption=My Mod\n", "My Mod")]
        [InlineData("[Settings]\nWindowCaption=\n", "")]
        public void Load_maps_window_caption(string iniBody, string expectedTitle)
        {
            PatcherConfig cfg = LoadConfig(iniBody);
            cfg.WindowTitle.Should().Be(expectedTitle);
        }

        [Fact]
        public void Load_install_list_parses_file_entry()
        {
            const string ini = @"
[Settings]
LogLevel=3

[InstallList]
install_folder0=Override

[install_folder0]
!SourceFolder=.
File0=sample.wav
";
            PatcherConfig cfg = LoadConfig(ini);
            cfg.InstallList.Should().ContainSingle();
            var entry = cfg.InstallList[0];
            entry.Destination.Should().Be("Override");
            entry.SaveAs.Should().Be("sample.wav");
            entry.SourceFolder.Should().Be(".");
            entry.ReplaceFile.Should().BeFalse();
        }

        private static PatcherConfig LoadConfig(string iniBody)
        {
            string dir = Path.Combine(Path.GetTempPath(), "cfg_snip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "changes.ini");
            File.WriteAllText(path, iniBody);
            try
            {
                ConfigReader reader = ConfigReader.FromFilePath(path, new PatchLogger());
                return reader.Load(new PatcherConfig());
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                }
            }
        }
    }
}
