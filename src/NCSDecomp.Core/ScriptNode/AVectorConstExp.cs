// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AVectorConstExp : ScriptNode, IAExpression
    {
        private IAExpression exp1;
        private IAExpression exp2;
        private IAExpression exp3;

        public AVectorConstExp(IAExpression exp1, IAExpression exp2, IAExpression exp3)
        {
            Exp1(exp1);
            Exp2(exp2);
            Exp3(exp3);
        }

        public void Exp1(IAExpression value)
        {
            exp1 = value;
            ((ScriptNode)value).Parent(this);
        }

        public void Exp2(IAExpression value)
        {
            exp2 = value;
            ((ScriptNode)value).Parent(this);
        }

        public void Exp3(IAExpression value)
        {
            exp3 = value;
            ((ScriptNode)value).Parent(this);
        }

        public override string ToString()
        {
            return "[" + exp1 + "," + exp2 + "," + exp3 + "]";
        }

        public StackEntry Stackentry()
        {
            return null;
        }

        public void Stackentry(StackEntry e)
        {
        }

        public override void Close()
        {
            base.Close();
            if (exp1 != null)
            {
                ((ScriptNode)exp1).Close();
                exp1 = null;
            }

            if (exp2 != null)
            {
                ((ScriptNode)exp2).Close();
                exp2 = null;
            }

            if (exp3 != null)
            {
                ((ScriptNode)exp3).Close();
                exp3 = null;
            }
        }
    }
}
