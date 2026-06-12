using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerCrossStageMemoryIntegrationTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly string _modRoot;
        private readonly string _gameRoot;
        private readonly string _tslPatchDataPath;
        private readonly string _overridePath;

        public ModInstallerCrossStageMemoryIntegrationTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_CrossStageMem_" + Guid.NewGuid().ToString("N"));
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
        public void Install_2DAMemoryPopulatedBeforeSsf_ResolvesTokenAtApplyTime()
        {
            var twoda = new TwoDAFile(new List<string> { "label", "soundref" });
            twoda.AddRow("0", new Dictionary<string, object>
            {
                { "label", "0" },
                { "soundref", "456" }
            });
            File.WriteAllBytes(Path.Combine(_overridePath, "memory.2da"), twoda.ToBytes());

            var ssf = new SSF();
            ssf.SetData(SSFSound.BATTLE_CRY_2, 0);
            File.WriteAllBytes(Path.Combine(_overridePath, "memory.ssf"), ssf.ToBytes());

            WriteChangesIni(@"
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
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            var patchedSsf = SSF.FromBytes(File.ReadAllBytes(Path.Combine(_overridePath, "memory.ssf")));
            patchedSsf.Get(SSFSound.BATTLE_CRY_2).Should().Be(456);
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
        }
    }
}
