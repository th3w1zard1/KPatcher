// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    public class AReturnStatement : ScriptNode
    {
        protected IAExpression returnexp;

        public AReturnStatement()
        {
        }

        public AReturnStatement(IAExpression returnexp)
        {
            Returnexp(returnexp);
        }

        public void Returnexp(IAExpression value)
        {
            ((ScriptNode)value).Parent(this);
            returnexp = value;
        }

        public IAExpression Exp()
        {
            return returnexp;
        }

        public override string ToString()
        {
            return returnexp == null
                ? tabs + "return;" + newline
                : tabs + "return " + ExpressionFormatter.FormatValue(returnexp) + ";" + newline;
        }

        public override void Close()
        {
            base.Close();
            if (returnexp != null)
            {
                ((ScriptNode)returnexp).Close();
                returnexp = null;
            }
        }
    }
}
