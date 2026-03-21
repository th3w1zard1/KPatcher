// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.IO;

namespace NCSDecomp.Core.Lexer
{
    /// <summary>
    /// TextReader that supports pushing back characters (for lexer).
    /// </summary>
    public sealed class PushbackReader : TextReader
    {
        private readonly TextReader _inner;
        private readonly List<int> _pushback = new List<int>();

        public PushbackReader(TextReader inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override int Read()
        {
            if (_pushback.Count > 0)
            {
                int last = _pushback.Count - 1;
                int c = _pushback[last];
                _pushback.RemoveAt(last);
                return c;
            }
            return _inner.Read();
        }

        public void Unread(int c)
        {
            _pushback.Add(c);
        }

        /// <summary>
        /// Pushes back characters after the accepted prefix (same range as DeNCS <c>Lexer.pushBack</c>).
        /// </summary>
        public void PushBack(string text, int acceptLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            for (int i = text.Length - 1; i >= acceptLength; i--)
            {
                _pushback.Add(text[i]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
