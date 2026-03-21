// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Parser
{
    /// <summary>
    /// Checked exception indicating parse failure; carries the offending token.
    /// </summary>
    public class ParserException : Exception
    {
        private readonly Token _token;

        public ParserException(Token token, string message) : base(message)
        {
            _token = token;
        }

        public Token GetToken()
        {
            return _token;
        }
    }
}
