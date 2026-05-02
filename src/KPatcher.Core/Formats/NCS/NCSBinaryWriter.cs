using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KPatcher.Core.Formats.NCS
{

    /// <summary>
    /// Writes NCS (NWScript Compiled Script) files.
    ///
    /// References:
    ///     vendor/reone/src/libs/script/format/ncsreader.cpp:28-40 (NCS header writing)
    ///     vendor/xoreos-tools/src/nwscript/compiler.cpp (NCS compilation)
    ///     vendor/xoreos-docs/specs/torlack/ncs.html (NCS format specification)
    /// </summary>
    public class NCSBinaryWriter
    {
        private const byte NCS_HEADER_MAGIC_BYTE = 0x42;
        private const int NCS_HEADER_SIZE = 13;

        private readonly NCS _ncs;

        public NCSBinaryWriter(NCS ncs)
        {
            _ncs = ncs ?? throw new ArgumentNullException(nameof(ncs));
        }

        public byte[] Write()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
            {
                IReadOnlyList<NCSInstruction> instructions = _ncs.Instructions;
                int n = instructions.Count;
                int[] offsets = new int[n];
                int offset = NCS_HEADER_SIZE;
                for (int i = 0; i < n; i++)
                {
                    offsets[i] = offset;
                    offset += DetermineSize(instructions[i]);
                }

                writer.Write(Encoding.ASCII.GetBytes("NCS "));
                writer.Write(Encoding.ASCII.GetBytes("V1.0"));
                writer.Write(NCS_HEADER_MAGIC_BYTE);
                WriteBigEndianUInt32(writer, (uint)offset);

                for (int i = 0; i < n; i++)
                {
                    WriteInstruction(writer, instructions[i], i, offsets, instructions);
                }

                return ms.ToArray();
            }
        }

        private static int IndexOfJumpTarget(IReadOnlyList<NCSInstruction> instructions, NCSInstruction jump)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (ReferenceEquals(instructions[i], jump))
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Jump target instruction is not present in the instruction list.");
        }

        private static void WriteInstruction(
            BinaryWriter writer,
            NCSInstruction instruction,
            int instructionIndex,
            int[] offsets,
            IReadOnlyList<NCSInstruction> instructions)
        {
            (NCSByteCode byteCode, byte qualifier) = instruction.InsType.GetValue();
            writer.Write((byte)byteCode);
            writer.Write(qualifier);

            // Handle instruction-specific arguments
            if (instruction.InsType == NCSInstructionType.DECxSP ||
                instruction.InsType == NCSInstructionType.INCxSP ||
                instruction.InsType == NCSInstructionType.DECxBP ||
                instruction.InsType == NCSInstructionType.INCxBP)
            {
                WriteBigEndianInt32(writer, ToSigned32Bit(Convert.ToInt64(instruction.Args[0])));
            }
            else if (instruction.InsType == NCSInstructionType.CPDOWNSP ||
                     instruction.InsType == NCSInstructionType.CPTOPSP ||
                     instruction.InsType == NCSInstructionType.CPDOWNBP ||
                     instruction.InsType == NCSInstructionType.CPTOPBP)
            {
                WriteBigEndianInt32(writer, Convert.ToInt32(instruction.Args[0]));
                ushort sizeValue = instruction.Args.Count > 1 ? Convert.ToUInt16(instruction.Args[1]) : (ushort)4;
                WriteBigEndianUInt16(writer, sizeValue);
            }
            else if (instruction.InsType == NCSInstructionType.CONSTF)
            {
                WriteBigEndianSingle(writer, Convert.ToSingle(instruction.Args[0]));
            }
            else if (instruction.InsType == NCSInstructionType.CONSTO)
            {
                WriteBigEndianInt32(writer, Convert.ToInt32(instruction.Args[0]));
            }
            else if (instruction.InsType == NCSInstructionType.CONSTI)
            {
                WriteBigEndianInt32(writer, ToSigned32Bit(Convert.ToInt64(instruction.Args[0])));
            }
            else if (instruction.InsType == NCSInstructionType.CONSTS)
            {
                string str = instruction.Args[0].ToString() ?? "";
                WriteBigEndianUInt16(writer, (ushort)str.Length);
                writer.Write(Encoding.ASCII.GetBytes(str));
            }
            else if (instruction.InsType == NCSInstructionType.ACTION)
            {
                WriteBigEndianUInt16(writer, Convert.ToUInt16(instruction.Args[0]));
                writer.Write(Convert.ToByte(instruction.Args[1]));
            }
            else if (instruction.InsType == NCSInstructionType.MOVSP)
            {
                WriteBigEndianInt32(writer, ToSigned32Bit(Convert.ToInt64(instruction.Args[0])));
            }
            else if (instruction.InsType == NCSInstructionType.JMP ||
                     instruction.InsType == NCSInstructionType.JSR ||
                     instruction.InsType == NCSInstructionType.JZ ||
                     instruction.InsType == NCSInstructionType.JNZ)
            {
                if (instruction.Jump == null)
                {
                    throw new InvalidOperationException($"{instruction} has a NoneType jump.");
                }
                int currentOffset = offsets[instructionIndex];
                int jumpIndex = IndexOfJumpTarget(instructions, instruction.Jump);
                int jumpFileOffset = offsets[jumpIndex];
                int relative = jumpFileOffset - currentOffset;
                WriteBigEndianInt32(writer, ToSigned32Bit(relative));
            }
            else if (instruction.InsType == NCSInstructionType.DESTRUCT)
            {
                WriteBigEndianUInt16(writer, Convert.ToUInt16(instruction.Args[0]));
                WriteBigEndianInt16(writer, ToSigned16Bit(Convert.ToInt32(instruction.Args[1])));
                WriteBigEndianUInt16(writer, Convert.ToUInt16(instruction.Args[2]));
            }
            else if (instruction.InsType == NCSInstructionType.STORE_STATE)
            {
                WriteBigEndianUInt32(writer, Convert.ToUInt32(instruction.Args[0]));
                WriteBigEndianUInt32(writer, Convert.ToUInt32(instruction.Args[1]));
            }
            else if (instruction.InsType == NCSInstructionType.EQUALTT ||
                     instruction.InsType == NCSInstructionType.NEQUALTT)
            {
                WriteBigEndianUInt16(writer, Convert.ToUInt16(instruction.Args[0]));
            }
            else
            {
                // All other instructions have no arguments (just opcode + qualifier)
                // This includes: RSADD variants, logical/arithmetic ops, RETN, SAVEBP, RESTOREBP, etc.
            }
        }

        private static int ToSigned32Bit(long n)
        {
            if (n >= 0x80000000L)
            {
                n -= 0x100000000L;
            }
            return (int)n;
        }

        private static short ToSigned16Bit(int n)
        {
            if (n >= 0x8000)
            {
                n -= 0x10000;
            }
            return (short)n;
        }

        private static int DetermineSize(NCSInstruction instruction)
        {
            int size = 2;

            switch (instruction.InsType)
            {
                case NCSInstructionType.CPDOWNSP:
                case NCSInstructionType.CPTOPSP:
                case NCSInstructionType.CPDOWNBP:
                case NCSInstructionType.CPTOPBP:
                    size += 6;
                    break;

                case NCSInstructionType.CONSTI:
                    size += 4;
                    break;

                case NCSInstructionType.CONSTF:
                    size += 4;
                    break;

                case NCSInstructionType.CONSTS:
                    {
                        string str = instruction.Args[0].ToString() ?? "";
                        size += 2 + str.Length;
                    }
                    break;

                case NCSInstructionType.CONSTO:
                    size += 4;
                    break;

                case NCSInstructionType.ACTION:
                    size += 3;
                    break;

                case NCSInstructionType.MOVSP:
                    size += 4;
                    break;

                case NCSInstructionType.JMP:
                case NCSInstructionType.JSR:
                case NCSInstructionType.JZ:
                case NCSInstructionType.JNZ:
                    size += 4;
                    break;

                case NCSInstructionType.DESTRUCT:
                    size += 6;
                    break;

                case NCSInstructionType.DECxSP:
                case NCSInstructionType.INCxSP:
                case NCSInstructionType.DECxBP:
                case NCSInstructionType.INCxBP:
                    size += 4;
                    break;

                case NCSInstructionType.STORE_STATE:
                    size += 8;
                    break;

                case NCSInstructionType.EQUALTT:
                case NCSInstructionType.NEQUALTT:
                    size += 2;
                    break;
            }

            return size;
        }

        private static void WriteBigEndianInt16(BinaryWriter writer, short value)
        {
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteBigEndianUInt16(BinaryWriter writer, ushort value)
        {
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteBigEndianInt32(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteBigEndianUInt32(BinaryWriter writer, uint value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteBigEndianSingle(BinaryWriter writer, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            writer.Write(bytes);
        }
    }
}

