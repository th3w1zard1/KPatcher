// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class TJsr : Token
    {
        public TJsr() { SetText("JSR"); }
        public TJsr(int line, int pos) { SetText("JSR"); SetLine(line); SetPos(pos); }
        protected override Token CloneToken() { return new TJsr(GetLine(), GetPos()); }
        public override void Apply(Switch sw)
        {
            if (sw is IAnalysis a)
            {
                a.CaseTJsr(this);
            }
        }
    }
}
