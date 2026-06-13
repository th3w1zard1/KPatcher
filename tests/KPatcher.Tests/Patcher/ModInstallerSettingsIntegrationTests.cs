using System;
using System.IO;
using System.Collections.Generic;
using FluentAssertions;
using KPatcher.Core.Tests.Patcher.Support;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using KPatcher.Core.Resources;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerSettingsIntegrationTests : ModInstallerIntegrationTestBase
    {
        public ModInstallerSettingsIntegrationTests()
            : base("KPatcher_Settings_")
        {
        }

        [Fact]
        public void Install_InstallerModeFalse_SkipsInstallListButRunsHackList()
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "install_only.txt"), "install");
            File.WriteAllBytes(Path.Combine(TslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });

            WriteChangesIni(@"
[Settings]
LogLevel=3

[InstallList]
folder0=Override

[folder0]
File0=install_only.txt

[HACKList]
hack.ncs=hack.ncs

[hack.ncs]
0x0=u8:7
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(GameRoot, "Override", "install_only.txt")).Should().BeFalse();
            File.Exists(Path.Combine(GameRoot, "Override", "hack.ncs")).Should().BeTrue();
            File.ReadAllBytes(Path.Combine(GameRoot, "Override", "hack.ncs"))[0].Should().Be(7);
        }

        [Fact]
        public void Install_InstallerModeTrue_AppliesInstallList()
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "install_only.txt"), "install");

            WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=install_only.txt
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(GameRoot, "Override", "install_only.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(GameRoot, "Override", "install_only.txt")).Should().Be("install");
        }

        [Fact]
        public void Install_BackupFilesTrue_CreatesBackupOfReplacedOverrideFile()
        {
            string targetPath = Path.Combine(GameRoot, "Override", "target.txt");
            File.WriteAllText(targetPath, "original");
            File.WriteAllText(Path.Combine(TslPatchDataPath, "target.txt"), "patched");

            WriteChangesIni(@"
[Settings]
LogLevel=3
BackupFiles=1
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
Replace0=target.txt
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string backupRoot = Path.Combine(ModRoot, "backup");
            Directory.Exists(backupRoot).Should().BeTrue();

            string[] backupDirs = Directory.GetDirectories(backupRoot);
            backupDirs.Should().NotBeEmpty();

            string backupFile = Path.Combine(backupDirs[0], "Override", "target.txt");
            File.Exists(backupFile).Should().BeTrue();
            File.ReadAllText(backupFile).Should().Be("original");
            File.ReadAllText(targetPath).Should().Be("patched");
        }

        [Fact]
        public void Install_BackupFilesFalse_DoesNotCreateBackupDirectory()
        {
            string targetPath = Path.Combine(GameRoot, "Override", "target.txt");
            File.WriteAllText(targetPath, "original");
            File.WriteAllText(Path.Combine(TslPatchDataPath, "target.txt"), "patched");

            WriteChangesIni(@"
[Settings]
LogLevel=3
BackupFiles=0
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
Replace0=target.txt
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            Directory.Exists(Path.Combine(ModRoot, "backup")).Should().BeFalse();
            File.ReadAllText(targetPath).Should().Be("patched");
        }

        [Fact]
        public void Install_PlaintextLogDefault_CreatesRtfInstallLog()
        {
            WriteChangesIni("[Settings]\nLogLevel=3\n");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(TslPatchDataPath, "installlog.rtf")).Should().BeTrue();
            File.Exists(Path.Combine(TslPatchDataPath, "installlog.txt")).Should().BeFalse();
        }

        [Fact]
        public void Install_PlaintextLogFalse_UsesRtfExtensionForInstallLogWriter()
        {
            WriteChangesIni("[Settings]\nLogLevel=3\nPlaintextLog=0\n");

            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            File.Exists(Path.Combine(TslPatchDataPath, "installlog.rtf")).Should().BeTrue();
            File.Exists(Path.Combine(TslPatchDataPath, "installlog.txt")).Should().BeFalse();
        }

        [Fact]
        public void Install_SaveProcessedScriptsZero_DeletesTempScriptFolder()
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "compile.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
SaveProcessedScripts=0

[CompileList]
File0=compile.nss
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            Directory.Exists(Path.Combine(TslPatchDataPath, "nsspatch_temp")).Should().BeFalse();
            File.Exists(Path.Combine(GameRoot, "Override", "compile.ncs")).Should().BeTrue();
        }

        [Fact]
        public void Install_SaveProcessedScriptsOne_KeepsTempScriptFolder()
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "compile.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
SaveProcessedScripts=1

