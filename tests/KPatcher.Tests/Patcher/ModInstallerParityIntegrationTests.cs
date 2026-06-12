using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using KPatcher.Core.Resources;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher
{
    /// <summary>
    /// Install-path integration tests for TSLPatcher parity gaps identified in the 2026-06-12 audit.
    /// </summary>
    public sealed class ModInstallerParityIntegrationTests : IDisposable
    {
        static ModInstallerParityIntegrationTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private readonly string _tempRoot;
        private readonly string _modRoot;
        private readonly string _gameRoot;
        private readonly string _tslPatchDataPath;
        private readonly string _overridePath;

        public ModInstallerParityIntegrationTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_ParityInt_" + Guid.NewGuid().ToString("N"));
            _modRoot = Path.Combine(_tempRoot, "mod");
            _gameRoot = Path.Combine(_tempRoot, "game");
            _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");
            _overridePath = Path.Combine(_gameRoot, "Override");
            Directory.CreateDirectory(_tslPatchDataPath);
            Directory.CreateDirectory(_overridePath);
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
        public void Install_ProtectedFolderReplace_SkipsDialogTlkOverwrite()
        {
            string dialogPath = Path.Combine(_gameRoot, "dialog.tlk");
            string modDialogPath = Path.Combine(_tslPatchDataPath, "dialog.tlk");
            new TLK(Language.English).Save(dialogPath);
            new TLK(Language.English).Save(modDialogPath);
            byte[] originalDialog = File.ReadAllBytes(dialogPath);

            WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=.

[folder0]
Replace0=dialog.tlk
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.ReadAllBytes(dialogPath).Should().Equal(originalDialog);
            logger.Notes.Should().Contain(n =>
                n.Message.Contains("dialog.tlk", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Install_OverrideTypeIgnore_LeavesOverrideShadowUntouched()
        {
            string modulePath = Path.Combine(_gameRoot, "Modules", "test.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            byte[] shadowBytes = new byte[] { 1, 2, 3 };
            File.WriteAllBytes(Path.Combine(_overridePath, "shadow.ncs"), shadowBytes);
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });

            WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\test.mod

[module0]
File0=shadow.ncs

[shadow.ncs]
!OverrideType=ignore
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            File.ReadAllBytes(Path.Combine(_overridePath, "shadow.ncs")).Should().Equal(shadowBytes);

            var capsule = new Capsule(modulePath, createIfNotExist: false);
            capsule.GetResource("shadow", ResourceType.NCS)[0].Should().Be(9);
        }

        [Fact]
        public void Install_ScriptCompilerFlags_CustomNwscript_CompilesThroughInstallPath()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "custom.nss"), "void Helper() {}\n");
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "main.nss"), "void Helper();\nvoid main() { Helper(); }\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
ScriptCompilerFlags=--nwscript custom.nss

[CompileList]
File0=main.nss
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            string compiledPath = Path.Combine(_overridePath, "main.ncs");
            File.Exists(compiledPath).Should().BeTrue();
            File.ReadAllBytes(compiledPath).Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Install_2DAExclusiveFallback_SkipsIncOnExistingRow()
        {
            var twoda = new TwoDAFile(new List<string> { "Col1", "Col2", "Col3" });
            twoda.AddRow("0", new Dictionary<string, object>
            {
                { "Col1", "key" },
                { "Col2", "5" },
                { "Col3", "1" }
            });
            File.WriteAllBytes(Path.Combine(_overridePath, "excl.2da"), twoda.ToBytes());

            WriteChangesIni(@"
[Settings]
LogLevel=3

[2DAList]
Table0=excl.2da

[excl.2da]
AddRow0=add_row_0

[add_row_0]
ExclusiveColumn=Col1
Col1=key
Col2=inc(10)
Col3=updated
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var patched = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(_overridePath, "excl.2da")));
            patched.GetHeight().Should().Be(1);
            patched.GetRow(0).GetString("Col1").Should().Be("key");
            patched.GetRow(0).GetString("Col2").Should().Be("5");
            patched.GetRow(0).GetString("Col3").Should().Be("updated");
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
        }
    }
}
