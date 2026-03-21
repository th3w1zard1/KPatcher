// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    public class AExpressionStatement : ScriptNode
    {
        private IAExpression exp;

        public AExpressionStatement(IAExpression exp)
        {
            exp.Parent(this);
            this.exp = exp;
        }

        public IAExpression Exp()
        {
            return exp;
        }

        public override string ToString()
        {
            return tabs + exp.ToString() + ";" + newline;
        }

        public override void Parent(ScriptNode parent)
        {
            base.Parent(parent);
            exp.Parent(this);
        }

        public override void Close()
        {
            base.Close();
            if (exp != null)
            {
                ((ScriptNode)exp).Close();
                exp = null;
            }
        }
    }
}
