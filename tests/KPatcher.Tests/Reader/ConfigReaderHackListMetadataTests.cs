using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Logger;
using KPatcher.Core.Mods;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    public class ConfigReaderHackListMetadataTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _iniFilePath;
        private readonly string _modPath;

        public ConfigReaderHackListMetadataTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _modPath = Path.Combine(_tempDir, "tslpatchdata");
            Directory.CreateDirectory(_modPath);
            _iniFilePath = Path.Combine(_modPath, "changes.ini");
        }

        public void Dispose()
        {
            if (!Directory.Exists(_tempDir))
            {
                return;
            }

            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }

        [Fact]
        public void LoadHackList_PerFileDestination_IsIgnoredButSourceAndSaveAsAreApplied()
        {
            string iniContent = @"[Settings]
ModName=Test Mod

[HACKList]
script.ncs=script.ncs

[script.ncs]
!SourceFile=source-alt.ncs
!SaveAs=patched.ncs
!Destination=Modules
0x0=u8:1
";
            File.WriteAllText(_iniFilePath, iniContent);

            var logger = new PatchLogger();
            var reader = ConfigReader.FromFilePath(_iniFilePath, logger);

            var result = reader.Load(reader.Config);

            result.PatchesNCS.Should().ContainSingle();
            result.PatchesNCS[0].SourceFile.Should().Be("source-alt.ncs");
            result.PatchesNCS[0].SaveAs.Should().Be("patched.ncs");
            result.PatchesNCS[0].Destination.Should().Be(PatcherModifications.DEFAULT_DESTINATION);
        }
    }
}
