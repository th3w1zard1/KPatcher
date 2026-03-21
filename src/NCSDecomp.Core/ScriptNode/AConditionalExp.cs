// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AConditionalExp : ScriptNode, IAExpression
    {
        private IAExpression left;
        private IAExpression right;
        private readonly string op;
        private StackEntry stackentry;
        private bool forceParens;

        public AConditionalExp(IAExpression left, IAExpression right, string op)
        {
            Left(left);
            Right(right);
            this.op = op;
        }

        protected void Left(IAExpression value)
        {
            left = value;
            ((ScriptNode)value).Parent(this);
        }

        protected void Right(IAExpression value)
        {
            right = value;
            ((ScriptNode)value).Parent(this);
        }

        public IAExpression Left()
        {
            return left;
        }

        public IAExpression Right()
        {
            return right;
        }

        public string Op()
        {
            return op;
        }

        public bool ForceParens()
        {
            return forceParens;
        }

        public void ForceParens(bool value)
        {
            forceParens = value;
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
            if (left != null)
            {
                ((ScriptNode)left).Close();
                left = null;
            }

            if (right != null)
            {
                ((ScriptNode)right).Close();
                right = null;
            }

            if (stackentry != null)
            {
                stackentry.Close();
            }

            stackentry = null;
        }
    }
}
