// Copyright (c) 2021-2025 DeNCS contributors (DeNCS)
// Port to C# for KPatcher. Original Java: com.kotor.resource.formats.ncs.Decoder
// Licensed under the MIT License (see NOTICE and licenses/DeNCS-MIT.txt).

using System;
using System.IO;
using System.Text;

namespace KPatcher.Core.Formats.NCS.Decompiler
{
    /// <summary>
    /// Decodes compiled NCS bytecode into a tokenized command stream.
    /// Port of DeNCS Decoder.java: reads binary instruction set, validates header,
    /// emits flat string representation consumed by the parser.
    /// Supports both 8-byte header (NCS V1.0, DeNCS style) and 13-byte header (KPatcher/reone: + magic + size).
    /// </summary>
    public sealed class Decoder
    {
        private readonly byte[] _data;
        private int _pos;
        private readonly bool _hasQualifier; // true = 13-byte header + 2-byte opcode/qualifier per instruction

        /// <summary>
        /// Decode from raw NCS bytes. Detects header format (8- or 13-byte) and whether each instruction
        /// has a qualifier byte (KPatcher format).
        /// </summary>
        public Decoder(byte[] ncsBytes)
        {
            _data = ncsBytes ?? throw new ArgumentNullException(nameof(ncsBytes));
            _pos = 0;
            _hasQualifier = DetectFormat(ncsBytes);
        }

