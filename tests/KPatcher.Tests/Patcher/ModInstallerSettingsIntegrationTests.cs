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
    public sealed class ModInstallerSettingsIntegrationTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly string _modRoot;
        private readonly string _gameRoot;
        private readonly string _tslPatchDataPath;

        public ModInstallerSettingsIntegrationTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_Settings_" + Guid.NewGuid().ToString("N"));
            _modRoot = Path.Combine(_tempRoot, "mod");
            _gameRoot = Path.Combine(_tempRoot, "game");
            _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");
            Directory.CreateDirectory(_tslPatchDataPath);
            Directory.CreateDirectory(Path.Combine(_gameRoot, "Override"));
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
        public void Install_InstallerModeFalse_SkipsInstallListButRunsHackList()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "install_only.txt"), "install");
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });

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
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(_gameRoot, "Override", "install_only.txt")).Should().BeFalse();
            File.Exists(Path.Combine(_gameRoot, "Override", "hack.ncs")).Should().BeTrue();
            File.ReadAllBytes(Path.Combine(_gameRoot, "Override", "hack.ncs"))[0].Should().Be(7);
        }

        [Fact]
        public void Install_InstallerModeTrue_AppliesInstallList()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "install_only.txt"), "install");

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
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(_gameRoot, "Override", "install_only.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(_gameRoot, "Override", "install_only.txt")).Should().Be("install");
        }

        [Fact]
        public void Install_BackupFilesTrue_CreatesBackupOfReplacedOverrideFile()
        {
            string targetPath = Path.Combine(_gameRoot, "Override", "target.txt");
            File.WriteAllText(targetPath, "original");
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "target.txt"), "patched");

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
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string backupRoot = Path.Combine(_modRoot, "backup");
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
            string targetPath = Path.Combine(_gameRoot, "Override", "target.txt");
            File.WriteAllText(targetPath, "original");
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "target.txt"), "patched");

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
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            Directory.Exists(Path.Combine(_modRoot, "backup")).Should().BeFalse();
            File.ReadAllText(targetPath).Should().Be("patched");
        }

        [Fact]
        public void Install_PlaintextLogDefault_CreatesRtfInstallLog()
        {
            WriteChangesIni("[Settings]\nLogLevel=3\n");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.rtf")).Should().BeTrue();
            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.txt")).Should().BeFalse();
        }

        [Fact]
        public void Install_PlaintextLogFalse_UsesRtfExtensionForInstallLogWriter()
        {
            WriteChangesIni("[Settings]\nLogLevel=3\nPlaintextLog=0\n");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.rtf")).Should().BeTrue();
            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.txt")).Should().BeFalse();
        }

        [Fact]
        public void Install_SaveProcessedScriptsZero_DeletesTempScriptFolder()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "compile.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
SaveProcessedScripts=0

[CompileList]
File0=compile.nss
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            Directory.Exists(Path.Combine(_tslPatchDataPath, "nsspatch_temp")).Should().BeFalse();
            File.Exists(Path.Combine(_gameRoot, "Override", "compile.ncs")).Should().BeTrue();
        }

        [Fact]
        public void Install_SaveProcessedScriptsOne_KeepsTempScriptFolder()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "compile.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
SaveProcessedScripts=1

[CompileList]
File0=compile.nss
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string tempFolder = Path.Combine(_tslPatchDataPath, "nsspatch_temp");
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
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.txt")).Should().BeTrue();
            File.Exists(Path.Combine(_tslPatchDataPath, "installlog.rtf")).Should().BeFalse();
        }

        [Fact]
        public void Install_CompileListDefaultDestination_ModuleCapsule_WritesCompiledNcsIntoArchive()
        {
            Directory.CreateDirectory(Path.Combine(_gameRoot, "Modules"));
            string modulePath = Path.Combine(_gameRoot, "Modules", "capsule.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "main.nss"), "void main() {}\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3

[CompileList]
!DefaultDestination=Modules\capsule.mod
File0=main.nss
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var capsule = new Capsule(modulePath, createIfNotExist: false);
            byte[] ncs = capsule.GetResource("main", ResourceType.NCS);
            ncs.Length.Should().BeGreaterThan(0);
            File.Exists(Path.Combine(_gameRoot, "Override", "main.ncs")).Should().BeFalse();
        }

        [Fact]
        public void Install_BackupFilesTrue_CreatesBackupWhenTwoDAIsPatched()
        {
            string targetPath = Path.Combine(_gameRoot, "Override", "backup.2da");
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

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            string backupRoot = Path.Combine(_modRoot, "backup");
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
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "install_only.txt"), "install");
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "combo.nss"), "void main() {}\n");

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

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            File.Exists(Path.Combine(_gameRoot, "Override", "install_only.txt")).Should().BeFalse();
            File.ReadAllBytes(Path.Combine(_gameRoot, "Override", "hack.ncs"))[0].Should().Be(7);
            InstallAssertionLadder.AssertParsesAsNcsL2(Path.Combine(_gameRoot, "Override", "combo.ncs"));
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
        }
    }
}
