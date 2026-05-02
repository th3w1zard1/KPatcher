using System.Collections.Generic;
using System.Text;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.ERF;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Formats.RIM;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Formats.TwoDA;
using KPatcher.Core.Resources;
using TlkFile = KPatcher.Core.Formats.TLK.TLK;

namespace KPatcher.Core.Tests.Common
{
    /// <summary>
    /// Canonical format graphs built in code for unit tests (no on-disk <c>test_files</c>).
    /// </summary>
    internal static class BinaryFormatFixtures
    {
        internal static GFF BuildCanonicalScalarGff()
        {
            var gff = new GFF(GFFContent.GFF);
            GFFStruct root = gff.Root;

            root.SetUInt8("uint8", 255);
            root.SetInt8("int8", -127);
            root.SetUInt16("uint16", 0xFFFF);
            root.SetInt16("int16", -32768);
            root.SetUInt32("uint32", 0xFFFFFFFF);
            root.SetInt32("int32", -2147483648);
            root.SetUInt64("uint64", 4294967296UL);
            root.SetSingle("single", 12.34567f);
            root.SetDouble("double", 12.345678901234);
            root.SetString("string", "abcdefghij123456789");
            root.SetResRef("resref", new ResRef("resref01"));
            root.SetBinary("binary", Encoding.ASCII.GetBytes("binarydata"));
            root.SetVector4("orientation", new Vector4(1, 2, 3, 4));
            root.SetVector3("position", new Vector3(11, 22, 33));

            var loc = new LocalizedString(-1);
            loc.SetData(Language.English, Gender.Male, "male_eng");
            loc.SetData(Language.German, Gender.Female, "fem_german");
            root.SetLocString("locstring", loc);

            var child = new GFFStruct(0);
            child.SetUInt8("child_uint8", 4);
            root.SetStruct("child_struct", child);

            var list = new GFFList();
            list.Add(1);
            list.Add(2);
            root.SetList("list", list);

            return gff;
        }

        internal static TwoDA BuildCanonicalTwoDA()
        {
            var twoda = new TwoDA(new List<string> { "col1", "col2", "col3" });
            twoda.AddRow(
                null,
                new Dictionary<string, object>
                {
                    ["col1"] = "abc",
                    ["col2"] = "def",
                    ["col3"] = "ghi"
                });
            twoda.AddRow(
                null,
                new Dictionary<string, object>
                {
                    ["col1"] = "def",
                    ["col2"] = "ghi",
                    ["col3"] = "123"
                });
            twoda.AddRow(
                null,
                new Dictionary<string, object>
                {
                    ["col1"] = "123",
                    ["col2"] = "",
                    ["col3"] = "abc"
                });
            return twoda;
        }

        internal static ERF BuildCanonicalErf()
        {
            var erf = new ERF(ERFType.ERF);
            erf.SetData("1", ResourceType.TXT, Encoding.ASCII.GetBytes("abc"));
            erf.SetData("2", ResourceType.TXT, Encoding.ASCII.GetBytes("def"));
            erf.SetData("3", ResourceType.TXT, Encoding.ASCII.GetBytes("ghi"));
            return erf;
        }

        internal static RIM BuildCanonicalRim()
        {
            var rim = new RIM();
            rim.SetData("1", ResourceType.TXT, Encoding.ASCII.GetBytes("abc"));
            rim.SetData("2", ResourceType.TXT, Encoding.ASCII.GetBytes("def"));
            rim.SetData("3", ResourceType.TXT, Encoding.ASCII.GetBytes("ghi"));
            return rim;
        }

        internal static SSF BuildCanonicalSsf()
        {
            var ssf = new SSF();
            for (int i = 0; i < 28; i++)
            {
                ssf[(SSFSound)i] = 123075 - i;
            }

            return ssf;
        }

        internal static TlkFile BuildTalkTableFixtureTlk()
        {
            var tlk = new TlkFile(Language.English);
            tlk.Add("abcdef", "resref01");
            tlk.Add("ghijklmnop", "resref02");
            tlk.Add("qrstuvwxyz", "");
            return tlk;
        }
    }
}
