// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;
using LexerImpl = global::NCSDecomp.Core.Lexer.Lexer;

namespace NCSDecomp.Core.Parser
{
    /// <summary>
    /// SableCC-generated parser that builds the AST from lexer tokens.
    /// Factory methods named New0/New1/... mirror Java new0/new1/...; do not rename.
    /// </summary>
    public class Parser
    {
        private const int Shift = 0;
        private const int Reduce = 1;
        private const int Accept = 2;
        private const int Error = 3;

        private static int[][][] _actionTable;
        private static int[][][] _gotoTable;
        private static string[] _errorMessages;
        private static int[] _errors;
        private static readonly object _tableLock = new object();

        public readonly AnalysisAdapter IgnoredTokens = new AnalysisAdapter();

        protected AstNode node;

        private readonly LexerImpl _lexer;
        private readonly List<State> _stack = new List<State>();
        /// <summary>Java ListIterator &quot;next&quot; index into <see cref="_stack"/>.</summary>
        private int _nextIndex;

        private int _lastShift;
        private int _lastPos;
        private int _lastLine;
        private Token _lastToken;
        private readonly TokenIndex _converter = new TokenIndex();
        private readonly int[] _action = new int[2];

        public Parser(LexerImpl lexer)
        {
            _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
            EnsureTablesLoaded();
        }

