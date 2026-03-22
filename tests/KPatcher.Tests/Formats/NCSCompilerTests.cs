using System;
using System.Collections.Generic;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Common.Script;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Compiler;
using KPatcher.Core.Formats.NCS.Compiler.NSS;
using Xunit;

namespace KPatcher.Tests.Formats
{
    /// <summary>
    /// Tests for NSS to NCS compilation.
    /// 1:1 port of test_ncs.py from tests/resource/formats/test_ncs.py
    /// </summary>
    public class NCSCompilerTests
    {
        /// <summary>
        /// Compile NSS script to NCS bytecode.
        /// </summary>
        private NCS Compile(string script, Dictionary<string, byte[]> library = null, List<string> libraryLookup = null)
        {
            if (library == null)
            {
                library = new Dictionary<string, byte[]>();
            }

            var compiler = new NssCompiler(Game.K1, libraryLookup);
            NCS ncs = compiler.Compile(script, library);
            return ncs;
        }

        #region Engine Call Tests

        /// <summary>
        /// Test basic engine function call.
        /// </summary>
        [Fact]
        public void TestEnginecall()
        {
            NCS ncs = Compile(@"
            void main()
            {
                object oExisting = GetExitingObject();
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].FunctionName.Should().Be("GetExitingObject");
            interpreter.ActionSnapshots[0].ArgValues.Should().BeEmpty();
        }

        /// <summary>
        /// Test engine function call with return value.
        /// </summary>
        [Fact]
        public void TestEnginecallReturnValue()
        {
            NCS ncs = Compile(@"
                void main()
                {
                    int inescapable = GetAreaUnescapable();
                }
            ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("GetAreaUnescapable", (args) => 10);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(10);
        }

        /// <summary>
        /// Test engine function call with parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallWithParams()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string tag = ""something"";
                int n = 15;
                object oSomething = GetObjectByTag(tag, n);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].FunctionName.Should().Be("GetObjectByTag");
            interpreter.ActionSnapshots[0].ArgValues.Count.Should().Be(2);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be("something");
            interpreter.ActionSnapshots[0].ArgValues[1].Value.Should().Be(15);
        }

