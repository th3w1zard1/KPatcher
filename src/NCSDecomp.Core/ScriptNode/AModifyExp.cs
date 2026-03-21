// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AModifyExp : ScriptNode, IAExpression
    {
        private AVarRef varref;
        private IAExpression exp;

        public AModifyExp(AVarRef varref, IAExpression exp)
        {
            VarRef(varref);
            Expression(exp);
        }

        protected void VarRef(AVarRef value)
        {
            varref = value;
            value.Parent(this);
        }

        protected void Expression(IAExpression value)
        {
            exp = value;
            ((ScriptNode)value).Parent(this);
        }

        public IAExpression Expression()
        {
            return exp;
        }

        public AVarRef VarRef()
        {
            return varref;
        }

        public override string ToString()
        {
            return ExpressionFormatter.Format(this);
        }

        public StackEntry Stackentry()
        {
            return varref.Var();
        }

        public void Stackentry(StackEntry e)
        {
        }

        public override void Close()
        {
            base.Close();
            if (exp != null)
            {
                ((ScriptNode)exp).Close();
                exp = null;
            }

            if (varref != null)
            {
                varref.Close();
            }

            varref = null;
        }
    }
}
