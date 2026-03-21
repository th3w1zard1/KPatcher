// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;
using KPatcher.Core.Formats.NCS.Decompiler;
using NCSDecomp.Core.Node;
using LexerImpl = global::NCSDecomp.Core.Lexer.Lexer;
using ParserImpl = global::NCSDecomp.Core.Parser.Parser;

namespace NCSDecomp.Core
{
    /// <summary>
    /// DeNCS pipeline stages on shared KPatcher.Core bytecode: decode → lexer → parser AST.
    /// Full NSS emission: run <see cref="FileDecompiler"/> after parse.
    /// </summary>
    public static class NcsParsePipeline
    {
        /// <summary>
        /// Decode NCS bytes to the DeNCS token stream (same string <see cref="NCSDecompiler"/> produces for tests).
        /// </summary>
        public static string DecodeToTokenStream(byte[] ncsBytes, IActionsData actions = null)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
            {
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            }

            var decoder = new Decoder(ncsBytes);
            return decoder.Decode(actions);
        }

        /// <summary>
        /// Run decode → SableCC lexer → parser and return the root <see cref="Start"/> node.
        /// </summary>
        public static Start ParseAst(byte[] ncsBytes, IActionsData actions = null)
        {
            string tokenStream = DecodeToTokenStream(ncsBytes, actions);
            var lexer = new LexerImpl(new StringReader(tokenStream));
            var parser = new ParserImpl(lexer);
            return parser.Parse();
        }
    }
}