[CompileList]
File0=compile.nss
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string tempFolder = Path.Combine(TslPatchDataPath, "nsspatch_temp");
            Directory.Exists(tempFolder).Should().BeTrue();
            Directory.GetFiles(tempFolder, "compile.nss", SearchOption.AllDirectories).Should().NotBeEmpty();
        }

        [Fact]
        public void Install_PlaintextLogTrue_CreatesTextInstallLog()
        {
            WriteChangesIni(@"
[Settings]
LogLevel=3
PlaintextLog=1
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(TslPatchDataPath, "installlog.txt")).Should().BeTrue();
            File.Exists(Path.Combine(TslPatchDataPath, "installlog.rtf")).Should().BeFalse();
        }

        [Fact]
        public void Install_CompileListDefaultDestination_ModuleCapsule_WritesCompiledNcsIntoArchive()
        {
            Directory.CreateDirectory(Path.Combine(GameRoot, "Modules"));
            string modulePath = Path.Combine(GameRoot, "Modules", "capsule.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();
            File.WriteAllText(Path.Combine(TslPatchDataPath, "main.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3

[CompileList]
!DefaultDestination=Modules\capsule.mod
File0=main.nss
");

            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var capsule = new Capsule(modulePath, createIfNotExist: false);
            byte[] ncs = capsule.GetResource("main", ResourceType.NCS);
            ncs.Length.Should().BeGreaterThan(0);
            File.Exists(Path.Combine(GameRoot, "Override", "main.ncs")).Should().BeFalse();
        }

        [Fact]
        public void Install_BackupFilesTrue_CreatesBackupWhenTwoDAIsPatched()
        {
            string targetPath = Path.Combine(GameRoot, "Override", "backup.2da");
            var twoda = new global::KPatcher.Core.Formats.TwoDA.TwoDA(new List<string> { "label" });
            twoda.AddRow("0", new Dictionary<string, object> { { "label", "original" } });
            File.WriteAllBytes(targetPath, twoda.ToBytes());

            WriteChangesIni(@"
[Settings]
LogLevel=3
BackupFiles=1

[2DAList]
Table0=backup.2da

[backup.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=patched
");

            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            string backupRoot = Path.Combine(ModRoot, "backup");
            Directory.Exists(backupRoot).Should().BeTrue();
            string[] backupDirs = Directory.GetDirectories(backupRoot);
            backupDirs.Should().NotBeEmpty();
            string backupFile = Path.Combine(backupDirs[0], "Override", "backup.2da");
            File.Exists(backupFile).Should().BeTrue();
            var backupTwoda = global::KPatcher.Core.Formats.TwoDA.TwoDA.FromBytes(File.ReadAllBytes(backupFile));
            backupTwoda.GetRow(0).GetString("label").Should().Be("original");
        }

        [Fact]
        public void Install_InstallerModeFalse_SkipsInstallListButRunsHackAndCompile()
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "install_only.txt"), "install");
            File.WriteAllBytes(Path.Combine(TslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });
            File.WriteAllText(Path.Combine(TslPatchDataPath, "combo.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3

[InstallList]
folder0=Override

[folder0]
File0=install_only.txt

[HACKList]
hack.ncs=hack.ncs

[hack.ncs]
0x0=u8:7

[CompileList]
File0=combo.nss
");

            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            File.Exists(Path.Combine(GameRoot, "Override", "install_only.txt")).Should().BeFalse();
            File.ReadAllBytes(Path.Combine(GameRoot, "Override", "hack.ncs"))[0].Should().Be(7);
            InstallAssertionLadder.AssertParsesAsNcsL2(Path.Combine(GameRoot, "Override", "combo.ncs"));
        }

    }
}
