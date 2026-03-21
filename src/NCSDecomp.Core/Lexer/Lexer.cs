// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;
using System.Text;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Lexer
{
    /// <summary>
    /// SableCC-generated lexer for NCS bytecode tokenization. Loads state tables from lexer.dat.
    /// </summary>
    public class Lexer
    {
        private static int[][][][] _gotoTable;
        private static int[][] _accept;
        private static readonly object _tableLock = new object();

        private Token _token;
        private readonly State _state = State.Initial;
        private readonly PushbackReader _in;
        private int _line;
        private int _pos;
        private bool _cr;
        private bool _eof;
        private readonly StringBuilder _text = new StringBuilder();

        public Lexer(PushbackReader reader)
        {
            _in = reader ?? throw new ArgumentNullException(nameof(reader));
            EnsureTablesLoaded();
        }

        public Lexer(TextReader reader) : this(new PushbackReader(reader)) { }

        private static void EnsureTablesLoaded()
        {
            if (_gotoTable != null)
            {
                return;
            }

            lock (_tableLock)
            {
                if (_gotoTable != null)
                {
                    return;
                }

                using (Stream s = ResourceLoader.OpenLexerDat())
                using (var br = new BinaryReader(s))
                {
                    int length = ReadInt32BE(br);
                    _gotoTable = new int[length][][][];
                    for (int i = 0; i < length; i++)
                    {
                        int len2 = ReadInt32BE(br);
                        _gotoTable[i] = new int[len2][][];
                        for (int j = 0; j < len2; j++)
                        {
                            int len3 = ReadInt32BE(br);
                            _gotoTable[i][j] = new int[len3][];
                            for (int k = 0; k < len3; k++)
                            {
                                _gotoTable[i][j][k] = new int[3];
                                for (int l = 0; l < 3; l++)
                                {
                                    _gotoTable[i][j][k][l] = ReadInt32BE(br);
                                }
                            }
                        }
                    }
                    length = ReadInt32BE(br);
                    _accept = new int[length][];
                    for (int i = 0; i < length; i++)
                    {
                        int len2 = ReadInt32BE(br);
                        _accept[i] = new int[len2];
                        for (int j = 0; j < len2; j++)
                        {
                            _accept[i][j] = ReadInt32BE(br);
                        }
                    }
                }
            }
        }

        private static int ReadInt32BE(BinaryReader br)
        {
            byte[] b = br.ReadBytes(4);
            if (b.Length < 4)
            {
                throw new EndOfStreamException();
            }

            return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        }

        protected virtual void Filter() { }

        public Token Peek()
        {
            while (_token == null)
            {
                _token = GetToken();
                Filter();
            }
            return _token;
        }

        public Token Next()
        {
            while (_token == null)
            {
                _token = GetToken();
                Filter();
            }
            Token result = _token;
            _token = null;
            return result;
        }

        protected virtual Token GetToken()
        {
            int dfa_state = 0;
            int start_pos = _pos;
            int start_line = _line;
            int accept_state = -1;
            int accept_token = -1;
            int accept_length = -1;
            int accept_pos = -1;
            int accept_line = -1;
            int[][][] gotoTable = _gotoTable[_state.Id()];
            int[] accept = _accept[_state.Id()];
            _text.Length = 0;

            while (true)
            {
                int c = GetChar();
                if (c == -1)
                {
                    dfa_state = -1;
                }
                else
                {
                    switch (c)
                    {
                        case 10:
                            if (_cr)
                            {
                                _cr = false;
                            }
                            else { _line++; _pos = 0; }
                            break;
                        case 13:
                            _line++; _pos = 0; _cr = true;
                            break;
                        default:
                            _pos++; _cr = false;
                            break;
                    }
                    _text.Append((char)c);

                    int oldState;
                    do
                    {
                        oldState = dfa_state < -1 ? -2 - dfa_state : dfa_state;
                        dfa_state = -1;
                        int[][] tmp1 = gotoTable[oldState];
                        int low = 0;
                        int high = tmp1.Length - 1;
                        while (low <= high)
                        {
                            int middle = (low + high) / 2;
                            int[] tmp2 = tmp1[middle];
                            if (c < tmp2[0])
                            {
                                high = middle - 1;
                            }
                            else if (c <= tmp2[1])
                            {
                                dfa_state = tmp2[2];
                                break;
                            }
                            else
                            {
                                low = middle + 1;
                            }
                        }
                    } while (dfa_state < -1);
                }

                if (dfa_state >= 0)
                {
                    if (accept[dfa_state] != -1)
                    {
                        accept_state = dfa_state;
                        accept_token = accept[dfa_state];
                        accept_length = _text.Length;
                        accept_pos = _pos;
                        accept_line = _line;
                    }
                }
                else
                {
                    if (accept_state == -1)
                    {
                        if (_text.Length > 0)
                        {
                            throw new LexerException("[" + (start_line + 1) + "," + (start_pos + 1) + "] Unknown token: " + _text);
                        }

                        return new EOF(start_line + 1, start_pos + 1);
                    }

                    Token token = CreateToken(accept_token, start_line + 1, start_pos + 1, accept_length);
                    PushBack(accept_length);
                    _pos = accept_pos;
                    _line = accept_line;
                    return token;
                }
            }
        }

        private Token CreateToken(int acceptToken, int line, int pos, int acceptLength)
        {
            switch (acceptToken)
            {
                case 0: return new TLPar(line, pos);
                case 1: return new TRPar(line, pos);
                case 2: return new TSemi(line, pos);
                case 3: return new TDot(line, pos);
                case 4: return new TCpdownsp(line, pos);
                case 5: return new TRsadd(line, pos);
                case 6: return new TCptopsp(line, pos);
                case 7: return new TConst(line, pos);
                case 8: return new TAction(line, pos);
                case 9: return new TLogandii(line, pos);
                case 10: return new TLogorii(line, pos);
                case 11: return new TIncorii(line, pos);
                case 12: return new TExcorii(line, pos);
                case 13: return new TBoolandii(line, pos);
                case 14: return new TEqual(line, pos);
                case 15: return new TNequal(line, pos);
                case 16: return new TGeq(line, pos);
                case 17: return new TGt(line, pos);
                case 18: return new TLt(line, pos);
                case 19: return new TLeq(line, pos);
                case 20: return new TShleft(line, pos);
                case 21: return new TShright(line, pos);
                case 22: return new TUnright(line, pos);
                case 23: return new TAdd(line, pos);
                case 24: return new TSub(line, pos);
                case 25: return new TMul(line, pos);
                case 26: return new TDiv(line, pos);
                case 27: return new TMod(line, pos);
                case 28: return new TNeg(line, pos);
                case 29: return new TComp(line, pos);
                case 30: return new TMovsp(line, pos);
                case 31: return new TJmp(line, pos);
                case 32: return new TJsr(line, pos);
                case 33: return new TJz(line, pos);
                case 34: return new TRetn(line, pos);
                case 35: return new TDestruct(line, pos);
                case 36: return new TNot(line, pos);
                case 37: return new TDecisp(line, pos);
                case 38: return new TIncisp(line, pos);
                case 39: return new TJnz(line, pos);
                case 40: return new TCpdownbp(line, pos);
                case 41: return new TCptopbp(line, pos);
                case 42: return new TDecibp(line, pos);
                case 43: return new TIncibp(line, pos);
                case 44: return new TSavebp(line, pos);
                case 45: return new TRestorebp(line, pos);
                case 46: return new TStorestate(line, pos);
                case 47: return new TNop(line, pos);
                case 48: return new TT(line, pos);
                case 49: return new TStringLiteral(GetText(acceptLength), line, pos);
                case 50: return new TBlank(GetText(acceptLength), line, pos);
                case 51: return new TIntegerConstant(GetText(acceptLength), line, pos);
                case 52: return new TFloatConstant(GetText(acceptLength), line, pos);
                default: throw new LexerException("Unknown accept token: " + acceptToken);
            }
        }

        private int GetChar()
        {
            if (_eof)
            {
                return -1;
            }

            int result = _in.Read();
            if (result == -1)
            {
                _eof = true;
            }

            return result;
        }

        /// <summary>Matches DeNCS <c>Lexer.pushBack</c>: put back all characters after the accepted prefix.</summary>
        private void PushBack(int acceptLength)
        {
            string t = _text.ToString();
            for (int i = t.Length - 1; i >= acceptLength; i--)
            {
                _eof = false;
                _in.Unread(t[i]);
            }
        }

        private string GetText(int acceptLength)
        {
            var s = new StringBuilder(acceptLength);
            string t = _text.ToString();
            for (int i = 0; i < acceptLength && i < t.Length; i++)
            {
                s.Append(t[i]);
            }

            return s.ToString();
        }

        public sealed class State
        {
            public static readonly State Initial = new State(0);
            private readonly int _id;
            private State(int id) { _id = id; }
            public int Id() { return _id; }
        }
    }
}
