using System;
using System.IO;
using KPatcher.Core.Formats.NCS.Decompiler;
using KPatcher.Core.Tests.Common;
using NCSDecomp.Core.Lexer;
using NCSDecomp.Core.Node;
using Xunit;

namespace KPatcher.Tests.Formats
{
    public sealed class NcsLexerSmokeTest
    {
        [Fact]
        public void Lexer_TypesPrologue_ThenIntegers()
        {
            using (var r = new StringReader("MOVSP 19 0 ; "))
            {
                var lex = new Lexer(r);
                Token t0 = lex.Next();
                Assert.IsType<TMovsp>(t0);
            }

            using (var r = new StringReader(" "))
            {
                var lex = new Lexer(r);
                Assert.IsType<TBlank>(lex.Next());
            }

            using (var r = new StringReader("T 8 9232; "))
            {
                var lex = new Lexer(r);
                Assert.IsType<TT>(lex.Next());
                Assert.IsType<TBlank>(lex.Next());
                Assert.IsType<TIntegerConstant>(lex.Next());
                Assert.IsType<TBlank>(lex.Next());
                Assert.IsType<TIntegerConstant>(lex.Next());
                Assert.IsType<TSemi>(lex.Next());
            }
        }

        [Fact]
        public void Decoder_TestNcs_StartsWithTPrologue()
        {
            byte[] bytes = CompiledNcsTestFixture.ReferenceK1NcsBytes();
            var dec = new Decoder(bytes);
            string s = dec.Decode(null);
            Assert.StartsWith("T 8 ", s);
            Assert.Contains("; ", s);
        }
    }
}
