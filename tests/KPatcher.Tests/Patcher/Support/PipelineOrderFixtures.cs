using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Logger;

namespace KPatcher.Core.Tests.Patcher.Support
{
    public static class PipelineOrderFixtures
    {
        public static readonly Regex PatchOrderRegex = new Regex(
            @"Install patch \d+/\d+: clrType=(?<type>\w+)",
            RegexOptions.CultureInvariant);

        public const string FullPipelineIniBody = @"
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
StrRefField=StrRef0

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
";

        public static void SeedFullPipelineAssets(ModInstallerIntegrationEnvironment env)
        {
            var dialogTlk = new TLK(Language.English);
            dialogTlk.Add("Existing", "vo_existing");
            dialogTlk.Save(Path.Combine(env.GameRoot, "dialog.tlk"));

            var appendTlk = new TLK(Language.English);
            appendTlk.Add("Appended", "vo_appended");
            appendTlk.Save(Path.Combine(env.TslPatchDataPath, "append.tlk"));

            var twoda = new TwoDAFile(new List<string> { "label" });
            twoda.AddRow("0", new Dictionary<string, object> { { "label", "original" } });
            File.WriteAllBytes(Path.Combine(env.OverridePath, "test.2da"), twoda.ToBytes());

            var gff = new GFF();
            gff.Root.SetUInt8("Field1", 1);
            gff.Root.SetUInt32("StrRefField", 0);
            File.WriteAllBytes(Path.Combine(env.OverridePath, "test.gff"), gff.ToBytes());

            var ssf = new SSF();
            File.WriteAllBytes(Path.Combine(env.OverridePath, "test.ssf"), ssf.ToBytes());

            File.WriteAllText(Path.Combine(env.TslPatchDataPath, "install_marker.txt"), "marker");
            File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "hack.ncs"), new byte[] { 0, 0, 0, 0 });
            File.WriteAllText(Path.Combine(env.TslPatchDataPath, "main.nss"), "void main() {}\n");
        }

        public static List<string> GetPatchTypes(PatchLogger logger)
        {
            return logger.Diagnostics
                .Select(log => log.Message)
                .Select(message =>
                {
                    Match match = PatchOrderRegex.Match(message);
                    return match.Success ? match.Groups["type"].Value : null;
                })
                .Where(type => type != null)
                .ToList();
        }
    }
}
