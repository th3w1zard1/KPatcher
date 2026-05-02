using System;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Tests.Common
{
    /// <summary>
    /// Canonical NCS bytes for format / lexer / smoke tests, produced by compiling NSS in-process (no on-disk .ncs).
    /// </summary>
    internal static class CompiledNcsTestFixture
    {
        private const string ReferenceNss = @"
void main()
{
    int value = 41;
    value = value + 1;
}
";

        private static readonly Lazy<byte[]> LazyK1Bytes = new Lazy<byte[]>(() =>
            NCSAuto.BytesNcs(NCSAuto.CompileNss(ReferenceNss, Game.K1, null, null, null)));

        internal static byte[] ReferenceK1NcsBytes() => LazyK1Bytes.Value;
    }
}
