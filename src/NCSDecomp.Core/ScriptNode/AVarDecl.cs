// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.ScriptNode
{
    public class AVarDecl : ScriptNode
    {
        private Variable var;
        private IAExpression exp;
        private bool isFcnReturn;

        public AVarDecl(Variable var)
        {
            Var(var);
            exp = null;
            isFcnReturn = false;
        }

        public void Var(Variable value)
        {
            var = value;
        }

        public Variable Var()
        {
            return var;
        }

        public void IsFcnReturn(bool value)
        {
            isFcnReturn = value;
        }

        public bool IsFcnReturn()
        {
            return isFcnReturn;
        }

        public DecompType Type()
        {
            return var.Type();
        }

        public void InitializeExp(IAExpression value)
        {
            ((ScriptNode)value).Parent(this);
            exp = value;
        }

        public IAExpression RemoveExp()
        {
            IAExpression aexp = exp;
            exp.Parent(null);
            exp = null;
            return aexp;
        }

        public IAExpression Exp()
        {
            return exp;
        }

        public override string ToString()
        {
            return exp == null
                ? tabs + var.ToDeclString() + ";" + newline
                : tabs + var.ToDeclString() + " = " + ExpressionFormatter.FormatValue(exp) + ";" + newline;
        }

        public override void Close()
        {
            base.Close();
            if (exp != null)
            {
                ((ScriptNode)exp).Close();
                exp = null;
            }

            if (var != null)
            {
                var.Close();
            }

            var = null;
        }
    }
}
