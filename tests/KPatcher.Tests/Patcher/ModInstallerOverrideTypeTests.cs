using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Resources;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerOverrideTypeTests : ModInstallerIntegrationTestBase
    {
        public ModInstallerOverrideTypeTests()
            : base("KPatcher_OverrideType_")
        {
        }

        [Fact]
        public void Install_OverrideTypeWarn_LogsShadowWarningWhenOverrideFileExists()
        {
            string modulePath = Path.Combine(GameRoot, "Modules", "test.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            File.WriteAllBytes(Path.Combine(GameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(TslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            WriteChangesIni(@"
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
            CreateInstaller(logger).Install();

            logger.Warnings.Should().Contain(w =>
                w.Message.Contains("shadow.ncs", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Install_OverrideTypeRename_RenamesExistingOverrideFileBeforeModuleInstall()
        {
            string modulePath = Path.Combine(GameRoot, "Modules", "test.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            File.WriteAllBytes(Path.Combine(GameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(TslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            WriteChangesIni(@"
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
            CreateInstaller(logger).Install();

            File.Exists(Path.Combine(GameRoot, "Override", "old_shadow.ncs")).Should().BeTrue();
            File.ReadAllBytes(Path.Combine(GameRoot, "Override", "old_shadow.ncs")).Should().Equal(new byte[] { 1, 2, 3 });
            File.Exists(Path.Combine(GameRoot, "Override", "shadow.ncs")).Should().BeFalse();

            var capsule = new Capsule(modulePath, createIfNotExist: false);
            capsule.GetResource("shadow", ResourceType.NCS)[0].Should().Be(9);
        }

        [Fact]
        public void Install_OverrideTypeWarn_DoesNotWarnWhenDestinationIsOverride()
        {
            File.WriteAllBytes(Path.Combine(GameRoot, "Override", "shadow.ncs"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(TslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            WriteChangesIni(@"
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
            CreateInstaller(logger).Install();

            logger.Warnings.Should().NotContain(w =>
                w.Message.Contains("shadowing", StringComparison.OrdinalIgnoreCase));
        }
    }
}