        /// <summary>
        /// Test engine function call with default parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallWithDefaultParams()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string tag = ""something"";
                object oSomething = GetObjectByTag(tag);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test engine function call with missing required parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallWithMissingParams()
        {
            string script = @"
            void main()
            {
                string tag = ""something"";
                object oSomething = GetObjectByTag();
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test engine function call with too many parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallWithTooManyParams()
        {
            string script = @"
            void main()
            {
                string tag = ""something"";
                object oSomething = GetObjectByTag("""", 0, ""shouldnotbehere"");
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test DelayCommand with nested function call.
        /// </summary>
        [Fact]
        public void TestEnginecallDelayCommand1()
        {
            NCS ncs = Compile(@"
            void main()
            {
                object oFirstPlayer = GetFirstPC();
                DelayCommand(1.0, GiveXPToCreature(oFirstPlayer, 9001));
            }
        ");
        }

        /// <summary>
        /// Test GetFirstObjectInShape with default parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallGetFirstObjectInShapeDefaults()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int nShape = SHAPE_CUBE;
                float fSize = 0.0;
                location lTarget;
                GetFirstObjectInShape(nShape, fSize, lTarget);
            }
        ");
        }

        /// <summary>
        /// Test GetFactionEqual with default parameters.
        /// </summary>
        [Fact]
        public void TestEnginecallGetFactionEqual()
        {
            NCS ncs = Compile(@"
            void main()
            {
                object oFirst;
                GetFactionEqual(oFirst);
            }
        ");
        }

        #endregion

        #region Arithmetic Operator Tests

        /// <summary>
        /// Test integer addition.
        /// </summary>
        [Fact]
        public void TestAddopIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 10 + 5;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(15);
        }

        /// <summary>
        /// Test float addition.
        /// </summary>
        [Fact]
        public void TestAddopFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 10.0 + 5.0;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(15.0f);
        }

        /// <summary>
        /// Test string concatenation.
        /// </summary>
        [Fact]
        public void TestAddopStringString()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string value = ""abc"" + ""def"";
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be("abcdef");
        }

        /// <summary>
        /// Test integer subtraction.
        /// </summary>
        [Fact]
        public void TestSubopIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 10 - 5;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(5);
        }

        /// <summary>
        /// Test float subtraction.
        /// </summary>
        [Fact]
        public void TestSubopFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 10.0 - 5.0;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(5.0f);
        }

        /// <summary>
        /// Test integer multiplication.
        /// </summary>
        [Fact]
        public void TestMulopIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 * 5;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(50);
        }

        /// <summary>
        /// Test float multiplication.
        /// </summary>
        [Fact]
        public void TestMulopFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float a = 10.0 * 5.0;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(50.0f);
        }

        /// <summary>
        /// Test integer division.
        /// </summary>
        [Fact]
        public void TestDivopIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 / 5;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(2);
        }

        /// <summary>
        /// Test float division.
        /// </summary>
        [Fact]
        public void TestDivopFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float a = 10.0 / 5.0;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(2.0f);
        }

        /// <summary>
        /// Test integer modulo.
        /// </summary>
        [Fact]
        public void TestModopIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 % 3;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1);
        }

        /// <summary>
        /// Test integer negation.
        /// </summary>
        [Fact]
        public void TestNegopInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = -10;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(-10);
        }

        /// <summary>
        /// Test float negation.
        /// </summary>
        [Fact]
        public void TestNegopFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float a = -10.0;
                PrintFloat(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(-10.0);
        }

        /// <summary>
        /// Test BIDMAS order of operations.
        /// </summary>
        [Fact]
        public void TestBidmas()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 2 + (5 * ((0)) + 5) * 3 + 2 - (2 + (2 * 4 - 12 / 2)) / 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(17);
        }

        /// <summary>
        /// Test operations with variables.
        /// </summary>
        [Fact]
        public void TestOpWithVariables()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10;
                int b = 5;
                int c = a * b * a;
                int d = 10 * 5 * 10;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(500);
            snapshot.Stack[snapshot.Stack.Count - 2].Value.Should().Be(500);
        }

        #endregion

        #region Logical Operator Tests

        /// <summary>
        /// Test logical NOT operator.
        /// </summary>
        [Fact]
        public void TestNotOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = !1;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test logical AND operator.
        /// </summary>
        [Fact]
        public void TestLogicalAndOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 0 && 0;
                int b = 1 && 0;
                int c = 1 && 1;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 3].Value.Should().Be(0);
            snapshot.Stack[snapshot.Stack.Count - 2].Value.Should().Be(0);
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1);
        }

        /// <summary>
        /// Test logical OR operator.
        /// </summary>
        [Fact]
        public void TestLogicalOrOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 0 || 0;
                int b = 1 || 0;
                int c = 1 || 1;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 3].Value.Should().Be(0);
            snapshot.Stack[snapshot.Stack.Count - 2].Value.Should().Be(1);
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1);
        }

        /// <summary>
        /// Test equality operator.
        /// </summary>
        [Fact]
        public void TestLogicalEquals()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1 == 1;
                int b = ""a"" == ""b"";
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 2].Value.Should().Be(1);
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(0);
        }

        /// <summary>
        /// Test inequality operator.
        /// </summary>
        [Fact]
        public void TestLogicalNotequalsOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1 != 1;
                int b = 1 != 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 2].Value.Should().Be(0);
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1);
        }

        #endregion

        #region Relational Operator Tests

        /// <summary>
        /// Test greater than operator.
        /// </summary>
        [Fact]
        public void TestCompareGreaterthanOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 > 1;
                int b = 10 > 10;
                int c = 10 > 20;

                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2].ArgValues[0].Value.Should().Be(0);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test greater than or equal operator.
        /// </summary>
        [Fact]
        public void TestCompareGreaterthanorequalOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 >= 1;
                int b = 10 >= 10;
                int c = 10 >= 20;

                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test less than operator.
        /// </summary>
        [Fact]
        public void TestCompareLessthanOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 < 1;
                int b = 10 < 10;
                int c = 10 < 20;

                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3].ArgValues[0].Value.Should().Be(0);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2].ArgValues[0].Value.Should().Be(0);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test less than or equal operator.
        /// </summary>
        [Fact]
        public void TestCompareLessthanorequalOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10 <= 1;
                int b = 10 <= 10;
                int c = 10 <= 20;

                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3].ArgValues[0].Value.Should().Be(0);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1].ArgValues[0].Value.Should().Be(1);
        }

        #endregion

        #region Bitwise Operator Tests

        /// <summary>
        /// Test bitwise OR operator.
        /// </summary>
        [Fact]
        public void TestBitwiseOrOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 5 | 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(7);
        }

        /// <summary>
        /// Test bitwise XOR operator.
        /// </summary>
        [Fact]
        public void TestBitwiseXorOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 7 ^ 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(5);
        }

        /// <summary>
        /// Test bitwise NOT operator.
        /// </summary>
        [Fact]
        public void TestBitwiseNotInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = ~1;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(-2);
        }

        /// <summary>
        /// Test bitwise AND operator.
        /// </summary>
        [Fact]
        public void TestBitwiseAndOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 7 & 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(2);
        }

        /// <summary>
        /// Test bitwise left shift operator.
        /// </summary>
        [Fact]
        public void TestBitwiseShiftleftOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 7 << 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(28);
        }

        /// <summary>
        /// Test bitwise right shift operator.
        /// </summary>
        [Fact]
        public void TestBitwiseShiftrightOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 7 >> 2;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1);
        }

        #endregion

        #region Assignment Tests

        /// <summary>
        /// Test basic assignment.
        /// </summary>
        [Fact]
        public void TestAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                a = 4;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(4);
        }

        /// <summary>
        /// Test complex assignment expression.
        /// </summary>
        [Fact]
        public void TestAssignmentComplex()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                a = a * 2 + 8;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(10);
        }

        /// <summary>
        /// Test string constant assignment.
        /// </summary>
        [Fact]
        public void TestAssignmentStringConstant()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string a = ""A"";
                PrintString(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be("A");
        }

        /// <summary>
        /// Test string assignment from engine call.
        /// </summary>
        [Fact]
        public void TestAssignmentStringEnginecall()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string a = GetGlobalString(""A"");
                PrintString(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("GetGlobalString", (args) => args[0]);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be("A");
        }

        /// <summary>
        /// Test integer addition assignment.
        /// </summary>
        [Fact]
        public void TestAdditionAssignmentIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 1;
                value += 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test integer addition assignment with float.
        /// </summary>
        [Fact]
        public void TestAdditionAssignmentIntFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 1;
                value += 2.0;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test float addition assignment.
        /// </summary>
        [Fact]
        public void TestAdditionAssignmentFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 1.0;
                value += 2.0;
                PrintFloat(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
        }

        /// <summary>
        /// Test float addition assignment with integer.
        /// </summary>
        [Fact]
        public void TestAdditionAssignmentFloatInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 1.0;
                value += 2;
                PrintFloat(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintFloat");
            lastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
        }

        /// <summary>
        /// Test string concatenation assignment.
        /// </summary>
        [Fact]
        public void TestAdditionAssignmentStringString()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string value = ""a"";
                value += ""b"";
                PrintString(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintString");
            lastSnapshot.ArgValues[0].Value.Should().Be("ab");
        }

        /// <summary>
        /// Test integer subtraction assignment.
        /// </summary>
        [Fact]
        public void TestSubtractionAssignmentIntInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 10;
                value -= 2 * 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(6);
        }

        /// <summary>
        /// Test integer subtraction assignment with float.
        /// </summary>
        [Fact]
        public void TestSubtractionAssignmentIntFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 10;
                value -= 2.0;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(8.0f);
        }

        /// <summary>
        /// Test float subtraction assignment.
        /// </summary>
        [Fact]
        public void TestSubtractionAssignmentFloatFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 10.0;
                value -= 2.0;
                PrintFloat(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintFloat");
            lastSnapshot.ArgValues[0].Value.Should().Be(8.0f);
        }

        /// <summary>
        /// Test float subtraction assignment with integer.
        /// </summary>
        [Fact]
        public void TestSubtractionAssignmentFloatInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float value = 10.0;
                value -= 2;
                PrintFloat(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(8.0f);
        }

        /// <summary>
        /// Test multiplication assignment.
        /// </summary>
        [Fact]
        public void TestMultiplicationAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 10;
                value *= 2 * 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(40);
        }

        /// <summary>
        /// Test division assignment.
        /// </summary>
        [Fact]
        public void TestDivisionAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 12;
                value /= 2 * 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test bitwise AND assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseAndAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 0xFF;
                value &= 0x0F;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(15);
        }

        /// <summary>
        /// Test bitwise OR assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseOrAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 0x0F;
                value |= 0xF0;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(255);
        }

        /// <summary>
        /// Test bitwise XOR assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseXorAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 0xFF;
                value ^= 0x0F;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(240);
        }

        /// <summary>
        /// Test bitwise left shift assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseLeftShiftAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 7;
                value <<= 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(28);
        }

        /// <summary>
        /// Test bitwise right shift assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseRightShiftAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 28;
                value >>= 2;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(7);
        }

        /// <summary>
        /// Test bitwise unsigned right shift assignment.
        /// </summary>
        [Fact]
        public void TestBitwiseUnsignedRightShiftAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 0x80000000;
                value >>>= 1;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(1073741824);
        }

        /// <summary>
        /// Test bitwise assignment with complex expression.
        /// </summary>
        [Fact]
        public void TestBitwiseAssignmentWithExpression()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 0xFF;
                value &= 0x0F | 0x10;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(31);
        }

        /// <summary>
        /// Test global variable bitwise assignment.
        /// </summary>
        [Fact]
        public void TestGlobalBitwiseAssignment()
        {
            NCS ncs = Compile(@"
            int global1 = 0xFF;

            void main()
            {
                int local1 = 0x0F;
                global1 &= local1;
                PrintInteger(global1);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(15);
        }

        /// <summary>
        /// Test modulo assignment.
        /// </summary>
        [Fact]
        public void TestModuloAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 17;
                value %= 5;
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.FunctionName.Should().Be("PrintInteger");
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test bitwise unsigned right shift operator.
        /// </summary>
        [Fact]
        public void TestBitwiseUnsignedRightShiftOp()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 0x80000000 >>> 1;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var snapshot = interpreter.StackSnapshots[interpreter.StackSnapshots.Count - 4];
            snapshot.Stack[snapshot.Stack.Count - 1].Value.Should().Be(1073741824);
        }

        #endregion

        #region Const Keyword Tests

        /// <summary>
        /// Test const global variable declaration.
        /// </summary>
        [Fact]
        public void TestConstGlobalDeclaration()
        {
            NCS ncs = Compile(@"
            const int TEST_CONST = 42;

            void main()
            {
                PrintInteger(TEST_CONST);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(42);
        }

        /// <summary>
        /// Test const global variable initialization.
        /// </summary>
        [Fact]
        public void TestConstGlobalInitialization()
        {
            NCS ncs = Compile(@"
            const float PI = 3.14159f;
            const string GREETING = ""Hello"";

            void main()
            {
                PrintFloat(PI);
                PrintString(GREETING);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            // Convert to double for comparison - matching Python's float comparison
            double piValue = secondLastSnapshot.ArgValues[0].Value is float f ? (double)f : Convert.ToDouble(secondLastSnapshot.ArgValues[0].Value);
            System.Math.Abs(piValue - 3.141592).Should().BeLessThan(0.000001);
            lastSnapshot.ArgValues[0].Value.Should().Be("Hello");
        }

        /// <summary>
        /// Test const local variable declaration.
        /// </summary>
        [Fact]
        public void TestConstLocalDeclaration()
        {
            NCS ncs = Compile(@"
            void main()
            {
                const int local_const = 100;
                PrintInteger(local_const);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(100);
        }

        /// <summary>
        /// Test const local variable initialization with expression.
        /// </summary>
        [Fact]
        public void TestConstLocalInitialization()
        {
            NCS ncs = Compile(@"
            void main()
            {
                const int a = 10;
                const int b = a * 2;
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(20);
        }

        /// <summary>
        /// Test that assigning to const variable raises error.
        /// </summary>
        [Fact]
        public void TestConstAssignmentError()
        {
            string script = @"
            const int TEST_CONST = 42;

            void main()
            {
                TEST_CONST = 100;
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test that compound assignment to const variable raises error.
        /// </summary>
        [Fact]
        public void TestConstCompoundAssignmentError()
        {
            string script = @"
            const int TEST_CONST = 42;

            void main()
            {
                TEST_CONST += 10;
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test that bitwise assignment to const variable raises error.
        /// </summary>
        [Fact]
        public void TestConstBitwiseAssignmentError()
        {
            string script = @"
            const int TEST_CONST = 0xFF;

            void main()
            {
                TEST_CONST &= 0x0F;
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test that incrementing const variable raises error.
        /// </summary>
        [Fact]
        public void TestConstIncrementError()
        {
            string script = @"
            const int TEST_CONST = 42;

            void main()
            {
                TEST_CONST++;
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test multiple const variable declarations.
        /// </summary>
        [Fact]
        public void TestConstMultiDeclaration()
        {
            NCS ncs = Compile(@"
            void main()
            {
                const int a = 1, b = 2, c = 3;
                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(1);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        #endregion

        #region Ternary Operator Tests

        /// <summary>
        /// Test basic ternary operator.
        /// </summary>
        [Fact]
        public void TestTernaryBasic()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1 > 0 ? 100 : 200;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(100);
        }

        /// <summary>
        /// Test ternary operator false branch.
        /// </summary>
        [Fact]
        public void TestTernaryFalseBranch()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 0 > 1 ? 100 : 200;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(200);
        }

        /// <summary>
        /// Test ternary operator with variables.
        /// </summary>
        [Fact]
        public void TestTernaryWithVariables()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int b = 5;
                int c = (b > 3) ? 100 : 200;
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(100);
        }

        /// <summary>
        /// Test nested ternary operator.
        /// </summary>
        [Fact]
        public void TestTernaryNested()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                int b = 2;
                int result = (a > b) ? 10 : ((a < b) ? 20 : 30);
                PrintInteger(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(20);
        }

        /// <summary>
        /// Test ternary operator in larger expression.
        /// </summary>
        [Fact]
        public void TestTernaryInExpression()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 5;
                int result = (a > 3 ? 10 : 5) + 20;
                PrintInteger(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(30);
        }

        /// <summary>
        /// Test that ternary with mismatched types raises error.
        /// </summary>
        [Fact]
        public void TestTernaryTypeMismatchError()
        {
            string script = @"
            void main()
            {
                int result = 1 ? 100 : ""string"";
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test ternary operator with float branches.
        /// </summary>
        [Fact]
        public void TestTernaryFloatBranches()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float result = 1 ? 3.14f : 2.71f;
                PrintFloat(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(3.14f);
        }

        /// <summary>
        /// Test ternary operator with string branches.
        /// </summary>
        [Fact]
        public void TestTernaryStringBranches()
        {
            NCS ncs = Compile(@"
            void main()
            {
                string result = 1 ? ""yes"" : ""no"";
                PrintString(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be("yes");
        }

        /// <summary>
        /// Test ternary operator with function calls.
        /// </summary>
        [Fact]
        public void TestTernaryWithFunctionCalls()
        {
            NCS ncs = Compile(@"
            int GetValue(int x)
            {
                return x * 2;
            }

            void main()
            {
                int result = 1 ? GetValue(5) : GetValue(10);
                PrintInteger(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(10);
        }

        /// <summary>
        /// Test ternary operator result assigned to variable.
        /// </summary>
        [Fact]
        public void TestTernaryAssignment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10;
                int b = 20;
                int max = (a > b) ? a : b;
                PrintInteger(max);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(20);
        }

        /// <summary>
        /// Test ternary operator precedence.
        /// </summary>
        [Fact]
        public void TestTernaryPrecedence()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1 ? 2 : 3 ? 4 : 5;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        #endregion

        #region Control Flow Tests

        /// <summary>
        /// Test if statement.
        /// </summary>
        [Fact]
        public void TestIf()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if(0)
                {
                    PrintInteger(0);
                }

                if(1)
                {
                    PrintInteger(1);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test if statement with multiple conditions.
        /// </summary>
        [Fact]
        public void TestIfMultipleConditions()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if(1 && 2 && 3)
                {
                    PrintInteger(0);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test if-else statement.
        /// </summary>
        [Fact]
        public void TestIfElse()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if (0) {    PrintInteger(0); }
                else {      PrintInteger(1); }

                if (1) {    PrintInteger(2); }
                else {      PrintInteger(3); }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test if-else if statement.
        /// </summary>
        [Fact]
        public void TestIfElseIf()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if (0)      { PrintInteger(0); }
                else if (0) { PrintInteger(1); }

                if (1)      { PrintInteger(2); } // hit
                else if (1) { PrintInteger(3); }

                if (1)      { PrintInteger(4); } // hit
                else if (0) { PrintInteger(5); }

                if (0)      { PrintInteger(6); }
                else if (1) { PrintInteger(7); } // hit
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(4);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(7);
        }

        /// <summary>
        /// Test if-else if-else statement.
        /// </summary>
        [Fact]
        public void TestIfElseIfElse()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if (0)      { PrintInteger(0); }
                else if (0) { PrintInteger(1); }
                else        { PrintInteger(3); } // hit

                if (0)      { PrintInteger(4); }
                else if (1) { PrintInteger(5); } // hit
                else        { PrintInteger(6); }

                if (1)      { PrintInteger(7); } // hit
                else if (1) { PrintInteger(8); }
                else        { PrintInteger(9); }

                if (1)      { PrintInteger(10); } //hit
                else if (0) { PrintInteger(11); }
                else        { PrintInteger(12); }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(4);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(5);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(7);
            interpreter.ActionSnapshots[3].ArgValues[0].Value.Should().Be(10);
        }

        /// <summary>
        /// Test single statement if (no braces).
        /// </summary>
        [Fact]
        public void TestSingleStatementIf()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if (1) PrintInteger(222);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(222);
        }

        /// <summary>
        /// Test single statement else if-else.
        /// </summary>
        [Fact]
        public void TestSingleStatementElseIfElse()
        {
            NCS ncs = Compile(@"
            void main()
            {
                if (0) PrintInteger(11);
                else if (0) PrintInteger(22);
                else PrintInteger(33);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(33);
        }

        /// <summary>
        /// Test while loop.
        /// </summary>
        [Fact]
        public void TestWhileLoop()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 3;
                while (value)
                {
                    PrintInteger(value);
                    value -= 1;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test while loop with break.
        /// </summary>
        [Fact]
        public void TestWhileLoopWithBreak()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 3;
                while (value)
                {
                    PrintInteger(value);
                    value -= 1;
                    break;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test while loop with continue.
        /// </summary>
        [Fact]
        public void TestWhileLoopWithContinue()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 3;
                while (value)
                {
                    PrintInteger(value);
                    value -= 1;
                    continue;
                    PrintInteger(99);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test while loop variable scope.
        /// </summary>
        [Fact]
        public void TestWhileLoopScope()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 11;
                int outer = 22;
                while (value)
                {
                    int inner = 33;
                    value = 0;
                    continue;
                    outer = 99;
                }

                PrintInteger(outer);
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(22);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test do-while loop.
        /// </summary>
        [Fact]
        public void TestDoWhileLoop()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 3;
                do
                {
                    PrintInteger(value);
                    value -= 1;
                } while (value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test do-while loop with break.
        /// </summary>
        [Fact]
        public void TestDoWhileLoopWithBreak()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 3;
                do
                {
                    PrintInteger(value);
                    value -= 1;
                    break;
                } while (value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test for loop.
        /// </summary>
        [Fact]
        public void TestForLoop()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int i = 99;
                for (i = 1; i <= 3; i += 1)
                {
                    PrintInteger(i);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test for loop with break.
        /// </summary>
        [Fact]
        public void TestForLoopWithBreak()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int i = 99;
                for (i = 1; i <= 3; i += 1)
                {
                    PrintInteger(i);
                    break;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test for loop with continue.
        /// </summary>
        [Fact]
        public void TestForLoopWithContinue()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int i = 99;
                for (i = 1; i <= 3; i += 1)
                {
                    PrintInteger(i);
                    continue;
                    PrintInteger(99);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(3);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[2].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test for loop variable scope.
        /// </summary>
        [Fact]
        public void TestForLoopScope()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int i = 11;
                int outer = 22;
                for (i = 0; i <= 5; i += 1)
                {
                    int inner = 33;
                    break;
                }

                PrintInteger(i);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test switch statement without breaks (fall-through).
        /// </summary>
        [Fact]
        public void TestSwitchNoBreaks()
        {
            NCS ncs = Compile(@"
            void main()
            {
                switch (2)
                {
                    case 1:
                        PrintInteger(1);
                    case 2:
                        PrintInteger(2);
                    case 3:
                        PrintInteger(3);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(2);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test switch statement jumping over all cases.
        /// </summary>
        [Fact]
        public void TestSwitchJumpOver()
        {
            NCS ncs = Compile(@"
            void main()
            {
                switch (4)
                {
                    case 1:
                        PrintInteger(1);
                    case 2:
                        PrintInteger(2);
                    case 3:
                        PrintInteger(3);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(0);
        }

        /// <summary>
        /// Test switch statement with breaks.
        /// </summary>
        [Fact]
        public void TestSwitchWithBreaks()
        {
            NCS ncs = Compile(@"
            void main()
            {
                switch (3)
                {
                    case 1:
                        PrintInteger(1);
                        break;
                    case 2:
                        PrintInteger(2);
                        break;
                    case 3:
                        PrintInteger(3);
                        break;
                    case 4:
                        PrintInteger(4);
                        break;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test switch statement with default case.
        /// </summary>
        [Fact]
        public void TestSwitchWithDefault()
        {
            NCS ncs = Compile(@"
            void main()
            {
                switch (4)
                {
                    case 1:
                        PrintInteger(1);
                        break;
                    case 2:
                        PrintInteger(2);
                        break;
                    case 3:
                        PrintInteger(3);
                        break;
                    default:
                        PrintInteger(4);
                        break;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(4);
        }

        /// <summary>
        /// Test switch statement with scoped blocks.
        /// </summary>
        [Fact]
        public void TestSwitchScopedBlocks()
        {
            NCS ncs = Compile(@"
            void main()
            {
                switch (2)
                {
                    case 1:
                    {
                        int inner = 10;
                        PrintInteger(inner);
                    }
                    break;

                    case 2:
                    {
                        int inner = 20;
                        PrintInteger(inner);
                    }
                    break;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(20);
        }

        #endregion

        #region Variable and Scope Tests

        /// <summary>
        /// Test basic variable scope.
        /// </summary>
        [Fact]
        public void TestScope()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = 1;

                if (value == 1)
                {
                    value = 2;
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test scoped block.
        /// </summary>
        [Fact]
        public void TestScopedBlock()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;

                {
                    int b = 2;
                    PrintInteger(a);
                    PrintInteger(b);
                }
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(1);
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test multiple variable declarations.
        /// </summary>
        [Fact]
        public void TestMultiDeclarations()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value1, value2 = 1, value3 = 2;

                PrintInteger(value1);
                PrintInteger(value2);
                PrintInteger(value3);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(0);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(1);
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test local variable declarations.
        /// </summary>
        [Fact]
        public void TestLocalDeclarations()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int INT;
                float FLOAT;
                string STRING;
                location LOCATION;
                effect EFFECT;
                talent TALENT;
                event EVENT;
                vector VECTOR;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test global variable declarations.
        /// </summary>
        [Fact]
        public void TestGlobalDeclarations()
        {
            NCS ncs = Compile(@"
            int INT;
            float FLOAT;
            string STRING;
            location LOCATION;
            effect EFFECT;
            talent TALENT;
            event EVENT;
            vector VECTOR;

            void main()
            {

            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            bool hasSaveBp = false;
            foreach (var inst in ncs.Instructions)
            {
                if (inst.InsType == NCSInstructionType.SAVEBP)
                {
                    hasSaveBp = true;
                    break;
                }
            }
            hasSaveBp.Should().BeTrue();
        }

        /// <summary>
        /// Test global variable initializations.
        /// </summary>
        [Fact]
        public void TestGlobalInitializations()
        {
            NCS ncs = Compile(@"
            int INT = 0;
            float FLOAT = 0.0;
            string STRING = """";
            vector VECTOR = [0.0, 0.0, 0.0];

            void main()
            {
                PrintInteger(INT);
                PrintFloat(FLOAT);
                PrintString(STRING);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(0);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(0.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be("");

            bool hasSaveBp = false;
            foreach (var inst in ncs.Instructions)
            {
                if (inst.InsType == NCSInstructionType.SAVEBP)
                {
                    hasSaveBp = true;
                    break;
                }
            }
            hasSaveBp.Should().BeTrue();
        }

        /// <summary>
        /// Test global variable initialization with unary operator.
        /// </summary>
        [Fact]
        public void TestGlobalInitializationWithUnary()
        {
            NCS ncs = Compile(@"
            int INT = -1;

            void main()
            {
                PrintInteger(INT);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(-1);
        }

        /// <summary>
        /// Test global integer addition assignment.
        /// </summary>
        [Fact]
        public void TestGlobalIntAdditionAssignment()
        {
            NCS ncs = Compile(@"
            int global1 = 1;
            int global2 = 2;

            void main()
            {
                int local1 = 3;
                int local2 = 4;

                global1 += local1;
                global2 = local2 + global1;

                PrintInteger(global1);
                PrintInteger(global2);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(4);
            lastSnapshot.ArgValues[0].Value.Should().Be(8);
        }

        /// <summary>
        /// Test integer declaration without initialization.
        /// </summary>
        [Fact]
        public void TestDeclarationInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a;
                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test float declaration without initialization.
        /// </summary>
        [Fact]
        public void TestDeclarationFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                float a;
                PrintFloat(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(0.0f);
        }

        #endregion

        #region Data Type Tests

        /// <summary>
        /// Test different float literal notations.
        /// </summary>
        [Fact]
        public void TestFloatNotations()
        {
            NCS ncs = Compile(@"
            void main()
            {
                PrintFloat(1.0f);
                PrintFloat(2.0);
                PrintFloat(3f);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(1);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test vector creation and operations.
        /// </summary>
        [Fact]
        public void TestVector()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector vec = Vector(2.0, 4.0, 4.0);
                float mag = VectorMagnitude(vec);
                PrintFloat(mag);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.SetMock("VectorMagnitude", (args) => ((Vector3)args[0]).Magnitude());
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
        }

        /// <summary>
        /// Test vector literal notation.
        /// </summary>
        [Fact]
        public void TestVectorNotation()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector vec = [1.0, 2.0, 3.0];
                PrintFloat(vec.x);
                PrintFloat(vec.y);
                PrintFloat(vec.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(1.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
        }

        /// <summary>
        /// Test vector component access.
        /// </summary>
        [Fact]
        public void TestVectorGetComponents()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector vec = Vector(2.0, 4.0, 6.0);
                PrintFloat(vec.x);
                PrintFloat(vec.y);
                PrintFloat(vec.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
        }

        /// <summary>
        /// Test vector component assignment.
        /// </summary>
        [Fact]
        public void TestVectorSetComponents()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector vec = Vector(0.0, 0.0, 0.0);
                vec.x = 2.0;
                vec.y = 4.0;
                vec.z = 6.0;
                PrintFloat(vec.x);
                PrintFloat(vec.y);
                PrintFloat(vec.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
        }

        /// <summary>
        /// Test struct member access.
        /// </summary>
        [Fact]
        public void TestStructGetMembers()
        {
            NCS ncs = Compile(@"
            struct ABC
            {
                int value1;
                string value2;
                float value3;
            };

            void main()
            {
                struct ABC abc;
                PrintInteger(abc.value1);
                PrintString(abc.value2);
                PrintFloat(abc.value3);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(0);
            secondLastSnapshot.ArgValues[0].Value.Should().Be("");
            lastSnapshot.ArgValues[0].Value.Should().Be(0.0f);
        }

        /// <summary>
        /// Test accessing invalid struct member.
        /// </summary>
        [Fact]
        public void TestStructGetInvalidMember()
        {
            string source = @"
            struct ABC
            {
                int value1;
                string value2;
                float value3;
            };

            void main()
            {
                struct ABC abc;
                PrintFloat(abc.value4);
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        /// <summary>
        /// Test struct member assignment.
        /// </summary>
        [Fact]
        public void TestStructSetMembers()
        {
            NCS ncs = Compile(@"
            struct ABC
            {
                int value1;
                string value2;
                float value3;
            };

            void main()
            {
                struct ABC abc;
                abc.value1 = 123;
                abc.value2 = ""abc"";
                abc.value3 = 3.14;
                PrintInteger(abc.value1);
                PrintString(abc.value2);
                PrintFloat(abc.value3);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();


            // Access values directly using indexer to match negative index access
            var snapshots = interpreter.ActionSnapshots;
            int idx1 = snapshots.Count - 3;
            int idx2 = snapshots.Count - 2;
            int idx3 = snapshots.Count - 1;

            // Access StackObjects and values in minimal steps
            var argVals1 = snapshots[idx1].ArgValues;
            var argVals2 = snapshots[idx2].ArgValues;
            var argVals3 = snapshots[idx3].ArgValues;

            var so1 = argVals1[0];
            var so2 = argVals2[0];
            var so3 = argVals3[0];

            // Extract values - use field access via reflection to completely bypass property getter
            var valueProp = typeof(KPatcher.Core.Formats.NCS.Compiler.StackObject).GetProperty("Value");
            object v1 = valueProp.GetValue(so1);
            object v2 = valueProp.GetValue(so2);
            object v3 = valueProp.GetValue(so3);

            // Convert and assert
            int intVal = v1 is int ? (int)v1 : System.Convert.ToInt32(v1);
            Assert.Equal(123, intVal);

            string strVal = v2 as string ?? v2?.ToString() ?? "";
            Assert.Equal("abc", strVal);

            float floatVal = v3 is float ? (float)v3 : System.Convert.ToSingle(v3);
            Assert.True(System.Math.Abs(floatVal - 3.14f) < 0.01f, $"Expected ~3.14, got {floatVal}");
        }

        #endregion

        #region Increment/Decrement Tests

        /// <summary>
        /// Test prefix increment on stack pointer integer.
        /// </summary>
        [Fact]
        public void TestPrefixIncrementSpInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                int b = ++a;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test prefix increment on base pointer integer.
        /// </summary>
        [Fact]
        public void TestPrefixIncrementBpInt()
        {
            NCS ncs = Compile(@"
            int a = 1;

            void main()
            {
                int b = ++a;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test postfix increment on stack pointer integer.
        /// </summary>
        [Fact]
        public void TestPostfixIncrementSpInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                int b = a++;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test postfix increment on base pointer integer.
        /// </summary>
        [Fact]
        public void TestPostfixIncrementBpInt()
        {
            NCS ncs = Compile(@"
            int a = 1;

            void main()
            {
                int b = a++;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test prefix decrement on stack pointer integer.
        /// </summary>
        [Fact]
        public void TestPrefixDecrementSpInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                int b = --a;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(0);
            lastSnapshot.ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test prefix decrement on base pointer integer.
        /// </summary>
        [Fact]
        public void TestPrefixDecrementBpInt()
        {
            NCS ncs = Compile(@"
            int a = 1;

            void main()
            {
                int b = --a;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(0);
            lastSnapshot.ArgValues[0].Value.Should().Be(0);
        }

        /// <summary>
        /// Test postfix decrement on stack pointer integer.
        /// </summary>
        [Fact]
        public void TestPostfixDecrementSpInt()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;
                int b = a--;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(0);
            lastSnapshot.ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test postfix decrement on base pointer integer.
        /// </summary>
        [Fact]
        public void TestPostfixDecrementBpInt()
        {
            NCS ncs = Compile(@"
            int a = 1;

            void main()
            {
                int b = a--;

                PrintInteger(a);
                PrintInteger(b);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(0);
            lastSnapshot.ArgValues[0].Value.Should().Be(1);
        }

        #endregion

        #region Function Tests

        /// <summary>
        /// Test function prototype with no arguments.
        /// </summary>
        [Fact]
        public void TestPrototypeNoArgs()
        {
            NCS ncs = Compile(@"
            void test();

            void main()
            {
                test();
            }

            void test()
            {
                PrintInteger(56);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(56);
        }

        /// <summary>
        /// Test function prototype with argument.
        /// </summary>
        [Fact]
        public void TestPrototypeWithArg()
        {
            NCS ncs = Compile(@"
            void test(int value);

            void main()
            {
                test(57);
            }

            void test(int value)
            {
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(57);
        }

        /// <summary>
        /// Test function prototype with three arguments.
        /// </summary>
        [Fact]
        public void TestPrototypeWithThreeArgs()
        {
            NCS ncs = Compile(@"
            void test(int a, int b, int c)
            {
                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }

            void main()
            {
                int a = 1, b = 2, c = 3;
                test(a, b, c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(1);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2);
            lastSnapshot.ArgValues[0].Value.Should().Be(3);
        }

        /// <summary>
        /// Test function prototype with many arguments including defaults.
        /// </summary>
        [Fact]
        public void TestPrototypeWithManyArgs()
        {
            NCS ncs = Compile(@"
            void test(int a, effect z, int b, int c, int d = 4)
            {
                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
                PrintInteger(d);
            }

            void main()
            {
                int a = 1, b = 2, c = 3;
                effect z;

                test(a, z, b, c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var fourthLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 4];
            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            fourthLastSnapshot.ArgValues[0].Value.Should().Be(1);
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(2);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(3);
            lastSnapshot.ArgValues[0].Value.Should().Be(4);
        }

        /// <summary>
        /// Test function prototype with default argument.
        /// </summary>
        [Fact]
        public void TestPrototypeWithDefaultArg()
        {
            NCS ncs = Compile(@"
            void test(int value = 57);

            void main()
            {
                test();
            }

            void test(int value = 57)
            {
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(57);
        }

        /// <summary>
        /// Test function prototype with default constant argument.
        /// </summary>
        [Fact]
        public void TestPrototypeWithDefaultConstantArg()
        {
            NCS ncs = Compile(@"
            void test(int value = DAMAGE_TYPE_COLD);

            void main()
            {
                test();
            }

            void test(int value = DAMAGE_TYPE_COLD)
            {
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(32);
        }

        /// <summary>
        /// Test function call with missing required argument.
        /// </summary>
        [Fact]
        public void TestPrototypeMissingArg()
        {
            string source = @"
            void test(int value);

            void main()
            {
                test();
            }

            void test(int value)
            {
                PrintInteger(value);
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        /// <summary>
        /// Test function call missing required argument when optional exists.
        /// </summary>
        [Fact]
        public void TestPrototypeMissingArgAndDefault()
        {
            string source = @"
            void test(int value1, int value2 = 123);

            void main()
            {
                test();
            }

            void test(int value1, int value2 = 123)
            {
                PrintInteger(value1);
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        /// <summary>
        /// Test function with default parameter before required (should error).
        /// </summary>
        [Fact]
        public void TestPrototypeDefaultBeforeRequired()
        {
            string source = @"
            void test(int value1 = 123, int value2);

            void main()
            {
                test(123, 123);
            }

            void test(int value1 = 123, int value2)
            {
                PrintInteger(value1);
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        /// <summary>
        /// Test redefining a function (should error).
        /// </summary>
        [Fact]
        public void TestRedefineFunction()
        {
            string script = @"
            void test()
            {

            }

            void test()
            {

            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test duplicate function prototype (should error).
        /// </summary>
        [Fact]
        public void TestDoublePrototype()
        {
            string script = @"
            void test();
            void test();
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test function prototype after definition (should error).
        /// </summary>
        [Fact]
        public void TestPrototypeAfterDefinition()
        {
            string script = @"
            void test()
            {

            }

            void test();
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test function prototype and definition parameter mismatch.
        /// </summary>
        [Fact]
        public void TestPrototypeAndDefinitionParamMismatch()
        {
            string script = @"
            void test(int a);

            void test()
            {

            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test function prototype and definition return type mismatch.
        /// </summary>
        [Fact]
        public void TestPrototypeAndDefinitionReturnMismatch()
        {
            string script = @"
            void test(int a);

            int test(int a)
            {

            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test calling undefined function (should error).
        /// </summary>
        [Fact]
        public void TestCallUndefined()
        {
            string script = @"
            void main()
            {
                test(0);
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(script));
        }

        /// <summary>
        /// Test calling void function with no arguments.
        /// </summary>
        [Fact]
        public void TestCallVoidWithNoArgs()
        {
            NCS ncs = Compile(@"
            void test()
            {
                PrintInteger(123);
            }

            void main()
            {
                test();
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(123);
        }

        /// <summary>
        /// Test calling void function with one argument.
        /// </summary>
        [Fact]
        public void TestCallVoidWithOneArg()
        {
            NCS ncs = Compile(@"
            void test(int value)
            {
                PrintInteger(value);
            }

            void main()
            {
                test(123);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(123);
        }

        /// <summary>
        /// Test calling void function with two arguments.
        /// </summary>
        [Fact]
        public void TestCallVoidWithTwoArgs()
        {
            NCS ncs = Compile(@"
            void test(int value1, int value2)
            {
                PrintInteger(value1);
                PrintInteger(value2);
            }

            void main()
            {
                test(1, 2);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
            interpreter.ActionSnapshots[1].ArgValues[0].Value.Should().Be(2);
        }

        /// <summary>
        /// Test calling integer-returning function with no arguments.
        /// </summary>
        [Fact]
        public void TestCallIntWithNoArgs()
        {
            NCS ncs = Compile(@"
            int test()
            {
                return 5;
            }

            void main()
            {
                int x = test();
                PrintInteger(x);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(5);
        }

        /// <summary>
        /// Test calling forward-declared integer-returning function.
        /// </summary>
        [Fact]
        public void TestCallIntWithNoArgsAndForwardDeclared()
        {
            NCS ncs = Compile(@"
            int test();

            int test()
            {
                return 5;
            }

            void main()
            {
                int x = test();
                PrintInteger(x);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(5);
        }

        /// <summary>
        /// Test function call with parameter type mismatch.
        /// </summary>
        [Fact]
        public void TestCallParamMismatch()
        {
            string source = @"
            int test(int a)
            {
                return a;
            }

            void main()
            {
                test(""123"");
            }
        ";

            Assert.Throws<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        #endregion

        #region Return Statement Tests

        /// <summary>
        /// Test return statement.
        /// </summary>
        [Fact]
        public void TestReturn()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1;

                if (a == 1)
                {
                    PrintInteger(a);
                    return;
                }

                PrintInteger(0);
                return;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test return statement with parentheses.
        /// </summary>
        [Fact]
        public void TestReturnParenthesis()
        {
            NCS ncs = Compile(@"
            int test()
            {
                return(321);
            }

            void main()
            {
                int value = test();
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(321);
        }

        /// <summary>
        /// Test return statement with constant in parentheses.
        /// </summary>
        [Fact]
        public void TestReturnParenthesisConstant()
        {
            NCS ncs = Compile(@"
            int test()
            {
                return(TRUE);
            }

            void main()
            {
                int value = test();
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(1);
        }

        /// <summary>
        /// Test integer declaration with parentheses.
        /// </summary>
        [Fact]
        public void TestIntParenthesisDeclaration()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int value = (123);
                PrintInteger(value);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(123);
        }

        #endregion

        #region Include Tests

        /// <summary>
        /// Test #include directive with built-in library.
        /// </summary>
        [Fact]
        public void TestIncludeBuiltin()
        {
            // Note: Using UTF-8 instead of windows-1252 for .NET Core compatibility
            byte[] otherscript = System.Text.Encoding.UTF8.GetBytes(@"
            void TestFunc()
            {
                PrintInteger(123);
            }
        ");

            var library = new Dictionary<string, byte[]>();
            library["otherscript"] = otherscript;
            NCS ncs = Compile(@"
            #include ""otherscript""

            void main()
            {
                TestFunc();
            }
        ", library);

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test nested #include directives.
        /// </summary>
        [Fact]
        public void TestNestedInclude()
        {
            // Note: Using UTF-8 instead of windows-1252 for .NET Core compatibility
            byte[] firstScript = System.Text.Encoding.UTF8.GetBytes(@"
            int SOME_COST = 13;

            void TestFunc(int value)
            {
                PrintInteger(value);
            }
        ");

            // Note: Using UTF-8 instead of windows-1252 for .NET Core compatibility
            byte[] secondScript = System.Text.Encoding.UTF8.GetBytes(@"
            #include ""first_script""
        ");

            var library = new Dictionary<string, byte[]>();
            library["first_script"] = firstScript;
            library["second_script"] = secondScript;
            NCS ncs = Compile(@"
            #include ""second_script""

            void main()
            {
                TestFunc(SOME_COST);
            }
        ", library);

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(1);
            interpreter.ActionSnapshots[0].ArgValues[0].Value.Should().Be(13);
        }

        /// <summary>
        /// Test missing #include file (should error).
        /// </summary>
        [Fact]
        public void TestMissingInclude()
        {
            // Note: Python's assertRaises catches subclasses, C# throws MissingIncludeError which inherits from CompileError
            string source = @"
            #include ""otherscript""

            void main()
            {
                TestFunc();
            }
        ";

            // Python's assertRaises catches subclasses, so MissingIncludeError (which inherits from CompileError) should be caught
            Assert.ThrowsAny<KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError>(() => Compile(source));
        }

        /// <summary>
        /// Test using global variable from included file.
        /// </summary>
        [Fact]
        public void TestImportedGlobalVariable()
        {
            // Note: Using UTF-8 instead of windows-1252 for .NET Core compatibility
            byte[] otherscript = System.Text.Encoding.UTF8.GetBytes(@"
            int iExperience = 55;
        ");

            var library = new Dictionary<string, byte[]>();
            library["otherscript"] = otherscript;
            NCS ncs = Compile(@"
            #include ""otherscript""

            void main()
            {
                object oPlayer = GetPCSpeaker();
                GiveXPToCreature(oPlayer, iExperience);
            }
        ", library);

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            interpreter.ActionSnapshots.Count.Should().Be(2);
            interpreter.ActionSnapshots[1].ArgValues[1].Value.Should().Be(55);
        }

        #endregion

        #region Comment Tests

        /// <summary>
        /// Test single-line comment.
        /// </summary>
        [Fact]
        public void TestComment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                // int a = ""abc""; // [] /*
                int a = 0;
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        /// <summary>
        /// Test multi-line comment.
        /// </summary>
        [Fact]
        public void TestMultilineComment()
        {
            NCS ncs = Compile(@"
            void main()
            {
                /* int
                abc =
                ;; 123
                */

                string aaa = """";
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();
        }

        #endregion

        #region Expression Tests

        /// <summary>
        /// Test expression without assignment.
        /// </summary>
        [Fact]
        public void TestAssignmentlessExpression()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 123;

                1;
                GetCheatCode(1);
                ""abc"";

                PrintInteger(a);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(123);
        }

        /// <summary>
        /// Test NOP statement.
        /// </summary>
        [Fact]
        public void TestNopStatement()
        {
            NCS ncs = Compile(@"
            void main()
            {
                NOP ""test message"";
                PrintInteger(42);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().Be(42);
        }

        #endregion

        #region Vector Arithmetic Tests

        /// <summary>
        /// Test vector addition.
        /// </summary>
        [Fact]
        public void TestVectorAddition()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v1 = Vector(1.0, 2.0, 3.0);
                vector v2 = Vector(4.0, 5.0, 6.0);
                vector result = v1 + v2;
                PrintFloat(result.x);
                PrintFloat(result.y);
                PrintFloat(result.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(5.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(7.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(9.0f);
        }

        /// <summary>
        /// Test vector subtraction.
        /// </summary>
        [Fact]
        public void TestVectorSubtraction()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v1 = Vector(5.0, 7.0, 9.0);
                vector v2 = Vector(1.0, 2.0, 3.0);
                vector result = v1 - v2;
                PrintFloat(result.x);
                PrintFloat(result.y);
                PrintFloat(result.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(5.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
        }

        /// <summary>
        /// Test vector multiplication by float.
        /// </summary>
        [Fact]
        public void TestVectorMultiplicationFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v1 = Vector(2.0, 3.0, 4.0);
                vector result = v1 * 2.0;
                PrintFloat(result.x);
                PrintFloat(result.y);
                PrintFloat(result.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(8.0f);
        }

        /// <summary>
        /// Test vector division by float.
        /// </summary>
        [Fact]
        public void TestVectorDivisionFloat()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v1 = Vector(8.0, 6.0, 4.0);
                vector result = v1 / 2.0;
                PrintFloat(result.x);
                PrintFloat(result.y);
                PrintFloat(result.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
        }

        /// <summary>
        /// Test vector compound assignment with addition.
        /// </summary>
        [Fact]
        public void TestVectorCompoundAssignmentAddition()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v = Vector(1.0, 2.0, 3.0);
                v += Vector(0.5, 0.5, 0.5);
                PrintFloat(v.x);
                PrintFloat(v.y);
                PrintFloat(v.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(1.5f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(2.5f);
            lastSnapshot.ArgValues[0].Value.Should().Be(3.5f);
        }

        /// <summary>
        /// Test vector compound assignment with subtraction.
        /// </summary>
        [Fact]
        public void TestVectorCompoundAssignmentSubtraction()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v = Vector(5.0, 5.0, 5.0);
                v -= Vector(1.0, 2.0, 3.0);
                PrintFloat(v.x);
                PrintFloat(v.y);
                PrintFloat(v.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
        }

        /// <summary>
        /// Test vector compound assignment with multiplication.
        /// </summary>
        [Fact]
        public void TestVectorCompoundAssignmentMultiplication()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v = Vector(2.0, 3.0, 4.0);
                v *= 2.0;
                PrintFloat(v.x);
                PrintFloat(v.y);
                PrintFloat(v.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(6.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(8.0f);
        }

        /// <summary>
        /// Test vector compound assignment with division.
        /// </summary>
        [Fact]
        public void TestVectorCompoundAssignmentDivision()
        {
            NCS ncs = Compile(@"
            void main()
            {
                vector v = Vector(8.0, 6.0, 4.0);
                v /= 2.0;
                PrintFloat(v.x);
                PrintFloat(v.y);
                PrintFloat(v.z);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.SetMock("Vector", (args) => new Vector3(
                args[0] is float f0 ? f0 : Convert.ToSingle(args[0]),
                args[1] is float f1 ? f1 : Convert.ToSingle(args[1]),
                args[2] is float f2 ? f2 : Convert.ToSingle(args[2])));
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(4.0f);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(3.0f);
            lastSnapshot.ArgValues[0].Value.Should().Be(2.0f);
        }

        #endregion

        #region Nested Struct Tests

        /// <summary>
        /// Test accessing nested struct members.
        /// </summary>
        [Fact]
        public void TestNestedStructAccess()
        {
            NCS ncs = Compile(@"
            struct Inner
            {
                int value;
            };

            struct Outer
            {
                struct Inner inner;
                string name;
            };

            void main()
            {
                struct Outer outer;
                outer.inner.value = 42;
                outer.name = ""test"";
                PrintInteger(outer.inner.value);
                PrintString(outer.name);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            secondLastSnapshot.ArgValues[0].Value.Should().Be(42);
            lastSnapshot.ArgValues[0].Value.Should().Be("test");
        }

        #endregion

        #region Complex Expression Tests

        /// <summary>
        /// Test complex expression with multiple operators and precedence.
        /// </summary>
        [Fact]
        public void TestComplexExpressionPrecedence()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 1 + 2 * 3 - 4 / 2;
                int b = (1 + 2) * (3 - 4) / 2;
                int c = 1 > 0 ? 10 + 5 : 20 - 5;
                PrintInteger(a);
                PrintInteger(b);
                PrintInteger(c);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var thirdLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 3];
            var secondLastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 2];
            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            thirdLastSnapshot.ArgValues[0].Value.Should().Be(5);
            secondLastSnapshot.ArgValues[0].Value.Should().Be(-1);
            lastSnapshot.ArgValues[0].Value.Should().Be(15);
        }

        /// <summary>
        /// Test expression combining all operator types.
        /// </summary>
        [Fact]
        public void TestExpressionWithAllOperators()
        {
            NCS ncs = Compile(@"
            void main()
            {
                int a = 10;
                int b = 5;
                int result = (a + b) * 2 - (a / b) % 3 & 0xFF | 0x0F ^ 0xAA << 1 >> 1;
                PrintInteger(result);
            }
        ");

            var interpreter = new Interpreter(ncs);
            interpreter.Run();

            var lastSnapshot = interpreter.ActionSnapshots[interpreter.ActionSnapshots.Count - 1];
            lastSnapshot.ArgValues[0].Value.Should().NotBeNull();
        }

        #endregion
    }
}