        private static bool DetectFormat(byte[] data)
        {
            if (data.Length < 8)
                throw new InvalidDataException("NCS data too short for header.");
            if (data[0] != 'N' || data[1] != 'C' || data[2] != 'S' || data[3] != ' ' ||
                data[4] != 'V' || data[5] != '1' || data[6] != '.' || data[7] != '0')
                throw new InvalidDataException("The data file is not an NCS V1.0 file.");
            if (data.Length >= 13 && data[8] == 0x42)
            {
                // KPatcher/reone: magic 0x42 + 4-byte size, then instructions with opcode+qualifier
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads and decodes the entire NCS file. Call after construction; header is skipped in Decode().
        /// </summary>
        /// <param name="actions">Optional actions table for ACTION opcode lookup; can be null for scripts without engine calls.</param>
        public string Decode(IActionsData actions = null)
        {
            ReadHeader();
            return ReadCommands(actions);
        }

        private void ReadHeader()
        {
            if (_hasQualifier)
            {
                if (_data.Length < 13)
                    throw new InvalidDataException("NCS file too short.");
                _pos = 13;
            }
            else
            {
                _pos = 8;
            }
        }

        private string ReadCommands(IActionsData actions)
        {
            var sb = new StringBuilder();
            // Java DeNCS reads byte 8 as opcode 66 ("T") + 4-byte signed int, then continues at offset 13.
            // We detect the same 0x42 byte as KPatcher/reone extended header and skip to 13, but the SableCC
            // lexer/parser still require the leading "T <pos> <int>; " line — emit it from bytes 8–12.
            if (_hasQualifier && _data.Length >= 13 && _data[8] == 0x42)
            {
                int tArg = ReadBigEndianInt32(_data, 9);
                sb.Append("T 8 ").Append(tArg).Append("; ");
            }

            while (ReadCommand(sb, actions))
            {
            }
            return sb.ToString();
        }

        private static int ReadBigEndianInt32(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
                throw new InvalidDataException("Unexpected EOF reading 32-bit value.");
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        private bool ReadCommand(StringBuilder strbuffer, IActionsData actions)
        {
            int commandpos = _pos;
            int opcode;
            byte qualifier = 0;
            if (_hasQualifier)
            {
                if (_pos + 2 > _data.Length)
                    return false;
                opcode = _data[_pos++];
                qualifier = _data[_pos++];
            }
            else
            {
                if (_pos >= _data.Length)
                    return false;
                opcode = _data[_pos++];
            }

            strbuffer.Append(GetCommand((byte)opcode));
            strbuffer.Append(" ").Append(commandpos);

            try
            {
                switch (opcode)
                {
                    case 1:
                    case 3:
                    case 38:
                    case 39:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        AppendSignedInt(strbuffer);
                        AppendUnsignedShort(strbuffer);
                        break;
                    case 2:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 32:
                    case 34:
                    case 42:
                    case 43:
                    case 45:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        break;
                    case 4:
                        byte bx = _hasQualifier ? qualifier : ReadByte();
                        strbuffer.Append(" ").Append(bx);
                        switch (bx)
                        {
                            case 3:
                                AppendUnsignedInt(strbuffer);
                                break;
                            case 4:
                                AppendFloat(strbuffer);
                                break;
                            case 5:
                                AppendString(strbuffer);
                                break;
                            case 6:
                                AppendSignedInt(strbuffer);
                                break;
                            default:
                                throw new InvalidDataException("Unknown or unexpected constant type: " + bx);
                        }
                        break;
                    case 5:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        AppendUnsignedShort(strbuffer);
                        AppendByte(strbuffer);
                        break;
                    case 11:
                    case 12:
                        byte b = _hasQualifier ? qualifier : ReadByte();
                        strbuffer.Append(" ").Append(b);
                        if (b == 36)
                            AppendUnsignedShort(strbuffer);
                        break;
                    case 27:
                    case 29:
                    case 30:
                    case 31:
                    case 35:
                    case 36:
                    case 37:
                    case 40:
                    case 41:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        AppendSignedInt(strbuffer);
                        break;
                    case 28:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        break;
                    case 33:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        AppendUnsignedShort(strbuffer);
                        AppendUnsignedShort(strbuffer);
                        AppendUnsignedShort(strbuffer);
                        break;
                    case 44:
                        AppendByteOrQualifier(strbuffer, qualifier);
                        AppendSignedInt(strbuffer);
                        AppendSignedInt(strbuffer);
                        break;
                    case 66:
                        AppendSignedInt(strbuffer);
                        break;
                    default:
                        throw new InvalidDataException("Unknown command type: " + opcode);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error in .ncs file at pos " + _pos, ex);
            }

            strbuffer.Append("; ");
            return true;
        }

        private byte ReadByte()
        {
            if (_pos >= _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            return _data[_pos++];
        }

        private void AppendByte(StringBuilder sb)
        {
            sb.Append(" ").Append(ReadByte());
        }

        private void AppendByteOrQualifier(StringBuilder sb, byte qualifier)
        {
            if (_hasQualifier)
                sb.Append(" ").Append(qualifier);
            else
                sb.Append(" ").Append(ReadByte());
        }

        private void AppendUnsignedInt(StringBuilder sb)
        {
            if (_pos + 4 > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            uint v = (uint)((_data[_pos] << 24) | (_data[_pos + 1] << 16) | (_data[_pos + 2] << 8) | _data[_pos + 3]);
            _pos += 4;
            sb.Append(" ").Append(v);
        }

        private void AppendSignedInt(StringBuilder sb)
        {
            if (_pos + 4 > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            int v = (_data[_pos] << 24) | (_data[_pos + 1] << 16) | (_data[_pos + 2] << 8) | _data[_pos + 3];
            _pos += 4;
            sb.Append(" ").Append(v);
        }

        private void AppendUnsignedShort(StringBuilder sb)
        {
            if (_pos + 2 > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            int v = (_data[_pos] << 8) | _data[_pos + 1];
            _pos += 2;
            sb.Append(" ").Append(v);
        }

        private void AppendFloat(StringBuilder sb)
        {
            if (_pos + 4 > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            int bits = (_data[_pos] << 24) | (_data[_pos + 1] << 16) | (_data[_pos + 2] << 8) | _data[_pos + 3];
            _pos += 4;
            float f = BitConverter.Int32BitsToSingle(bits);
            string result = f.ToString("G15");
            if (result.IndexOf('.') < 0 && Math.Abs(f) < 1.0f && f != 0.0f)
                result = "0." + result;
            sb.Append(" ").Append(result);
        }

        private void AppendString(StringBuilder sb)
        {
            if (_pos + 2 > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            int size = (_data[_pos] << 8) | _data[_pos + 1];
            _pos += 2;
            if (_pos + size > _data.Length)
                throw new InvalidDataException("Unexpected EOF");
            string s = Encoding.ASCII.GetString(_data, _pos, size);
            _pos += size;
            sb.Append(" \"").Append(s).Append("\"");
        }

        private static string GetCommand(byte command)
        {
            switch (command)
            {
                case 1: return "CPDOWNSP";
                case 2: return "RSADD";
                case 3: return "CPTOPSP";
                case 4: return "CONST";
                case 5: return "ACTION";
                case 6: return "LOGANDII";
                case 7: return "LOGORII";
                case 8: return "INCORII";
                case 9: return "EXCORII";
                case 10: return "BOOLANDII";
                case 11: return "EQUAL";
                case 12: return "NEQUAL";
                case 13: return "GEQ";
                case 14: return "GT";
                case 15: return "LT";
                case 16: return "LEQ";
                case 17: return "SHLEFTII";
                case 18: return "SHRIGHTII";
                case 19: return "USHRIGHTII";
                case 20: return "ADD";
                case 21: return "SUB";
                case 22: return "MUL";
                case 23: return "DIV";
                case 24: return "MOD";
                case 25: return "NEG";
                case 26: return "COMP";
                case 27: return "MOVSP";
                case 28: return "STATEALL";
                case 29: return "JMP";
                case 30: return "JSR";
                case 31: return "JZ";
                case 32: return "RETN";
                case 33: return "DESTRUCT";
                case 34: return "NOT";
                case 35: return "DECISP";
                case 36: return "INCISP";
                case 37: return "JNZ";
                case 38: return "CPDOWNBP";
                case 39: return "CPTOPBP";
                case 40: return "DECIBP";
                case 41: return "INCIBP";
                case 42: return "SAVEBP";
                case 43: return "RESTOREBP";
                case 44: return "STORE_STATE";
                case 45: return "NOP";
                case 66: return "T";
                default:
                    throw new InvalidDataException("Unknown command code: " + command);
            }
        }
    }
}
