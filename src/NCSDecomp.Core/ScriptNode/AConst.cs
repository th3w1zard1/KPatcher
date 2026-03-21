// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AConst : ScriptNode, IAExpression
    {
        private Const theconst;

        public AConst(Const theconst)
        {
            this.theconst = theconst;
        }

        public override string ToString()
        {
            return theconst.ToString();
        }

        public StackEntry Stackentry()
        {
            return theconst;
        }

        public void Stackentry(StackEntry e)
        {
            theconst = (Const)e;
        }

        public override void Close()
        {
            base.Close();
            theconst = null;
        }
    }
}
