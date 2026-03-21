using System;
using System.Linq;
using FluentAssertions;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Shared assertions for comparing <see cref="NCS"/> graphs (instruction type, args, jumps, constants),
    /// aligned with checks in <see cref="VanillaNSSCompileTests"/>.
    /// </summary>
    internal static class NcsRoundTripAssertHelpers
    {
        /// <summary>
        /// Whitespace / newline hygiene only — not the full <c>DeNCSCLIRoundTripTest.normalizeNewlines</c> pipeline
        /// from vendor DeNCS (which strips comments, reorders functions, normalizes constants, etc.).
        /// Set <c>NSS_ROUNDTRIP_EXACT=1</c> on the external round-trip test to require raw string equality instead.
        /// </summary>
        internal static string NormalizeNssForLooseTextCompare(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = s.Split('\n');
            var trimmed = lines
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(line => line.Replace("\t", "    "))
                .ToArray();
            return string.Join("\n", trimmed).TrimEnd('\n');
        }

        internal static void AssertNcsStructurallyEqual(NCS expected, NCS actual, string context)
        {
            actual.Should().NotBeNull($"{context}: actual NCS");
            expected.Should().NotBeNull($"{context}: expected NCS");
            actual.Instructions.Should().NotBeNull($"{context}: actual instructions");
            expected.Instructions.Should().NotBeNull($"{context}: expected instructions");
            actual.Instructions.Count.Should().Be(expected.Instructions.Count, $"{context}: instruction count");

            var jumpExp = expected.Instructions.Where(i => i.IsJumpInstruction()).ToList();
            var jumpAct = actual.Instructions.Where(i => i.IsJumpInstruction()).ToList();
            jumpAct.Count.Should().Be(jumpExp.Count, $"{context}: jump instruction count");

            for (int i = 0; i < expected.Instructions.Count; i++)
            {
                var oe = expected.Instructions[i];
                var oa = actual.Instructions[i];
                oa.InsType.Should().Be(oe.InsType, $"{context}: instruction {i} type");
                oa.Args.Count.Should().Be(oe.Args.Count, $"{context}: instruction {i} arg count");
                for (int j = 0; j < oe.Args.Count; j++)
                {
                    object a = oe.Args[j];
                    object b = oa.Args[j];
                    if (a is long al && b is int bi)
                    {
                        bi.Should().Be((int)al, $"{context}: instruction {i} arg {j}");
                    }
                    else if (a is int ai && b is long bl)
                    {
                        bl.Should().Be(ai, $"{context}: instruction {i} arg {j}");
                    }
                    else if (a is int ai32 && b is uint bu && (ai32 == -1 && bu == 0xFFFFFFFF || (uint)ai32 == bu))
                    {
                    }
                    else if (a is long a64 && b is uint bu2 && (a64 == -1 && bu2 == 0xFFFFFFFF || (uint)a64 == bu2))
                    {
                    }
                    else
                    {
                        b.Should().Be(a, $"{context}: instruction {i} arg {j}");
                    }
                }
            }

            for (int i = 0; i < jumpExp.Count; i++)
            {
                var je = jumpExp[i];
                var ja = jumpAct[i];
                if (je.Jump != null)
                {
                    ja.Jump.Should().NotBeNull($"{context}: jump {i} target");
                    int te = expected.Instructions.IndexOf(je.Jump);
                    int ta = actual.Instructions.IndexOf(ja.Jump);
                    ta.Should().Be(te, $"{context}: jump {i} target index");
                }
            }

            var cExp = expected.Instructions.Where(i => i.IsConstant()).ToList();
            var cAct = actual.Instructions.Where(i => i.IsConstant()).ToList();
            cAct.Count.Should().Be(cExp.Count, $"{context}: constant instruction count");
            for (int i = 0; i < cExp.Count; i++)
            {
                cExp[i].Args.Count.Should().Be(cAct[i].Args.Count, $"{context}: constant {i} arg count");
                for (int j = 0; j < cExp[i].Args.Count; j++)
                {
                    var co = cExp[i].Args[j];
                    var ca = cAct[i].Args[j];
                    if (co is int coi && ca is uint cru && (uint)coi == cru)
                    {
                        continue;
                    }

                    if (co is long col && ca is uint cr2 && col >= 0 && col <= uint.MaxValue && (uint)col == cr2)
                    {
                        continue;
                    }

                    ca.Should().Be(co, $"{context}: constant {i} arg {j}");
                }
            }
        }
    }
}
