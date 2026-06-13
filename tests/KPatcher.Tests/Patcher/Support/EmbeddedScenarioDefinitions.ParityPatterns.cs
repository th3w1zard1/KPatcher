using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Resources;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher.Support
{
  public static partial class EmbeddedScenarioDefinitions
  {
    internal static IEnumerable<EmbeddedInstallScenario> ParityPatternScenarios()
    {
      yield return TwoDaSsfMemory();
      yield return TwoDaExclusiveFallback();
      yield return CapsuleDialogTlk();
      yield return ProtectedDialogSkip();
      yield return BackupFilesInstallReplace();
      yield return CompileModuleCapsule();
      yield return CustomNwscriptCompile();
      yield return OverrideTypeIgnoreModule();
    }

    private static EmbeddedInstallScenario TwoDaSsfMemory()
    {
      return new EmbeddedInstallScenario(
        "inline_2da_ssf_memory",
        @"
[Settings]
LogLevel=3

[2DAList]
Table0=memory.2da

[memory.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=patched_label
2DAMEMORY5=soundref

[SSFList]
File0=memory.ssf

[memory.ssf]
Battlecry 2=2DAMEMORY5
",
        env =>
        {
          var twoda = new TwoDAFile(new List<string> { "label", "soundref" });
          twoda.AddRow("0", new Dictionary<string, object>
          {
            { "label", "0" },
            { "soundref", "456" }
          });
          File.WriteAllBytes(Path.Combine(env.OverridePath, "memory.2da"), twoda.ToBytes());

          var ssf = new SSF();
          ssf.SetData(SSFSound.BATTLE_CRY_2, 0);
          File.WriteAllBytes(Path.Combine(env.OverridePath, "memory.ssf"), ssf.ToBytes());
        },
        env =>
        {
          var patched = SSF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "memory.ssf")));
          patched.Get(SSFSound.BATTLE_CRY_2).Should().Be(456);
        });
    }

    private static EmbeddedInstallScenario TwoDaExclusiveFallback()
    {
      return new EmbeddedInstallScenario(
        "inline_2da_exclusive_fallback",
        @"
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
",
        env =>
        {
          var twoda = new TwoDAFile(new List<string> { "Col1", "Col2", "Col3" });
          twoda.AddRow("0", new Dictionary<string, object>
          {
            { "Col1", "key" },
            { "Col2", "5" },
            { "Col3", "1" }
          });
          File.WriteAllBytes(Path.Combine(env.OverridePath, "excl.2da"), twoda.ToBytes());
        },
        env =>
        {
          var patched = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "excl.2da")));
          patched.GetHeight().Should().Be(1);
          patched.GetRow(0).GetString("Col2").Should().Be("5");
          patched.GetRow(0).GetString("Col3").Should().Be("updated");
        });
    }

    private static EmbeddedInstallScenario CapsuleDialogTlk()
    {
      return new EmbeddedInstallScenario(
        "inline_capsule_dialog_tlk",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\capsule.mod

[module0]
Replace0=dialog.tlk
",
        env =>
        {
          string modulePath = Path.Combine(env.ModulesPath, "capsule.mod");
          new Capsule(modulePath, createIfNotExist: true).Save();
          var modDialog = new TLK(Language.English);
          modDialog.Add("CapsuleLine", string.Empty);
          modDialog.Save(Path.Combine(env.TslPatchDataPath, "dialog.tlk"));
        },
        env =>
        {
          string modulePath = Path.Combine(env.ModulesPath, "capsule.mod");
          var capsule = new Capsule(modulePath, createIfNotExist: false);
          byte[] tlkBytes = capsule.GetResource("dialog", ResourceType.TLK);
          tlkBytes.Should().NotBeNull();
          var moduleTlk = TLK.FromBytes(tlkBytes);
          moduleTlk.Get(0).Text.Should().Be("CapsuleLine");
        });
    }

    private static EmbeddedInstallScenario ProtectedDialogSkip()
    {
      return new EmbeddedInstallScenario(
        "inline_protected_dialog_skip",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=.

[folder0]
Replace0=dialog.tlk
",
        env =>
        {
          var dialog = new TLK(Language.English);
          dialog.Save(Path.Combine(env.GameRoot, "dialog.tlk"));
          var modDialog = new TLK(Language.English);
          modDialog.Save(Path.Combine(env.TslPatchDataPath, "dialog.tlk"));
          env.SetProtectedDialogOriginalBytes(File.ReadAllBytes(Path.Combine(env.GameRoot, "dialog.tlk")));
        },
        env =>
        {
          byte[] after = File.ReadAllBytes(Path.Combine(env.GameRoot, "dialog.tlk"));
          after.Should().Equal(env.ProtectedDialogOriginalBytes);
        });
    }

    private static EmbeddedInstallScenario BackupFilesInstallReplace()
    {
      return new EmbeddedInstallScenario(
        "inline_backup_files_replace",
        @"
[Settings]
LogLevel=3
BackupFiles=1
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
Replace0=backup_target.txt
",
        env =>
        {
          File.WriteAllText(Path.Combine(env.OverridePath, "backup_target.txt"), "original");
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "backup_target.txt"), "patched");
        },
        env =>
        {
          string backupRoot = Path.Combine(env.ModRoot, "backup");
          Directory.Exists(backupRoot).Should().BeTrue();
          string[] dirs = Directory.GetDirectories(backupRoot);
          dirs.Should().NotBeEmpty();
          string backupFile = Path.Combine(dirs[0], "Override", "backup_target.txt");
          File.Exists(backupFile).Should().BeTrue();
          File.ReadAllText(backupFile).Should().Be("original");
          File.ReadAllText(Path.Combine(env.OverridePath, "backup_target.txt")).Should().Be("patched");
        });
    }

    private static EmbeddedInstallScenario CompileModuleCapsule()
    {
      return new EmbeddedInstallScenario(
        "inline_compile_module_capsule",
        @"
[Settings]
LogLevel=3

[CompileList]
!DefaultDestination=Modules\capsule.mod
File0=main.nss
",
        env =>
        {
          string modulePath = Path.Combine(env.ModulesPath, "capsule.mod");
          new Capsule(modulePath, createIfNotExist: true).Save();
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "main.nss"), "void main() {}\n");
        },
        env =>
        {
          string modulePath = Path.Combine(env.ModulesPath, "capsule.mod");
          var capsule = new Capsule(modulePath, createIfNotExist: false);
          byte[] ncs = capsule.GetResource("main", ResourceType.NCS);
          ncs.Length.Should().BeGreaterThan(0);
          File.Exists(Path.Combine(env.OverridePath, "main.ncs")).Should().BeFalse();
        });
    }

    private static EmbeddedInstallScenario CustomNwscriptCompile()
    {
      return new EmbeddedInstallScenario(
        "inline_custom_nwscript_compile",
        @"
[Settings]
LogLevel=3
ScriptCompilerFlags=--nwscript custom.nss

[CompileList]
File0=main.nss
",
        env =>
        {
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "custom.nss"), "void Helper() {}\n");
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "main.nss"), "void Helper();\nvoid main() { Helper(); }\n");
        },
        env => InstallAssertionLadder.AssertParsesAsNcsL2(Path.Combine(env.OverridePath, "main.ncs")));
    }

    private static EmbeddedInstallScenario OverrideTypeIgnoreModule()
    {
      return new EmbeddedInstallScenario(
        "inline_override_type_ignore_module",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
module0=Modules\test.mod

[module0]
File0=shadow.ncs

[shadow.ncs]
!OverrideType=ignore
",
        env =>
        {
          string modulePath = Path.Combine(env.ModulesPath, "test.mod");
          new Capsule(modulePath, createIfNotExist: true).Save();
          byte[] shadowBytes = new byte[] { 1, 2, 3 };
          File.WriteAllBytes(Path.Combine(env.OverridePath, "shadow.ncs"), shadowBytes);
          File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "shadow.ncs"), new byte[] { 9, 9, 9 });
          env.StashBytes("shadow_override_original", shadowBytes);
        },
        env =>
        {
          byte[] original = env.RetrieveBytes("shadow_override_original");
          File.ReadAllBytes(Path.Combine(env.OverridePath, "shadow.ncs")).Should().Equal(original);
          string modulePath = Path.Combine(env.ModulesPath, "test.mod");
          var capsule = new Capsule(modulePath, createIfNotExist: false);
          capsule.GetResource("shadow", ResourceType.NCS)[0].Should().Be(9);
        });
    }
  }
}
