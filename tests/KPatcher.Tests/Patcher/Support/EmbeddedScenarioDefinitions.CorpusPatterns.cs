using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.GFF;

namespace KPatcher.Core.Tests.Patcher.Support
{
  public static partial class EmbeddedScenarioDefinitions
  {
    internal static IEnumerable<EmbeddedInstallScenario> CorpusPatternScenarios()
    {
      yield return GffAddFieldNested();
      yield return InstallSourceSubfolder();
      yield return HackRenameSource();
    }

    private static EmbeddedInstallScenario GffAddFieldNested()
    {
      return new EmbeddedInstallScenario(
        "inline_gff_add_field",
        @"
[Settings]
LogLevel=3

[GFFList]
File0=nested.gff

[nested.gff]
AddField0=add_byte

[add_byte]
FieldType=Byte
Label=AddedByte
Value=7
",
        env =>
        {
          var gff = new GFF();
          File.WriteAllBytes(Path.Combine(env.OverridePath, "nested.gff"), gff.ToBytes());
        },
        env =>
        {
          var gff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "nested.gff")));
          gff.Root.GetUInt8("AddedByte").Should().Be(7);
        });
    }

    private static EmbeddedInstallScenario InstallSourceSubfolder()
    {
      return new EmbeddedInstallScenario(
        "inline_install_source_subfolder",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
!SourceFolder=assets
File0=subfile.txt
",
        env =>
        {
          string assetsDir = Path.Combine(env.TslPatchDataPath, "assets");
          Directory.CreateDirectory(assetsDir);
          File.WriteAllText(Path.Combine(assetsDir, "subfile.txt"), "from_assets");
        },
        env => InstallAssertionLadder.AssertTextEqualL1(
          Path.Combine(env.OverridePath, "subfile.txt"),
          "from_assets"));
    }

    private static EmbeddedInstallScenario HackRenameSource()
    {
      return new EmbeddedInstallScenario(
        "inline_hack_rename_source",
        @"
[Settings]
LogLevel=3

[HACKList]
script.ncs=script.ncs

[script.ncs]
!SourceFile=source-alt.ncs
!SaveAs=patched.ncs
0x0=u8:1
",
        env => File.WriteAllBytes(
          Path.Combine(env.TslPatchDataPath, "source-alt.ncs"),
          new byte[] { 0, 2, 3, 4 }),
        env => File.ReadAllBytes(Path.Combine(env.OverridePath, "patched.ncs"))[0].Should().Be(1));
    }
  }
}
