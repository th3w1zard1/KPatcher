// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.ScriptNode
{
    public class AVarRef : ScriptNode, IAExpression
    {
        private Variable var;

        public AVarRef(Variable var)
        {
            Var(var);
        }

        public AVarRef(VarStruct st)
        {
            Var(st);
        }

        public DecompType Type()
        {
            return var.Type();
        }

        public Variable Var()
        {
            return var;
        }

        public void Var(Variable value)
        {
            var = value;
        }

        public void ChooseStructElement(Variable v)
        {
            if (var is VarStruct vs && vs.Contains(v))
            {
                var = v;
            }
            else
            {
                throw new InvalidOperationException("Attempted to select a struct element not in struct");
            }
        }

        public override string ToString()
        {
            return var.ToString();
        }

        public StackEntry Stackentry()
        {
            return var;
        }

        public void Stackentry(StackEntry e)
        {
            var = (Variable)e;
        }

        public override void Close()
        {
            base.Close();
            if (var != null)
            {
                var.Close();
            }

            var = null;
        }
    }
}
