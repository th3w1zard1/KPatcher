// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class TStringLiteral : Token
    {
        public TStringLiteral(string text) { SetText(text ?? ""); }
        public TStringLiteral(string text, int line, int pos) { SetText(text ?? ""); SetLine(line); SetPos(pos); }
        protected override Token CloneToken() { return new TStringLiteral(GetText(), GetLine(), GetPos()); }
        public override void Apply(Switch sw) { if (sw is IAnalysis a)
            {
                a.CaseTStringLiteral(this);
            }
        }
    }
}
