using FluentAssertions;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Reader;
using System;
using System.IO;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    public sealed class ConfigReaderSettingsCompatibilityTests
    {
        [Fact]
        public void Load_tolerates_requiredmsg_without_required()
        {
            const string ini = @"
[Settings]
LogLevel=3
RequiredMsg=TSLRCM is not installed properly.
";

            PatcherConfig config = LoadConfig(ini);

            config.RequiredFiles.Should().BeEmpty();
            config.RequiredMessages.Should().ContainSingle().Which.Should().Be("TSLRCM is not installed properly.");
        }

        [Fact]
        public void Load_installer_mode_and_backup_files_defaults()
        {
            const string ini = @"
[Settings]
LogLevel=3
";

            PatcherConfig config = LoadConfig(ini);

            config.InstallerMode.Should().BeFalse();
            config.BackupFiles.Should().BeTrue();
            config.PlaintextLog.Should().BeFalse();
        }

        [Fact]
        public void Load_installer_mode_and_backup_files_from_bool_strings()
        {
            const string ini = @"
[Settings]
LogLevel=3
InstallerMode=true
BackupFiles=false
PlaintextLog=false
";

            PatcherConfig config = LoadConfig(ini);

            config.InstallerMode.Should().BeTrue();
            config.BackupFiles.Should().BeFalse();
            config.PlaintextLog.Should().BeFalse();
        }

        [Fact]
        public void Load_installer_mode_and_backup_files_from_ini()
        {
            const string ini = @"
[Settings]
LogLevel=3
InstallerMode=1
BackupFiles=0
PlaintextLog=1
";

            PatcherConfig config = LoadConfig(ini);

            config.InstallerMode.Should().BeTrue();
            config.BackupFiles.Should().BeFalse();
            config.PlaintextLog.Should().BeTrue();
        }

        [Fact]
        public void Load_tolerates_required_without_requiredmsg()
        {
            const string ini = @"
[Settings]
LogLevel=3
Required=foo.mod
";

            PatcherConfig config = LoadConfig(ini);

            config.RequiredFiles.Should().ContainSingle();
            config.RequiredFiles[0].Should().Equal("foo.mod");
            config.RequiredMessages.Should().BeEmpty();
        }

        private static PatcherConfig LoadConfig(string iniBody)
        {
            string dir = Path.Combine(Path.GetTempPath(), "cfg_settings_compat_" + Guid.NewGuid().ToString("N"));
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