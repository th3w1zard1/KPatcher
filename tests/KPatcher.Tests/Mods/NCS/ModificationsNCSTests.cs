using System.Collections.Generic;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Mods.NCS;
using Xunit;

namespace KPatcher.Core.Tests.Mods.NCS
{
    public sealed class ModificationsNCSTests
    {
        [Fact]
        public void PatchResource_UInt8Modifier_WritesByteAtOffset()
        {
            byte[] source = new byte[] { 0, 0, 0, 0, 0 };
            var memory = new PatcherMemory();
            var logger = new PatchLogger();
            var config = new ModificationsNCS("test.ncs", modifiers: new List<ModifyNCS>
            {
                new ModifyNCS(NCSTokenType.UINT8, 2, 0xAB)
            });

            byte[] patched = (byte[])config.PatchResource(source, memory, logger, Game.K1);

            patched[2].Should().Be(0xAB);
            patched[0].Should().Be(0);
            patched[4].Should().Be(0);
        }
    }
}
