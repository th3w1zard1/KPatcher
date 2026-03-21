// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Base class for all lexer tokens; stores text and source position.
    /// </summary>
    public abstract class Token : Node
    {
        private string _text;
        private int _line;
        private int _pos;

        public string GetText() { return _text ?? ""; }
        public void SetText(string text) { _text = text; }

        public int GetLine() { return _line; }
        public void SetLine(int line) { _line = line; }

        public int GetPos() { return _pos; }
        public void SetPos(int pos) { _pos = pos; }

        public override string ToString()
        {
            return (GetText() ?? "") + " ";
        }

        internal override void RemoveChild(Node child) { }
        internal override void ReplaceChild(Node oldChild, Node newChild) { }

        public abstract override void Apply(Switch sw);

        public override object Clone()
        {
            return CloneToken();
        }

        protected abstract Token CloneToken();
    }
}
