// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AUnaryModExp : ScriptNode, IAExpression
    {
        private AVarRef varref;
        private readonly string op;
        private readonly bool prefix;
        private StackEntry stackentry;

        public AUnaryModExp(AVarRef varref, string op, bool prefix)
        {
            VarRef(varref);
            this.op = op;
            this.prefix = prefix;
        }

        protected void VarRef(AVarRef value)
        {
            varref = value;
            value.Parent(this);
        }

        public AVarRef VarRef()
        {
            return varref;
        }

        public string Op()
        {
            return op;
        }

        public bool Prefix()
        {
            return prefix;
        }

        public override string ToString()
        {
            return ExpressionFormatter.Format(this);
        }

        public StackEntry Stackentry()
        {
            return stackentry;
        }

        public void Stackentry(StackEntry e)
        {
            stackentry = e;
        }

        public override void Close()
        {
            base.Close();
            if (varref != null)
            {
                varref.Close();
            }

            varref = null;
            if (stackentry != null)
            {
                stackentry.Close();
            }

            stackentry = null;
        }
    }
}
