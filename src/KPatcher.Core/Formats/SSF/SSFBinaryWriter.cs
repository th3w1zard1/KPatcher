using System;
using System.IO;
using System.Text;

namespace KPatcher.Core.Formats.SSF
{

    /// <summary>
    /// Writes SSF (Sound Set File) binary data.
    /// </summary>
    public class SSFBinaryWriter
    {
        private readonly SSF _ssf;

        public SSFBinaryWriter(SSF ssf)
        {
            _ssf = ssf;
        }

        public byte[] Write()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.ASCII))
            {
                // Write header
                writer.Write(Encoding.ASCII.GetBytes("SSF "));
                writer.Write(Encoding.ASCII.GetBytes("V1.1"));
                writer.Write((uint)12); // Sounds offset

                // Write all 40 sound references in order
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_1));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_2));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_3));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_4));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_5));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BATTLE_CRY_6));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.SELECT_1));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.SELECT_2));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.SELECT_3));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.ATTACK_GRUNT_1));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.ATTACK_GRUNT_2));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.ATTACK_GRUNT_3));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.PAIN_GRUNT_1));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.PAIN_GRUNT_2));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.LOW_HEALTH));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.DEAD));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.CRITICAL_HIT));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.TARGET_IMMUNE));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.LAY_MINE));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.DISARM_MINE));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BEGIN_STEALTH));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BEGIN_SEARCH));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.BEGIN_UNLOCK));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNLOCK_FAILED));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNLOCK_SUCCESS));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.SEPARATED_FROM_PARTY));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.REJOINED_PARTY));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.POISONED));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_29));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_30));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_31));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_32));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_33));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_34));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_35));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_36));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_37));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_38));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_39));
                WriteInt32MaxNeg1(writer, _ssf.Get(SSFSound.UNKNOWN_40));

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Writes an int32, converting -1 to 0xFFFFFFFF.
        /// </summary>
        private static void WriteInt32MaxNeg1(BinaryWriter writer, int? value)
        {
            uint toWrite = value.HasValue && value.Value == -1 ? 0xFFFFFFFF : (uint)(value ?? -1);
            if (toWrite == uint.MaxValue && (!value.HasValue || value.Value != -1))
            {
                toWrite = 0xFFFFFFFF;
            }
            writer.Write(toWrite);
        }
    }
}

