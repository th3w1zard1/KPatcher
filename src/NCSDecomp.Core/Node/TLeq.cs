// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class TLeq : Token
    {
        public TLeq() { SetText("LEQ"); }
        public TLeq(int line, int pos) { SetText("LEQ"); SetLine(line); SetPos(pos); }
        protected override Token CloneToken() { return new TLeq(GetLine(), GetPos()); }
        public override void Apply(Switch sw)
        {
            if (sw is IAnalysis a)
            {
                a.CaseTLeq(this);
            }
        }
    }
}
