// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class TIncibp : Token
    {
        public TIncibp() { SetText("INCIBP"); }
        public TIncibp(int line, int pos) { SetText("INCIBP"); SetLine(line); SetPos(pos); }
        protected override Token CloneToken() { return new TIncibp(GetLine(), GetPos()); }
        public override void Apply(Switch sw) { if (sw is IAnalysis a)
            {
                a.CaseTIncibp(this);
            }
        }
    }
}
