// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Lexer
{
    /// <summary>
    /// Checked exception indicating tokenization failure in the generated lexer.
    /// </summary>
    public class LexerException : Exception
    {
        public LexerException(string message) : base(message) { }
        public LexerException(string message, Exception inner) : base(message, inner) { }
    }
}
