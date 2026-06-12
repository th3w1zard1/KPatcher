using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Resources;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerOverrideTypeTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly string _modRoot;
        private readonly string _gameRoot;
        private readonly string _tslPatchDataPath;

        public ModInstallerOverrideTypeTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_OverrideType_" + Guid.NewGuid().ToString("N"));
            _modRoot = Path.Combine(_tempRoot, "mod");
            _gameRoot = Path.Combine(_tempRoot, "game");
            _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");
            Directory.CreateDirectory(_tslPatchDataPath);
            Directory.CreateDirectory(Path.Combine(_gameRoot, "Override"));
            Directory.CreateDirectory(Path.Combine(_gameRoot, "Modules"));
            File.WriteAllText(Path.Combine(_gameRoot, "swkotor2.exe"), string.Empty);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                try
                {
                    Directory.Delete(_tempRoot, true);
                }
                catch
                {
                }
            }
        }

        [Fact]
        public void Install_OverrideTypeWarn_LogsShadowWarningWhenOverrideFileExists()
        {
            string modulePath = Path.Combine(_gameRoot, "Modules", "test.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            File.WriteAllBytes(Path.Combine(_gameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\test.mod

[module0]
File0=shadow.ncs

[shadow.ncs]
!OverrideType=warn
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            logger.Warnings.Should().Contain(w =>
                w.Message.Contains("shadow.ncs", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Install_OverrideTypeRename_RenamesExistingOverrideFileBeforeModuleInstall()
        {
            string modulePath = Path.Combine(_gameRoot, "Modules", "test.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            File.WriteAllBytes(Path.Combine(_gameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\test.mod

[module0]
File0=shadow.ncs

[shadow.ncs]
!OverrideType=rename
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(_gameRoot, "Override", "old_shadow.ncs")).Should().BeTrue();
            File.ReadAllBytes(Path.Combine(_gameRoot, "Override", "old_shadow.ncs")).Should().Equal(new byte[] { 1, 2, 3 });
            File.Exists(Path.Combine(_gameRoot, "Override", "shadow.ncs")).Should().BeFalse();

            var capsule = new Capsule(modulePath, createIfNotExist: false);
            capsule.GetResource("shadow", ResourceType.NCS)[0].Should().Be(9);
        }

        [Fact]
        public void Install_OverrideTypeWarn_DoesNotWarnWhenDestinationIsOverride()
        {
            File.WriteAllBytes(Path.Combine(_gameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
Replace0=shadow.ncs

[shadow.ncs]
!OverrideType=warn
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            logger.Warnings.Should().NotContain(w =>
                w.Message.Contains("shadowing", StringComparison.OrdinalIgnoreCase));
        }
    }
}
