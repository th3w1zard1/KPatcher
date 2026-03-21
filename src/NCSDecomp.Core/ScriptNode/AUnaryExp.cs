// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AUnaryExp : ScriptNode, IAExpression
    {
        private IAExpression exp;
        private readonly string op;
        private StackEntry stackentry;

        public AUnaryExp(IAExpression exp, string op)
        {
            Exp(exp);
            this.op = op;
        }

        protected void Exp(IAExpression value)
        {
            exp = value;
            ((ScriptNode)value).Parent(this);
        }

        public IAExpression Exp()
        {
            return exp;
        }

        public string Op()
        {
            return op;
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
            if (exp != null)
            {
                ((ScriptNode)exp).Close();
            }

            exp = null;
            if (stackentry != null)
            {
                stackentry.Close();
            }

            stackentry = null;
        }
    }
}
