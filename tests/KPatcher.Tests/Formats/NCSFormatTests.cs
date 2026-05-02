using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Tests.Common;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for NCS binary I/O operations.
    /// Reference bytes come from in-process NSS compilation (<see cref="CompiledNcsTestFixture"/>), not from disk fixtures.
    /// </summary>
    public class NCSFormatTests
    {
        [Fact]
        public void TestBinaryIO()
        {
            byte[] originalData = CompiledNcsTestFixture.ReferenceK1NcsBytes();
            originalData.Length.Should().BeGreaterThan(13, "NCS header present");

            NCS ncs;
            using (var reader = new NCSBinaryReader(originalData))
            {
                ncs = reader.Load();
            }

            ncs.Instructions.Should().NotBeNull();
            ncs.Instructions.Count.Should().BeGreaterThan(0);

            string tempPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.ncs");
            try
            {
                NCSAuto.WriteNcs(ncs, tempPath);

                byte[] data = NCSAuto.BytesNcs(ncs);

                NCS ncs2 = NCSAuto.ReadNcs(data);
                ncs2.Instructions.Count.Should().Be(ncs.Instructions.Count);

                data.Should().Equal(originalData, "NCS binary I/O should produce byte-identical output");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
