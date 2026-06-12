using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher.Support
{
  /// <summary>
  /// Inline characterization scenarios (no committed mod trees). Legacy manifest rows without inline bodies
  /// remain inventoried in <c>manifest.json</c> for maintainer migration.
  /// </summary>
  public static partial class EmbeddedScenarioDefinitions
  {
    public static IReadOnlyList<EmbeddedInstallScenario> RunnableScenarios { get; } = BuildRunnableScenarios();

    private static List<EmbeddedInstallScenario> BuildRunnableScenarios()
    {
      var scenarios = new List<EmbeddedInstallScenario>
      {
        SettingsOnly(),
        InstallMarker(),
        TwoDaChangeRow(),
        HackBinary(),
        GffUInt8Field(),
        TlkAppendGffStrRef(),
        SsfBattlecry(),
        CompileVoidMain(),
        InstallReplaceOverride(),
        TwoDaAddColumn(),
        InstallerModeFalseHackOnly(),
        NamespaceSubfolderIni()
      };
      scenarios.AddRange(ParityPatternScenarios());
      return scenarios;
    }

    private static EmbeddedInstallScenario SettingsOnly()
    {
      return new EmbeddedInstallScenario(
        "inline_settings_only",
        "[Settings]\nLogLevel=3\n",
        env => { });
    }

    private static EmbeddedInstallScenario InstallMarker()
    {
      return new EmbeddedInstallScenario(
        "inline_install_marker",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=marker.txt
",
        env => File.WriteAllText(Path.Combine(env.TslPatchDataPath, "marker.txt"), "installed"),
        env => InstallAssertionLadder.AssertTextEqualL1(
          Path.Combine(env.OverridePath, "marker.txt"),
          "installed"));
    }

    private static EmbeddedInstallScenario TwoDaChangeRow()
    {
      return new EmbeddedInstallScenario(
        "inline_2da_change_row",
        @"
[Settings]
LogLevel=3

[2DAList]
Table0=patch.2da

[patch.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=patched
",
        env =>
        {
          var twoda = new TwoDAFile(new List<string> { "label" });
          twoda.AddRow("0", new Dictionary<string, object> { { "label", "original" } });
          File.WriteAllBytes(Path.Combine(env.OverridePath, "patch.2da"), twoda.ToBytes());
        },
        env =>
        {
          var patched = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "patch.2da")));
          patched.GetRow(0).GetString("label").Should().Be("patched");
        });
    }

    private static EmbeddedInstallScenario HackBinary()
    {
      return new EmbeddedInstallScenario(
        "inline_hack_byte",
        @"
[Settings]
LogLevel=3

[HACKList]
Replace0=marker.bin

[marker.bin]
0x0=u8:55
",
        env =>
        {
          byte[] bytes = new byte[] { 0, 0, 0, 0 };
          File.WriteAllBytes(Path.Combine(env.OverridePath, "marker.bin"), bytes);
          File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "marker.bin"), bytes);
        },
        env => File.ReadAllBytes(Path.Combine(env.OverridePath, "marker.bin"))[0].Should().Be(55));
    }

    private static EmbeddedInstallScenario GffUInt8Field()
    {
      return new EmbeddedInstallScenario(
        "inline_gff_uint8",
        @"
[Settings]
LogLevel=3

[GFFList]
File0=patch.gff

[patch.gff]
Field1=42
",
        env =>
        {
          var gff = new GFF();
          gff.Root.SetUInt8("Field1", 1);
          File.WriteAllBytes(Path.Combine(env.OverridePath, "patch.gff"), gff.ToBytes());
        },
        env =>
        {
          var gff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "patch.gff")));
          gff.Root.GetUInt8("Field1").Should().Be(42);
        });
    }

    private static EmbeddedInstallScenario TlkAppendGffStrRef()
    {
      return new EmbeddedInstallScenario(
        "inline_tlk_gff_strref",
        @"
[Settings]
LogLevel=3

[TLKList]
StrRef0=0

[append.tlk]
0=InlineLine

[GFFList]
File0=token.gff

[token.gff]
StrRefField=StrRef0
",
        env =>
        {
          var dialog = new TLK(Language.English);
          dialog.Add("Existing", string.Empty);
          dialog.Save(Path.Combine(env.GameRoot, "dialog.tlk"));

          var append = new TLK(Language.English);
          append.Add("InlineLine", string.Empty);
          append.Save(Path.Combine(env.TslPatchDataPath, "append.tlk"));

          var gff = new GFF();
          gff.Root.SetUInt32("StrRefField", 0u);
          File.WriteAllBytes(Path.Combine(env.OverridePath, "token.gff"), gff.ToBytes());
        },
        env =>
        {
          var gff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "token.gff")));
          gff.Root.GetUInt32("StrRefField").Should().Be(1u);
        });
    }

    private static EmbeddedInstallScenario SsfBattlecry()
    {
      return new EmbeddedInstallScenario(
        "inline_ssf_battlecry",
        @"
[Settings]
LogLevel=3

[SSFList]
File0=patch.ssf

[patch.ssf]
Battlecry 1=321
",
        env =>
        {
          var ssf = new SSF();
          File.WriteAllBytes(Path.Combine(env.OverridePath, "patch.ssf"), ssf.ToBytes());
        },
        env =>
        {
          var ssf = SSF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "patch.ssf")));
          ssf.Get(SSFSound.BATTLE_CRY_1).Should().Be(321);
        });
    }

    private static EmbeddedInstallScenario CompileVoidMain()
    {
      return new EmbeddedInstallScenario(
        "inline_compile_void_main",
        @"
[Settings]
LogLevel=3

[CompileList]
File0=main.nss
",
        env => File.WriteAllText(Path.Combine(env.TslPatchDataPath, "main.nss"), "void main() {}\n"),
        env => InstallAssertionLadder.AssertParsesAsNcsL2(Path.Combine(env.OverridePath, "main.ncs")));
    }

    private static EmbeddedInstallScenario InstallReplaceOverride()
    {
      return new EmbeddedInstallScenario(
        "inline_install_replace",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
Replace0=target.txt
",
        env =>
        {
          File.WriteAllText(Path.Combine(env.OverridePath, "target.txt"), "original");
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "target.txt"), "replaced");
        },
        env => InstallAssertionLadder.AssertTextEqualL1(
          Path.Combine(env.OverridePath, "target.txt"),
          "replaced"));
    }

    private static EmbeddedInstallScenario TwoDaAddColumn()
    {
      return new EmbeddedInstallScenario(
        "inline_2da_add_column",
        @"
[Settings]
LogLevel=3

[2DAList]
Table0=cols.2da

[cols.2da]
AddColumn0=add_col

[add_col]
ColumnLabel=NewCol
DefaultValue=****
",
        env =>
        {
          var twoda = new TwoDAFile(new List<string> { "Col1" });
          twoda.AddRow("0", new Dictionary<string, object> { { "Col1", "A" } });
          File.WriteAllBytes(Path.Combine(env.OverridePath, "cols.2da"), twoda.ToBytes());
        },
        env =>
        {
          var patched = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "cols.2da")));
          patched.GetHeaders().Should().Contain("NewCol");
        });
    }

    private static EmbeddedInstallScenario InstallerModeFalseHackOnly()
    {
      return new EmbeddedInstallScenario(
        "inline_installer_mode_false_hack",
        @"
[Settings]
LogLevel=3

[InstallList]
folder0=Override

[folder0]
File0=skip.txt

[HACKList]
hack.ncs=hack.ncs

[hack.ncs]
0x0=u8:9
",
        env =>
        {
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "skip.txt"), "skip");
          File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });
        },
        env =>
        {
          File.Exists(Path.Combine(env.OverridePath, "skip.txt")).Should().BeFalse();
          File.ReadAllBytes(Path.Combine(env.OverridePath, "hack.ncs"))[0].Should().Be(9);
        });
    }

    private static EmbeddedInstallScenario NamespaceSubfolderIni()
    {
      return new EmbeddedInstallScenario(
        "inline_namespace_subfolder",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=ns_marker.txt
",
        env =>
        {
          string nsDir = Path.Combine(env.TslPatchDataPath, "alt_ns");
          Directory.CreateDirectory(nsDir);
          File.WriteAllText(Path.Combine(nsDir, "ns_marker.txt"), "ns_installed");
        },
        env => InstallAssertionLadder.AssertTextEqualL1(
          Path.Combine(env.OverridePath, "ns_marker.txt"),
          "ns_installed"),
        "alt_ns/changes.ini");
    }
  }
}
