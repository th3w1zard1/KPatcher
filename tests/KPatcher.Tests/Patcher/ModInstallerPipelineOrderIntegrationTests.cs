using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Logger;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher
{
  public sealed class ModInstallerPipelineOrderIntegrationTests
  {
    [Fact]
    public void Install_QueuesPatchesInBinaryVerifiedTslPatcherOrder()
    {
      using (var env = new ModInstallerIntegrationEnvironment("KPatcher_PipelineOrder_"))
      {
        PipelineOrderFixtures.SeedFullPipelineAssets(env);
        env.WriteChangesIni(PipelineOrderFixtures.FullPipelineIniBody);

        var logger = new PatchLogger();
        env.CreateInstaller(logger).Install();

        List<string> patchTypes = PipelineOrderFixtures.GetPatchTypes(logger);

        patchTypes.Should().Contain(new[]
        {
          "ModificationsTLK",
          "ModificationsGFF",
          "Modifications2DA",
          "InstallFile",
          "ModificationsNCS",
          "ModificationsNSS",
          "ModificationsSSF"
        });

        int tlk = patchTypes.IndexOf("ModificationsTLK");
        int gff = patchTypes.IndexOf("ModificationsGFF");
        int twoda = patchTypes.IndexOf("Modifications2DA");
        int install = patchTypes.IndexOf("InstallFile");
        int ncs = patchTypes.IndexOf("ModificationsNCS");
        int nss = patchTypes.IndexOf("ModificationsNSS");
        int ssf = patchTypes.IndexOf("ModificationsSSF");

        tlk.Should().BeLessThan(gff);
        gff.Should().BeLessThan(twoda);
        twoda.Should().BeLessThan(install);
        install.Should().BeLessThan(ncs);
        ncs.Should().BeLessThan(nss);
        nss.Should().BeLessThan(ssf);
      }
    }

    [Fact]
    public void Install_AppliesPostInstallBytesForEachPipelineStage()
    {
      using (var env = new ModInstallerIntegrationEnvironment("KPatcher_PipelineBytes_"))
      {
        PipelineOrderFixtures.SeedFullPipelineAssets(env);
        env.WriteChangesIni(PipelineOrderFixtures.FullPipelineIniBody);

        env.CreateInstaller(new PatchLogger()).Install();

        var gff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "test.gff")));
        gff.Root.GetUInt8("Field1").Should().Be(2);
        gff.Root.GetUInt32("StrRefField").Should().Be(1u);

        var twoda = TwoDAFile.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "test.2da")));
        twoda.GetRow(0).GetString("label").Should().Be("patched");

        InstallAssertionLadder.AssertTextEqualL1(
          Path.Combine(env.OverridePath, "install_marker.txt"),
          "marker");

        File.ReadAllBytes(Path.Combine(env.OverridePath, "hack.ncs"))[0].Should().Be(7);

        string mainNcsPath = Path.Combine(env.OverridePath, "main.ncs");
        InstallAssertionLadder.AssertParsesAsNcsL2(mainNcsPath);

        var ssf = SSF.FromBytes(File.ReadAllBytes(Path.Combine(env.OverridePath, "test.ssf")));
        ssf.Get(SSFSound.BATTLE_CRY_1).Should().Be(123);
      }
    }

    [Fact]
    public void Install_InstallerModeFalse_OmitsInstallFileFromQueue()
    {
      using (var env = new ModInstallerIntegrationEnvironment("KPatcher_PipelineMode_"))
      {
        File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });

        env.WriteChangesIni(@"
[Settings]
LogLevel=3

[InstallList]
folder0=Override

[folder0]
File0=install_marker.txt

[HACKList]
hack.ncs=hack.ncs

[hack.ncs]
0x0=u8:7
");

        var logger = new PatchLogger();
        env.CreateInstaller(logger).Install();

        List<string> patchTypes = PipelineOrderFixtures.GetPatchTypes(logger);
        patchTypes.Should().NotContain("InstallFile");
        patchTypes.Should().Contain("ModificationsNCS");
      }
    }
  }
}
