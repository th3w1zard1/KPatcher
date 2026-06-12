using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Config;
using KPatcher.Core.Mods;
using KPatcher.Core.Mods.NSS;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Mods
{
    public sealed class KPatcherINISerializerCompileListTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _modPath;

        public KPatcherINISerializerCompileListTests()
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
        public void SerializeCompileList_RoundTripsThroughConfigReader()
        {
            var modifications = new ModificationsByType
            {
                Nss = new List<ModificationsNSS>
                {
                    new ModificationsNSS("compile.nss", replaceFile: false)
                    {
                        Destination = "Override",
                        OverrideTypeValue = OverrideType.WARN
                    }
                }
            };

            string iniText = new KPatcherINISerializer().Serialize(
                modifications,
                includeHeader: false,
                includeSettings: false);

            iniText.Should().Contain("[CompileList]");
            iniText.Should().Contain("File0=compile.nss");
            iniText.Should().Contain("[compile.nss]");
            iniText.Should().Contain("!OverrideType=warn");

            string iniPath = Path.Combine(_modPath, "changes.ini");
            File.WriteAllText(iniPath, iniText);

            var reader = ConfigReader.FromFilePath(iniPath, tslPatchDataPath: _modPath);
            PatcherConfig config = reader.Load(new PatcherConfig());

            config.PatchesNSS.Should().ContainSingle();
            ModificationsNSS roundTrip = config.PatchesNSS[0];
            roundTrip.SourceFile.Should().Be("compile.nss");
            roundTrip.ReplaceFile.Should().BeFalse();
            roundTrip.OverrideTypeValue.Should().Be(OverrideType.WARN);
        }
    }
}
