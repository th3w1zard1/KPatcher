using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Tests.Patcher.Support;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.GFF;
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

        [Theory]
        [InlineData("swkotor.exe", "overwrite EXE files")]
        [InlineData("swkotor2.exe", "overwrite EXE files")]
        [InlineData("chitin.key", "overwrite the chitin.key file")]
        [InlineData("templates.bif", "overwrite BIF data files")]
        public void Install_ProtectedFolderReplace_SkipsProtectedGameRootTargets(string targetFile, string expectedMessageFragment)
        {
            string targetPath = Path.Combine(_gameRoot, targetFile);
            string modSourcePath = Path.Combine(_tslPatchDataPath, targetFile);
            byte[] originalBytes = new byte[] { 1, 2, 3 };
            byte[] replacementBytes = new byte[] { 9, 9, 9 };
            File.WriteAllBytes(targetPath, originalBytes);
            File.WriteAllBytes(modSourcePath, replacementBytes);

            WriteChangesIni($@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=.

[folder0]
Replace0={targetFile}
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            File.ReadAllBytes(targetPath).Should().Equal(originalBytes);
            logger.Notes.Should().Contain(n =>
                n.Message.Contains(expectedMessageFragment, StringComparison.Ordinal));
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

            InstallAssertionLadder.AssertParsesAsNcsL2(Path.Combine(_overridePath, "main.ncs"));
        }

        [Fact]
        public void Install_TlkAppendDedup_ReusesExistingDialogEntryForStrRefMemory()
        {
            var dialogTlk = new TLK(Language.English);
            dialogTlk.Add("SharedLine", string.Empty);
            dialogTlk.Save(Path.Combine(_gameRoot, "dialog.tlk"));

            var appendTlk = new TLK(Language.English);
            appendTlk.Add("SharedLine", string.Empty);
            appendTlk.Save(Path.Combine(_tslPatchDataPath, "append.tlk"));

            var gff = new GFF();
            gff.Root.SetUInt32("StrRefField", 0u);
            File.WriteAllBytes(Path.Combine(_overridePath, "token.gff"), gff.ToBytes());

            WriteChangesIni(@"
[Settings]
LogLevel=3

[TLKList]
StrRef0=0

[append.tlk]
0=SharedLine

[GFFList]
File0=token.gff

[token.gff]
StrRefField=StrRef0
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var dialogAfter = TLK.FromBytes(File.ReadAllBytes(Path.Combine(_gameRoot, "dialog.tlk")));
            dialogAfter.Count.Should().Be(1, "dedup should not append a duplicate dialog line");

            var patchedGff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(_overridePath, "token.gff")));
            patchedGff.Root.GetUInt32("StrRefField").Should().Be(0u);
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

        [Fact]
        public void Install_ReadOnlyExistingOverrideFile_PatchesSuccessfully()
        {
            var twoda = new TwoDAFile(new List<string> { "label" });
            twoda.AddRow("0", new Dictionary<string, object> { { "label", "old" } });
            string targetPath = Path.Combine(_overridePath, "readonly.2da");
            File.WriteAllBytes(targetPath, twoda.ToBytes());
            MakeFileReadOnlyForOverwrite(targetPath);

            WriteChangesIni(@"
[Settings]
LogLevel=3

[2DAList]
Table0=readonly.2da

[readonly.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=new
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var patched = TwoDAFile.FromBytes(File.ReadAllBytes(targetPath));
            patched.GetRow(0).GetString("label").Should().Be("new");
        }

        [Fact]
        public void Install_2DA_AddColumnBeforeChangeRow_InIniOrder_AppliesBoth()
        {
            var twoda = new TwoDAFile(new List<string> { "Col1" });
            twoda.AddRow("0", new Dictionary<string, object> { { "Col1", "A" } });
            File.WriteAllBytes(Path.Combine(_overridePath, "order.2da"), twoda.ToBytes());

            WriteChangesIni(@"
[Settings]
LogLevel=3

[2DAList]
Table0=order.2da

[order.2da]
AddColumn0=add_first
ChangeRow0=change_second

[add_first]
ColumnLabel=NewCol
DefaultValue=****

[change_second]
RowIndex=0
NewCol=X
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            var patched = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(_overridePath, "order.2da")));
            patched.GetHeaders().Should().Contain("NewCol");
            patched.GetRow(0).GetString("NewCol").Should().Be("X");
        }

        [Fact]
        public void Install_ScriptCompilerFlags_MissingNwscript_LeavesCompileErrors()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "main.nss"), "void Helper();\nvoid main() { Helper(); }\n");

            WriteChangesIni(@"
[Settings]
LogLevel=3
ScriptCompilerFlags=--nwscript missing_custom.nss

[CompileList]
File0=main.nss
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            logger.Errors.Should().NotBeEmpty();
            InstallAssertionLadder.AssertLoggerContainsFragment(logger, "main.nss");
            File.Exists(Path.Combine(_overridePath, "main.ncs")).Should().BeFalse();
        }

        [Fact]
        public void Install_HackList_OnNonNcsExtension_StillPatchesBytesAtOffset()
        {
            byte[] original = new byte[] { 0, 0, 0, 0 };
            File.WriteAllBytes(Path.Combine(_overridePath, "marker.bin"), original);
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "marker.bin"), original);

            WriteChangesIni(@"
[Settings]
LogLevel=3

[HACKList]
Replace0=marker.bin

[marker.bin]
0x0=u8:42
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            File.ReadAllBytes(Path.Combine(_overridePath, "marker.bin"))[0].Should().Be(42);
        }

        [Fact]
        public void Install_ProtectedCapsuleReplace_AllowsDialogTlkInModule()
        {
            string modulesPath = Path.Combine(_gameRoot, "Modules");
            Directory.CreateDirectory(modulesPath);
            string modulePath = Path.Combine(modulesPath, "capsule.mod");
            new Capsule(modulePath, createIfNotExist: true).Save();

            var modDialog = new TLK(Language.English);
            modDialog.Add("CapsuleLine", string.Empty);
            modDialog.Save(Path.Combine(_tslPatchDataPath, "dialog.tlk"));

            WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\capsule.mod

[module0]
Replace0=dialog.tlk
");

            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), new PatchLogger());

            installer.Install();

            File.Exists(Path.Combine(_gameRoot, "dialog.tlk")).Should().BeFalse();
            var capsule = new Capsule(modulePath, createIfNotExist: false);
            byte[] tlkBytes = capsule.GetResource("dialog", ResourceType.TLK);
            tlkBytes.Should().NotBeNull();
            var moduleTlk = TLK.FromBytes(tlkBytes);
            moduleTlk.Get(0).Text.Should().Be("CapsuleLine");
        }

        private static void MakeFileReadOnlyForOverwrite(string filePath)
        {
            if (OperatingSystem.IsWindows())
            {
                new FileInfo(filePath).IsReadOnly = true;
                return;
            }

            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                ArgumentList = { "444", filePath },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start chmod for read-only test setup.");
                }

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string stderr = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException("chmod 444 failed: " + stderr);
                }
            }
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
        }
    }
}
