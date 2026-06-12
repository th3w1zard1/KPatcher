using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerCrossStageMemoryIntegrationTests : ModInstallerIntegrationTestBase
    {
        public ModInstallerCrossStageMemoryIntegrationTests()
            : base("KPatcher_CrossStageMem_")
        {
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
            File.WriteAllBytes(Path.Combine(OverridePath, "memory.2da"), twoda.ToBytes());

            var ssf = new SSF();
            ssf.SetData(SSFSound.BATTLE_CRY_2, 0);
            File.WriteAllBytes(Path.Combine(OverridePath, "memory.ssf"), ssf.ToBytes());

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
            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            var patchedSsf = SSF.FromBytes(File.ReadAllBytes(Path.Combine(OverridePath, "memory.ssf")));
            patchedSsf.Get(SSFSound.BATTLE_CRY_2).Should().Be(456);
        }

        [Fact]
        public void Install_TlkMemoryBeforeGff_ResolvesStrRefAtApplyTime()
        {
            var dialogTlk = new TLK(Language.English);
            dialogTlk.Add("Existing", string.Empty);
            dialogTlk.Save(Path.Combine(GameRoot, "dialog.tlk"));

            var appendTlk = new TLK(Language.English);
            appendTlk.Add("CrossStageLine", string.Empty);
            appendTlk.Save(Path.Combine(TslPatchDataPath, "append.tlk"));

            var gff = new GFF();
            gff.Root.SetUInt32("StrRefField", 999u);
            File.WriteAllBytes(Path.Combine(OverridePath, "cross.gff"), gff.ToBytes());

            WriteChangesIni(@"
[Settings]
LogLevel=3

[TLKList]
StrRef0=0

[append.tlk]
0=CrossStageLine

[GFFList]
File0=cross.gff

[cross.gff]
StrRefField=StrRef0
");

            var installer = new ModInstaller(ModRoot, GameRoot, Path.Combine(TslPatchDataPath, "changes.ini"), new PatchLogger());
            installer.Install();

            var patchedGff = GFF.FromBytes(File.ReadAllBytes(Path.Combine(OverridePath, "cross.gff")));
            patchedGff.Root.GetUInt32("StrRefField").Should().Be(1u);
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(TslPatchDataPath, "changes.ini"), body);
        }
    }
}
