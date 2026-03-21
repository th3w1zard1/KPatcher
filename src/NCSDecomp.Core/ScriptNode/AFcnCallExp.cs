// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AFcnCallExp : ScriptNode, IAExpression
    {
        private List<IAExpression> @params;
        private readonly byte id;
        private StackEntry stackentry;

        public AFcnCallExp(byte id, List<IAExpression> @params)
        {
            this.id = id;
            this.@params = new List<IAExpression>();
            for (int i = 0; i < @params.Count; i++)
            {
                AddParam(@params[i]);
            }
        }

        protected void AddParam(IAExpression param)
        {
            param.Parent(this);
            @params.Add(param);
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append("sub").Append(id).Append("(");
            string prefix = "";
            for (int i = 0; i < @params.Count; i++)
            {
                buff.Append(prefix).Append(@params[i].ToString());
                prefix = ", ";
            }

            buff.Append(")");
            return buff.ToString();
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
            if (@params != null)
            {
                foreach (IAExpression param in @params)
                {
                    ((ScriptNode)param).Close();
                }

                @params = null;
            }

            if (stackentry != null)
            {
                stackentry.Close();
            }

            stackentry = null;
        }
    }
}
