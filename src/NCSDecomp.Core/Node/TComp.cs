// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class TComp : Token
    {
        public TComp() { SetText("COMP"); }
        public TComp(int line, int pos) { SetText("COMP"); SetLine(line); SetPos(pos); }
        protected override Token CloneToken() { return new TComp(GetLine(), GetPos()); }
        public override void Apply(Switch sw)
        {
            if (sw is IAnalysis a)
            {
                a.CaseTComp(this);
            }
        }
    }
}
