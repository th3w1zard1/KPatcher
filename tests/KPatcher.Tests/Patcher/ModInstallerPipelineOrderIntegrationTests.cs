using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher
{
  public sealed class ModInstallerPipelineOrderIntegrationTests : IDisposable
  {
    private static readonly Regex PatchOrderRegex = new Regex(
      @"Install patch \d+/\d+: clrType=(?<type>\w+)",
      RegexOptions.CultureInvariant);

    private readonly string _tempRoot;
    private readonly string _modRoot;
    private readonly string _gameRoot;
    private readonly string _tslPatchDataPath;
    private readonly string _overridePath;

    public ModInstallerPipelineOrderIntegrationTests()
    {
      _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_PipelineOrder_" + Guid.NewGuid().ToString("N"));
      _modRoot = Path.Combine(_tempRoot, "mod");
      _gameRoot = Path.Combine(_tempRoot, "game");
      _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");
      _overridePath = Path.Combine(_gameRoot, "Override");
      Directory.CreateDirectory(_tslPatchDataPath);
      Directory.CreateDirectory(_overridePath);
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
    public void Install_QueuesPatchesInBinaryVerifiedTslPatcherOrder()
    {
      SeedGameAndModAssets();

      WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[TLKList]
StrRef0=0

[append.tlk]
0=Appended

[2DAList]
Table0=test.2da

[test.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=patched

[GFFList]
File0=test.gff

[test.gff]
Field1=2

[InstallList]
folder0=Override

[folder0]
File0=install_marker.txt

[HACKList]
hack.ncs=hack.ncs

[hack.ncs]
0x0=u8:7

[CompileList]
File0=main.nss

[SSFList]
File0=test.ssf

[test.ssf]
Battlecry 1=123
");

      var logger = new PatchLogger();
      var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

      installer.Install();

      List<string> patchTypes = logger.Diagnostics
        .Select(log => log.Message)
        .Select(message =>
        {
          Match match = PatchOrderRegex.Match(message);
          return match.Success ? match.Groups["type"].Value : null;
        })
        .Where(type => type != null)
        .ToList();

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

    [Fact]
    public void Install_InstallerModeFalse_OmitsInstallFileFromQueue()
    {
      File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });

      WriteChangesIni(@"
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
      var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

      installer.Install();

      List<string> patchTypes = logger.Diagnostics
        .Select(log => log.Message)
        .Select(message =>
        {
          Match match = PatchOrderRegex.Match(message);
          return match.Success ? match.Groups["type"].Value : null;
        })
        .Where(type => type != null)
        .ToList();

      patchTypes.Should().NotContain("InstallFile");
      patchTypes.Should().Contain("ModificationsNCS");
    }

    private void SeedGameAndModAssets()
    {
      var dialogTlk = new TLK(Language.English);
      dialogTlk.Add("Existing", "vo_existing");
      dialogTlk.Save(Path.Combine(_gameRoot, "dialog.tlk"));

      var appendTlk = new TLK(Language.English);
      appendTlk.Add("Appended", "vo_appended");
      appendTlk.Save(Path.Combine(_tslPatchDataPath, "append.tlk"));

      var twoda = new TwoDAFile(new List<string> { "label" });
      twoda.AddRow("0", new Dictionary<string, object> { { "label", "original" } });
      File.WriteAllBytes(Path.Combine(_overridePath, "test.2da"), twoda.ToBytes());

      var gff = new GFF();
      gff.Root.SetUInt8("Field1", 1);
      File.WriteAllBytes(Path.Combine(_overridePath, "test.gff"), gff.ToBytes());

      var ssf = new SSF();
      File.WriteAllBytes(Path.Combine(_overridePath, "test.ssf"), ssf.ToBytes());

      File.WriteAllText(Path.Combine(_tslPatchDataPath, "install_marker.txt"), "marker");
      File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });
      File.WriteAllText(Path.Combine(_tslPatchDataPath, "main.nss"), "void main() {}\n");
    }

    private void WriteChangesIni(string body)
    {
      File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
    }
  }
}
