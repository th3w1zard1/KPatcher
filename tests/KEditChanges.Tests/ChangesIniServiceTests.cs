using System.IO;
using System;
using Xunit;
using KEditChanges;
using KEditChanges.Ini;

namespace KEditChanges.Tests
{
    public sealed class ChangesIniServiceTests
    {
        private const string SampleIni =
@"[Settings]
LogLevel=3
WindowCaption=Hello<#LF#>World
ConfirmMessage=OK
InstallerMode=1

[TLKList]
";

        [Fact]
        public void Load_inline_ini_decodes_window_caption()
        {
            var service = new ChangesIniService();
            ChangesIniDocument doc = service.LoadFromIniText(SampleIni);
            Assert.Equal("Hello\nWorld", doc.Config.WindowTitle);
            Assert.True(doc.Config.InstallerMode);
        }

        [Fact]
        public void Serialize_preserves_tst_escapes_in_settings()
        {
            var service = new ChangesIniService();
            ChangesIniDocument doc = service.LoadFromIniText(SampleIni);
            string text = service.Serialize(doc);
            Assert.Contains("WindowCaption=Hello<#LF#>World", text);
            Assert.Contains("[Settings]", text);
        }

        [Fact]
        public void Round_trip_through_temp_file()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "keditchanges-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string iniPath = Path.Combine(tempDir, "changes.ini");
            try
            {
                File.WriteAllText(iniPath, SampleIni);
                var service = new ChangesIniService();
                ChangesIniDocument loaded = service.Load(iniPath);
                string outPath = Path.Combine(tempDir, "out.ini");
                service.Save(loaded, outPath, includeHeader: false);
                ChangesIniDocument reloaded = service.Load(outPath);
                Assert.Equal(loaded.Config.WindowTitle, reloaded.Config.WindowTitle);
                Assert.Equal(loaded.Config.InstallerMode, reloaded.Config.InstallerMode);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }

        [Fact]
        public void Summary_reports_patch_counts()
        {
            var service = new ChangesIniService();
            ChangesIniDocument doc = service.LoadFromIniText(SampleIni);
            ChangesIniSummary summary = service.BuildSummary(doc);
            Assert.Equal("Hello\nWorld", summary.WindowTitle);
            Assert.Equal(3, summary.LogLevel);
            Assert.True(summary.InstallerMode);
        }

        [Fact]
        public void ChangeEditReMapping_info_is_non_empty()
        {
            Assert.Contains("KEditChanges", ChangeEditReMapping.Info);
            Assert.Equal("0x004b03a0", ChangeEditReMapping.EntryAddress);
        }
    }
}
