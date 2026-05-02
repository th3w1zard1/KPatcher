using System;
using System.IO;
using System.Linq;
using KPatcher.Core.Formats.NCS;

var o = NCSAuto.ReadNcs(@"c:\GitHub\KPatcher\test-work\roundtrip-work\k1\K1\Data\scripts.bif\k_act_atkonend.ncs");
var m = NCSAuto.ReadNcs(@"c:\GitHub\KPatcher\test-work\roundtrip-work\k1\K1\Data\scripts.bif\k_act_atkonend.kcompiler.ncs");
Console.WriteLine($"orig ins {o.Instructions.Count} managed {m.Instructions.Count}");
int n = Math.Min(o.Instructions.Count, m.Instructions.Count);
for (int i = 0; i < n; i++)
{
    var a = o.Instructions[i];
    var b = m.Instructions[i];
    if (a.InsType != b.InsType || !SameArgs(a,b))
    {
        Console.WriteLine($"first diff at instruction index {i}");
        Console.WriteLine($"  orig: {a}");
        Console.WriteLine($"  man:  {b}");
        for (int j = Math.Max(0,i-2); j < Math.Min(n, i+5); j++)
            Console.WriteLine($"  [{j}] o={o.Instructions[j]} | m={m.Instructions[j]}");
        break;
    }
}
static bool SameArgs(NCSInstruction a, NCSInstruction b)
{
    if (a.Args == null && b.Args == null) return true;
    if (a.Args == null || b.Args == null) return false;
    if (a.Args.Count != b.Args.Count) return false;
    for (int i = 0; i < a.Args.Count; i++)
        if (!Equals(a.Args[i], b.Args[i])) return false;
    return true;
}