        private static void EnsureTablesLoaded()
        {
            if (_actionTable != null)
            {
                return;
            }

            lock (_tableLock)
            {
                if (_actionTable != null)
                {
                    return;
                }

                try
                {
                    using (Stream s = ResourceLoader.OpenParserDat())
                    using (var br = new BinaryReader(s))
                    {
                        int length = ReadInt32BE(br);
                        _actionTable = new int[length][][];
                        for (int i = 0; i < _actionTable.Length; i++)
                        {
                            length = ReadInt32BE(br);
                            _actionTable[i] = new int[length][];
                            for (int j = 0; j < _actionTable[i].Length; j++)
                            {
                                _actionTable[i][j] = new int[3];
                                for (int k = 0; k < 3; k++)
                                {
                                    _actionTable[i][j][k] = ReadInt32BE(br);
                                }
                            }
                        }

                        length = ReadInt32BE(br);
                        _gotoTable = new int[length][][];
                        for (int i = 0; i < _gotoTable.Length; i++)
                        {
                            length = ReadInt32BE(br);
                            _gotoTable[i] = new int[length][];
                            for (int j = 0; j < _gotoTable[i].Length; j++)
                            {
                                _gotoTable[i][j] = new int[2];
                                for (int k = 0; k < 2; k++)
                                {
                                    _gotoTable[i][j][k] = ReadInt32BE(br);
                                }
                            }
                        }

                        length = ReadInt32BE(br);
                        _errorMessages = new string[length];
                        for (int i = 0; i < _errorMessages.Length; i++)
                        {
                            length = ReadInt32BE(br);
                            var buffer = new StringBuilder(length);
                            for (int j = 0; j < length; j++)
                            {
                                buffer.Append(ReadCharBE(br));
                            }

                            _errorMessages[i] = buffer.ToString();
                        }

                        length = ReadInt32BE(br);
                        _errors = new int[length];
                        for (int i = 0; i < _errors.Length; i++)
                        {
                            _errors[i] = ReadInt32BE(br);
                        }
                    }
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The file \"parser.dat\" is either missing or corrupted.");
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

        /// <summary>Matches Java <c>DataInputStream.readChar()</c> (UTF-16 code unit, big-endian).</summary>
        private static char ReadCharBE(BinaryReader br)
        {
            int hi = br.ReadByte();
            int lo = br.ReadByte();
            return (char)((hi << 8) | lo);
        }

        protected virtual void Filter()
        {
        }

        private int GoTo(int index)
        {
            int state = StateTop();
            int low = 1;
            int high = _gotoTable[index].Length - 1;
            int value = _gotoTable[index][0][1];

            while (low <= high)
            {
                int middle = (low + high) / 2;
                if (state < _gotoTable[index][middle][0])
                {
                    high = middle - 1;
                }
                else
                {
                    if (state <= _gotoTable[index][middle][0])
                    {
                        value = _gotoTable[index][middle][1];
                        break;
                    }

                    low = middle + 1;
                }
            }

            return value;
        }

        private void Push(int state, AstNode astNode, bool runFilter)
        {
            node = astNode;
            if (runFilter)
            {
                Filter();
            }

            if (_nextIndex >= _stack.Count)
            {
                _stack.Add(new State(state, node));
            }
            else
            {
                State s = _stack[_nextIndex];
                s.state = state;
                s.node = node;
            }

            _nextIndex++;
        }

        /// <summary>Peek current state number (top of stack) without popping.</summary>
        private int StateTop()
        {
            if (_nextIndex <= 0)
            {
                throw new InvalidOperationException("Parser stack underflow.");
            }

            State s = _stack[_nextIndex - 1];
            return s.state;
        }

        private AstNode Pop()
        {
            if (_nextIndex <= 0)
            {
                throw new InvalidOperationException("Parser stack underflow.");
            }

            _nextIndex--;
            return _stack[_nextIndex].node;
        }

        private int Index(Switchable token)
        {
            _converter.Index = -1;
            token.Apply(_converter);
            return _converter.Index;
        }

        public Start Parse()
        {
            Push(0, null, false);
            TypedLinkedList<Token> ign = null;

            while (true)
            {
                while (Index(_lexer.Peek()) == -1)
                {
                    if (ign == null)
                    {
                        ign = new TypedLinkedList<Token>(NoCast<Token>.Instance);
                    }

                    ign.AddLast(_lexer.Next());
                }

                if (ign != null)
                {
                    IgnoredTokens.SetIn(_lexer.Peek(), ign);
                    ign = null;
                }

                _lastPos = _lexer.Peek().GetPos();
                _lastLine = _lexer.Peek().GetLine();
                _lastToken = _lexer.Peek();
                int tokenIndex = Index(_lexer.Peek());
                int st = StateTop();
                _action[0] = _actionTable[st][0][1];
                _action[1] = _actionTable[st][0][2];
                int low = 1;
                int high = _actionTable[st].Length - 1;

                while (low <= high)
                {
                    int middle = (low + high) / 2;
                    if (tokenIndex < _actionTable[st][middle][0])
                    {
                        high = middle - 1;
                    }
                    else
                    {
                        if (tokenIndex <= _actionTable[st][middle][0])
                        {
                            _action[0] = _actionTable[st][middle][1];
                            _action[1] = _actionTable[st][middle][2];
                            break;
                        }

                        low = middle + 1;
                    }
                }

                if (_action[0] == Shift)
                {
                    Push(_action[1], _lexer.Next(), true);
                    _lastShift = _action[1];
                }
                else if (_action[0] == Reduce)
                {
                    // Reduce (NewN) must run before GoTo: pops RHS; GoTo reads the new stack top. Java's
                    // push(goTo(), newN()) evaluates newN first; C# Push(GoTo(), NewN()) would not — use locals.
                    switch (_action[1])
                    {
                        case 0:
                            {
                                AstNode r = New0();
                                Push(GoTo(0), r, true);
                                continue;
                            }
                        case 1:
                            {
                                AstNode r = New1();
                                Push(GoTo(31), r, false);
                                continue;
                            }
                        case 2:
                            {
                                AstNode r = New2();
                                Push(GoTo(31), r, false);
                                continue;
                            }
                        case 3:
                            {
                                AstNode r = New3();
                                Push(GoTo(0), r, true);
                                continue;
                            }
                        case 4:
                            {
                                AstNode r = New4();
                                Push(GoTo(1), r, true);
                                continue;
                            }
                        case 5:
                            {
                                AstNode r = New5();
                                Push(GoTo(1), r, true);
                                continue;
                            }
                        case 6:
                            {
                                AstNode r = New6();
                                Push(GoTo(2), r, true);
                                continue;
                            }
                        case 7:
                            {
                                AstNode r = New7();
                                Push(GoTo(32), r, false);
                                continue;
                            }
                        case 8:
                            {
                                AstNode r = New8();
                                Push(GoTo(32), r, false);
                                continue;
                            }
                        case 9:
                            {
                                AstNode r = New9();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 10:
                            {
                                AstNode r = New10();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 11:
                            {
                                AstNode r = New11();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 12:
                            {
                                AstNode r = New12();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 13:
                            {
                                AstNode r = New13();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 14:
                            {
                                AstNode r = New14();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 15:
                            {
                                AstNode r = New15();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 16:
                            {
                                AstNode r = New16();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 17:
                            {
                                AstNode r = New17();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 18:
                            {
                                AstNode r = New18();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 19:
                            {
                                AstNode r = New19();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 20:
                            {
                                AstNode r = New20();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 21:
                            {
                                AstNode r = New21();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 22:
                            {
                                AstNode r = New22();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 23:
                            {
                                AstNode r = New23();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 24:
                            {
                                AstNode r = New24();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 25:
                            {
                                AstNode r = New25();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 26:
                            {
                                AstNode r = New26();
                                Push(GoTo(3), r, true);
                                continue;
                            }
                        case 27:
                            {
                                AstNode r = New27();
                                Push(GoTo(4), r, true);
                                continue;
                            }
                        case 28:
                            {
                                AstNode r = New28();
                                Push(GoTo(4), r, true);
                                continue;
                            }
                        case 29:
                            {
                                AstNode r = New29();
                                Push(GoTo(4), r, true);
                                continue;
                            }
                        case 30:
                            {
                                AstNode r = New30();
                                Push(GoTo(4), r, true);
                                continue;
                            }
                        case 31:
                            {
                                AstNode r = New31();
                                Push(GoTo(4), r, true);
                                continue;
                            }
                        case 32:
                            {
                                AstNode r = New32();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 33:
                            {
                                AstNode r = New33();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 34:
                            {
                                AstNode r = New34();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 35:
                            {
                                AstNode r = New35();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 36:
                            {
                                AstNode r = New36();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 37:
                            {
                                AstNode r = New37();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 38:
                            {
                                AstNode r = New38();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 39:
                            {
                                AstNode r = New39();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 40:
                            {
                                AstNode r = New40();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 41:
                            {
                                AstNode r = New41();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 42:
                            {
                                AstNode r = New42();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 43:
                            {
                                AstNode r = New43();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 44:
                            {
                                AstNode r = New44();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 45:
                            {
                                AstNode r = New45();
                                Push(GoTo(5), r, true);
                                continue;
                            }
                        case 46:
                            {
                                AstNode r = New46();
                                Push(GoTo(6), r, true);
                                continue;
                            }
                        case 47:
                            {
                                AstNode r = New47();
                                Push(GoTo(6), r, true);
                                continue;
                            }
                        case 48:
                            {
                                AstNode r = New48();
                                Push(GoTo(6), r, true);
                                continue;
                            }
                        case 49:
                            {
                                AstNode r = New49();
                                Push(GoTo(7), r, true);
                                continue;
                            }
                        case 50:
                            {
                                AstNode r = New50();
                                Push(GoTo(7), r, true);
                                continue;
                            }
                        case 51:
                            {
                                AstNode r = New51();
                                Push(GoTo(7), r, true);
                                continue;
                            }
                        case 52:
                            {
                                AstNode r = New52();
                                Push(GoTo(7), r, true);
                                continue;
                            }
                        case 53:
                            {
                                AstNode r = New53();
                                Push(GoTo(8), r, true);
                                continue;
                            }
                        case 54:
                            {
                                AstNode r = New54();
                                Push(GoTo(8), r, true);
                                continue;
                            }
                        case 55:
                            {
                                AstNode r = New55();
                                Push(GoTo(8), r, true);
                                continue;
                            }
                        case 56:
                            {
                                AstNode r = New56();
                                Push(GoTo(9), r, true);
                                continue;
                            }
                        case 57:
                            {
                                AstNode r = New57();
                                Push(GoTo(9), r, true);
                                continue;
                            }
                        case 58:
                            {
                                AstNode r = New58();
                                Push(GoTo(10), r, true);
                                continue;
                            }
                        case 59:
                            {
                                AstNode r = New59();
                                Push(GoTo(10), r, true);
                                continue;
                            }
                        case 60:
                            {
                                AstNode r = New60();
                                Push(GoTo(11), r, true);
                                continue;
                            }
                        case 61:
                            {
                                AstNode r = New61();
                                Push(GoTo(12), r, true);
                                continue;
                            }
                        case 62:
                            {
                                AstNode r = New62();
                                Push(GoTo(13), r, true);
                                continue;
                            }
                        case 63:
                            {
                                AstNode r = New63();
                                Push(GoTo(14), r, true);
                                continue;
                            }
                        case 64:
                            {
                                AstNode r = New64();
                                Push(GoTo(15), r, true);
                                continue;
                            }
                        case 65:
                            {
                                AstNode r = New65();
                                Push(GoTo(16), r, true);
                                continue;
                            }
                        case 66:
                            {
                                AstNode r = New66();
                                Push(GoTo(17), r, true);
                                continue;
                            }
                        case 67:
                            {
                                AstNode r = New67();
                                Push(GoTo(18), r, true);
                                continue;
                            }
                        case 68:
                            {
                                AstNode r = New68();
                                Push(GoTo(19), r, true);
                                continue;
                            }
                        case 69:
                            {
                                AstNode r = New69();
                                Push(GoTo(20), r, true);
                                continue;
                            }
                        case 70:
                            {
                                AstNode r = New70();
                                Push(GoTo(21), r, true);
                                continue;
                            }
                        case 71:
                            {
                                AstNode r = New71();
                                Push(GoTo(22), r, true);
                                continue;
                            }
                        case 72:
                            {
                                AstNode r = New72();
                                Push(GoTo(23), r, true);
                                continue;
                            }
                        case 73:
                            {
                                AstNode r = New73();
                                Push(GoTo(24), r, true);
                                continue;
                            }
                        case 74:
                            {
                                AstNode r = New74();
                                Push(GoTo(24), r, true);
                                continue;
                            }
                        case 75:
                            {
                                AstNode r = New75();
                                Push(GoTo(25), r, true);
                                continue;
                            }
                        case 76:
                            {
                                AstNode r = New76();
                                Push(GoTo(26), r, true);
                                continue;
                            }
                        case 77:
                            {
                                AstNode r = New77();
                                Push(GoTo(27), r, true);
                                continue;
                            }
                        case 78:
                            {
                                AstNode r = New78();
                                Push(GoTo(28), r, true);
                                continue;
                            }
                        case 79:
                            {
                                AstNode r = New79();
                                Push(GoTo(29), r, true);
                                continue;
                            }
                        case 80:
                            {
                                AstNode r = New80();
                                Push(GoTo(30), r, true);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
                else if (_action[0] == Accept)
                {
                    var node2 = (EOF)_lexer.Next();
                    var node1 = (PProgram)Pop();
                    return new Start(node1, node2);
                }
                else if (_action[0] == Error)
                {
                    throw new ParserException(
                        _lastToken,
                        "[" + _lastLine + "," + _lastPos + "] " + _errorMessages[_errors[_action[1]]]);
                }
            }
        }

        protected AstNode New0()
        {
            XPSubroutine node5 = (XPSubroutine)Pop();
            PReturn node4 = (PReturn)Pop();
            PJumpToSubroutine node3 = (PJumpToSubroutine)Pop();
            PRsaddCommand node2 = null;
            PSize node1 = (PSize)Pop();
            return new AProgram(node1, node2, node3, node4, node5);
        }

        protected AstNode New1()
        {
            PSubroutine node2 = (PSubroutine)Pop();
            XPSubroutine node1 = (XPSubroutine)Pop();
            return new X1PSubroutine(node1, node2);
        }

        protected AstNode New2()
        {
            PSubroutine node1 = (PSubroutine)Pop();
            return new X2PSubroutine(node1);
        }

        protected AstNode New3()
        {
            XPSubroutine node5 = (XPSubroutine)Pop();
            PReturn node4 = (PReturn)Pop();
            PJumpToSubroutine node3 = (PJumpToSubroutine)Pop();
            PRsaddCommand node2 = (PRsaddCommand)Pop();
            PSize node1 = (PSize)Pop();
            return new AProgram(node1, node2, node3, node4, node5);
        }

        protected AstNode New4()
        {
            PReturn node2 = (PReturn)Pop();
            PCommandBlock node1 = null;
            return new ASubroutine(node1, node2);
        }

        protected AstNode New5()
        {
            PReturn node2 = (PReturn)Pop();
            PCommandBlock node1 = (PCommandBlock)Pop();
            return new ASubroutine(node1, node2);
        }

        protected AstNode New6()
        {
            XPCmd node1 = (XPCmd)Pop();
            return new ACommandBlock(node1);
        }

        protected AstNode New7()
        {
            PCmd node2 = (PCmd)Pop();
            XPCmd node1 = (XPCmd)Pop();
            return new X1PCmd(node1, node2);
        }

        protected AstNode New8()
        {
            PCmd node1 = (PCmd)Pop();
            return new X2PCmd(node1);
        }

        protected AstNode New9()
        {
            PRsaddCommand node1 = (PRsaddCommand)Pop();
            return new AAddVarCmd(node1);
        }

        protected AstNode New10()
        {
            PReturn node4 = (PReturn)Pop();
            PCommandBlock node3 = (PCommandBlock)Pop();
            PJumpCommand node2 = (PJumpCommand)Pop();
            PStoreStateCommand node1 = (PStoreStateCommand)Pop();
            return new AActionJumpCmd(node1, node2, node3, node4);
        }

        protected AstNode New11()
        {
            PConstCommand node1 = (PConstCommand)Pop();
            return new AConstCmd(node1);
        }

        protected AstNode New12()
        {
            PCopyDownSpCommand node1 = (PCopyDownSpCommand)Pop();
            return new ACopydownspCmd(node1);
        }

        protected AstNode New13()
        {
            PCopyTopSpCommand node1 = (PCopyTopSpCommand)Pop();
            return new ACopytopspCmd(node1);
        }

        protected AstNode New14()
        {
            PCopyDownBpCommand node1 = (PCopyDownBpCommand)Pop();
            return new ACopydownbpCmd(node1);
        }

        protected AstNode New15()
        {
            PCopyTopBpCommand node1 = (PCopyTopBpCommand)Pop();
            return new ACopytopbpCmd(node1);
        }

        protected AstNode New16()
        {
            PConditionalJumpCommand node1 = (PConditionalJumpCommand)Pop();
            return new ACondJumpCmd(node1);
        }

        protected AstNode New17()
        {
            PJumpCommand node1 = (PJumpCommand)Pop();
            return new AJumpCmd(node1);
        }

        protected AstNode New18()
        {
            PJumpToSubroutine node1 = (PJumpToSubroutine)Pop();
            return new AJumpSubCmd(node1);
        }

        protected AstNode New19()
        {
            PMoveSpCommand node1 = (PMoveSpCommand)Pop();
            return new AMovespCmd(node1);
        }

        protected AstNode New20()
        {
            PLogiiCommand node1 = (PLogiiCommand)Pop();
            return new ALogiiCmd(node1);
        }

        protected AstNode New21()
        {
            PUnaryCommand node1 = (PUnaryCommand)Pop();
            return new AUnaryCmd(node1);
        }

        protected AstNode New22()
        {
            PBinaryCommand node1 = (PBinaryCommand)Pop();
            return new ABinaryCmd(node1);
        }

        protected AstNode New23()
        {
            PDestructCommand node1 = (PDestructCommand)Pop();
            return new ADestructCmd(node1);
        }

        protected AstNode New24()
        {
            PBpCommand node1 = (PBpCommand)Pop();
            return new ABpCmd(node1);
        }

        protected AstNode New25()
        {
            PActionCommand node1 = (PActionCommand)Pop();
            return new AActionCmd(node1);
        }

        protected AstNode New26()
        {
            PStackCommand node1 = (PStackCommand)Pop();
            return new AStackOpCmd(node1);
        }

        protected AstNode New27()
        {
            TLogandii node1 = (TLogandii)Pop();
            return new AAndLogiiOp(node1);
        }

        protected AstNode New28()
        {
            TLogorii node1 = (TLogorii)Pop();
            return new AOrLogiiOp(node1);
        }

        protected AstNode New29()
        {
            TIncorii node1 = (TIncorii)Pop();
            return new AInclOrLogiiOp(node1);
        }

        protected AstNode New30()
        {
            TExcorii node1 = (TExcorii)Pop();
            return new AExclOrLogiiOp(node1);
        }

        protected AstNode New31()
        {
            TBoolandii node1 = (TBoolandii)Pop();
            return new ABitAndLogiiOp(node1);
        }

        protected AstNode New32()
        {
            TEqual node1 = (TEqual)Pop();
            return new AEqualBinaryOp(node1);
        }

        protected AstNode New33()
        {
            TNequal node1 = (TNequal)Pop();
            return new ANequalBinaryOp(node1);
        }

        protected AstNode New34()
        {
            TGeq node1 = (TGeq)Pop();
            return new AGeqBinaryOp(node1);
        }

        protected AstNode New35()
        {
            TGt node1 = (TGt)Pop();
            return new AGtBinaryOp(node1);
        }

        protected AstNode New36()
        {
            TLt node1 = (TLt)Pop();
            return new ALtBinaryOp(node1);
        }

        protected AstNode New37()
        {
            TLeq node1 = (TLeq)Pop();
            return new ALeqBinaryOp(node1);
        }

        protected AstNode New38()
        {
            TShright node1 = (TShright)Pop();
            return new AShrightBinaryOp(node1);
        }

        protected AstNode New39()
        {
            TShleft node1 = (TShleft)Pop();
            return new AShleftBinaryOp(node1);
        }

        protected AstNode New40()
        {
            TUnright node1 = (TUnright)Pop();
            return new AUnrightBinaryOp(node1);
        }

        protected AstNode New41()
        {
            TAdd node1 = (TAdd)Pop();
            return new AAddBinaryOp(node1);
        }

        protected AstNode New42()
        {
            TSub node1 = (TSub)Pop();
            return new ASubBinaryOp(node1);
        }

        protected AstNode New43()
        {
            TMul node1 = (TMul)Pop();
            return new AMulBinaryOp(node1);
        }

        protected AstNode New44()
        {
            TDiv node1 = (TDiv)Pop();
            return new ADivBinaryOp(node1);
        }

        protected AstNode New45()
        {
            TMod node1 = (TMod)Pop();
            return new AModBinaryOp(node1);
        }

        protected AstNode New46()
        {
            TNeg node1 = (TNeg)Pop();
            return new ANegUnaryOp(node1);
        }

        protected AstNode New47()
        {
            TComp node1 = (TComp)Pop();
            return new ACompUnaryOp(node1);
        }

        protected AstNode New48()
        {
            TNot node1 = (TNot)Pop();
            return new ANotUnaryOp(node1);
        }

        protected AstNode New49()
        {
            TDecisp node1 = (TDecisp)Pop();
            return new ADecispStackOp(node1);
        }

        protected AstNode New50()
        {
            TIncisp node1 = (TIncisp)Pop();
            return new AIncispStackOp(node1);
        }

        protected AstNode New51()
        {
            TDecibp node1 = (TDecibp)Pop();
            return new ADecibpStackOp(node1);
        }

        protected AstNode New52()
        {
            TIncibp node1 = (TIncibp)Pop();
            return new AIncibpStackOp(node1);
        }

        protected AstNode New53()
        {
            TIntegerConstant node1 = (TIntegerConstant)Pop();
            return new AIntConstant(node1);
        }

        protected AstNode New54()
        {
            TFloatConstant node1 = (TFloatConstant)Pop();
            return new AFloatConstant(node1);
        }

        protected AstNode New55()
        {
            TStringLiteral node1 = (TStringLiteral)Pop();
            return new AStringConstant(node1);
        }

        protected AstNode New56()
        {
            TJz node1 = (TJz)Pop();
            return new AZeroJumpIf(node1);
        }

        protected AstNode New57()
        {
            TJnz node1 = (TJnz)Pop();
            return new ANonzeroJumpIf(node1);
        }

        protected AstNode New58()
        {
            TSavebp node1 = (TSavebp)Pop();
            return new ASavebpBpOp(node1);
        }

        protected AstNode New59()
        {
            TRestorebp node1 = (TRestorebp)Pop();
            return new ARestorebpBpOp(node1);
        }

        protected AstNode New60()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PJumpIf node1 = (PJumpIf)Pop();
            return new AConditionalJumpCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New61()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TJmp node1 = (TJmp)Pop();
            return new AJumpCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New62()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TJsr node1 = (TJsr)Pop();
            return new AJumpToSubroutine(node1, node2, node3, node4, node5);
        }

        protected AstNode New63()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TRetn node1 = (TRetn)Pop();
            return new AReturn(node1, node2, node3, node4);
        }

        protected AstNode New64()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TCpdownsp node1 = (TCpdownsp)Pop();
            return new ACopyDownSpCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New65()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TCptopsp node1 = (TCptopsp)Pop();
            return new ACopyTopSpCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New66()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TCpdownbp node1 = (TCpdownbp)Pop();
            return new ACopyDownBpCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New67()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TCptopbp node1 = (TCptopbp)Pop();
            return new ACopyTopBpCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New68()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TMovsp node1 = (TMovsp)Pop();
            return new AMoveSpCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New69()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TRsadd node1 = (TRsadd)Pop();
            return new ARsaddCommand(node1, node2, node3, node4);
        }

        protected AstNode New70()
        {
            TSemi node5 = (TSemi)Pop();
            PConstant node4 = (PConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TConst node1 = (TConst)Pop();
            return new AConstCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New71()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TAction node1 = (TAction)Pop();
            return new AActionCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New72()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PLogiiOp node1 = (PLogiiOp)Pop();
            return new ALogiiCommand(node1, node2, node3, node4);
        }

        protected AstNode New73()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = null;
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PBinaryOp node1 = (PBinaryOp)Pop();
            return new ABinaryCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New74()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PBinaryOp node1 = (PBinaryOp)Pop();
            return new ABinaryCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New75()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PUnaryOp node1 = (PUnaryOp)Pop();
            return new AUnaryCommand(node1, node2, node3, node4);
        }

        protected AstNode New76()
        {
            TSemi node5 = (TSemi)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PStackOp node1 = (PStackOp)Pop();
            return new AStackCommand(node1, node2, node3, node4, node5);
        }

        protected AstNode New77()
        {
            TSemi node7 = (TSemi)Pop();
            TIntegerConstant node6 = (TIntegerConstant)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TDestruct node1 = (TDestruct)Pop();
            return new ADestructCommand(node1, node2, node3, node4, node5, node6, node7);
        }

        protected AstNode New78()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            PBpOp node1 = (PBpOp)Pop();
            return new ABpCommand(node1, node2, node3, node4);
        }

        protected AstNode New79()
        {
            TSemi node6 = (TSemi)Pop();
            TIntegerConstant node5 = (TIntegerConstant)Pop();
            TIntegerConstant node4 = (TIntegerConstant)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TStorestate node1 = (TStorestate)Pop();
            return new AStoreStateCommand(node1, node2, node3, node4, node5, node6);
        }

        protected AstNode New80()
        {
            TSemi node4 = (TSemi)Pop();
            TIntegerConstant node3 = (TIntegerConstant)Pop();
            TIntegerConstant node2 = (TIntegerConstant)Pop();
            TT node1 = (TT)Pop();
            return new ASize(node1, node2, node3, node4);
        }
    }
}
