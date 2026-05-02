using System;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Formats.SSF;
using KPatcher.Core.Resources;
using KPatcher.Core.Tests.Common;
using Xunit;
using static global::KPatcher.Core.Formats.SSF.SSFAuto;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// Tests for SSF binary I/O operations.
    /// 1:1 port of tests/resource/formats/test_ssf.py
    /// </summary>
    public class SSFFormatTests
    {
        private const string DoesNotExistFile = "./thisfiledoesnotexist";

        [Fact]
        public void TestBinaryIO()
        {
            SSF ssf = BinaryFormatFixtures.BuildCanonicalSsf();
            ValidateIO(ssf);

            var writer = new SSFBinaryWriter(ssf);
            byte[] data = writer.Write();

            var newReader = new SSFBinaryReader(data);
            SSF newSsf = newReader.Load();
            ValidateIO(newSsf);
        }

        [Fact]
        public void WriteRoundTrip_BytesMatchSourceFile()
        {
            SSF ssf = BinaryFormatFixtures.BuildCanonicalSsf();
            byte[] onDisk = ssf.ToBytes();
            var reader = new SSFBinaryReader(onDisk);
            SSF loaded = reader.Load();
            var writer = new SSFBinaryWriter(loaded);
            byte[] serialized = writer.Write();
            serialized.Should().Equal(onDisk);
        }

        [Fact]
        public void TestReadRaises()
        {
            Action act1 = () => new SSFBinaryReader(".").Load();
            act1.Should().Throw<UnauthorizedAccessException>();

            Action act2 = () => new SSFBinaryReader(DoesNotExistFile).Load();
            act2.Should().Throw<FileNotFoundException>();

            byte[] corrupt = FormatCorruptBinarySamples.CorruptSsf;
            Action act3 = () => new SSFBinaryReader(corrupt).Load();
            act3.Should().Throw<Exception>();
        }

        private static void ValidateIO(SSF ssf)
        {
            ssf.Get(SSFSound.BATTLE_CRY_1).Should().Be(123075);
            ssf.Get(SSFSound.BATTLE_CRY_2).Should().Be(123074);
            ssf.Get(SSFSound.BATTLE_CRY_3).Should().Be(123073);
            ssf.Get(SSFSound.BATTLE_CRY_4).Should().Be(123072);
            ssf.Get(SSFSound.BATTLE_CRY_5).Should().Be(123071);
            ssf.Get(SSFSound.BATTLE_CRY_6).Should().Be(123070);
            ssf.Get(SSFSound.SELECT_1).Should().Be(123069);
            ssf.Get(SSFSound.SELECT_2).Should().Be(123068);
            ssf.Get(SSFSound.SELECT_3).Should().Be(123067);
            ssf.Get(SSFSound.ATTACK_GRUNT_1).Should().Be(123066);
            ssf.Get(SSFSound.ATTACK_GRUNT_2).Should().Be(123065);
            ssf.Get(SSFSound.ATTACK_GRUNT_3).Should().Be(123064);
            ssf.Get(SSFSound.PAIN_GRUNT_1).Should().Be(123063);
            ssf.Get(SSFSound.PAIN_GRUNT_2).Should().Be(123062);
            ssf.Get(SSFSound.LOW_HEALTH).Should().Be(123061);
            ssf.Get(SSFSound.DEAD).Should().Be(123060);
            ssf.Get(SSFSound.CRITICAL_HIT).Should().Be(123059);
            ssf.Get(SSFSound.TARGET_IMMUNE).Should().Be(123058);
            ssf.Get(SSFSound.LAY_MINE).Should().Be(123057);
            ssf.Get(SSFSound.DISARM_MINE).Should().Be(123056);
            ssf.Get(SSFSound.BEGIN_STEALTH).Should().Be(123055);
            ssf.Get(SSFSound.BEGIN_SEARCH).Should().Be(123054);
            ssf.Get(SSFSound.BEGIN_UNLOCK).Should().Be(123053);
            ssf.Get(SSFSound.UNLOCK_FAILED).Should().Be(123052);
            ssf.Get(SSFSound.UNLOCK_SUCCESS).Should().Be(123051);
            ssf.Get(SSFSound.SEPARATED_FROM_PARTY).Should().Be(123050);
            ssf.Get(SSFSound.REJOINED_PARTY).Should().Be(123049);
            ssf.Get(SSFSound.POISONED).Should().Be(123048);
        }

        [Fact]
        public void TestWriteRaises()
        {
            var ssf = new SSF();

            Action act1 = () => WriteSsf(ssf, ".", ResourceType.SSF);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                act1.Should().Throw<UnauthorizedAccessException>();
            }
            else
            {
                act1.Should().Throw<IOException>();
            }

            Action act2 = () => WriteSsf(ssf, ".", ResourceType.INVALID);
            act2.Should().Throw<ArgumentException>().WithMessage("*Unsupported format*");
        }
    }
}
