using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Config;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.NCS;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Mods
{
  public sealed class KPatcherINISerializerHackListTests : IDisposable
  {
    private readonly string _tempDir;
    private readonly string _modPath;

    public KPatcherINISerializerHackListTests()
    {
      _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
      _modPath = Path.Combine(_tempDir, "tslpatchdata");
      Directory.CreateDirectory(_modPath);
    }

    public void Dispose()
    {
      if (Directory.Exists(_tempDir))
      {
        try
        {
          Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
      }
    }

    [Fact]
    public void SerializeHackList_RoundTripsThroughConfigReader()
    {
      var modifications = new ModificationsByType
      {
        Ncs = new List<ModificationsNCS>
        {
          new ModificationsNCS(
            "hack.ncs",
            modifiers: new List<ModifyNCS>
            {
              new ModifyNCS(NCSTokenType.UINT8, 0, 0xAB),
              new ModifyNCS(NCSTokenType.STRREF32, 4, 2)
            })
        }
      };

      string iniText = new KPatcherINISerializer().Serialize(
        modifications,
        includeHeader: false,
        includeSettings: false);

      iniText.Should().Contain("[HACKList]");
      iniText.Should().Contain("hack.ncs=hack.ncs");
      iniText.Should().Contain("0x0=u8:171");
      iniText.Should().Contain("0x4=i32:StrRef2");

      string iniPath = Path.Combine(_modPath, "changes.ini");
      File.WriteAllText(iniPath, iniText);

      var reader = ConfigReader.FromFilePath(iniPath, tslPatchDataPath: _modPath);
      PatcherConfig config = reader.Load(new PatcherConfig());

      config.PatchesNCS.Should().ContainSingle();
      ModificationsNCS roundTrip = config.PatchesNCS[0];
      roundTrip.SourceFile.Should().Be("hack.ncs");
      roundTrip.Modifiers.Should().HaveCount(2);
      roundTrip.Modifiers[0].TokenType.Should().Be(NCSTokenType.UINT8);
      roundTrip.Modifiers[0].Offset.Should().Be(0);
      roundTrip.Modifiers[0].TokenIdOrValue.Should().Be(0xAB);
      roundTrip.Modifiers[1].TokenType.Should().Be(NCSTokenType.STRREF32);
      roundTrip.Modifiers[1].Offset.Should().Be(4);
      roundTrip.Modifiers[1].TokenIdOrValue.Should().Be(2);
    }
  }
}
