// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AActionExp : ScriptNode, IAExpression
    {
        private List<IAExpression> @params;
        private readonly string action;
        private StackEntry stackentry;
        private readonly int id;
        private readonly ActionsData actionsData;

        public AActionExp(string action, int id, List<IAExpression> @params)
            : this(action, id, @params, null)
        {
        }

        public AActionExp(string action, int id, List<IAExpression> @params, ActionsData actionsData)
        {
            this.action = action;
            this.@params = new List<IAExpression>();
            this.actionsData = actionsData;
            for (int i = 0; i < @params.Count; i++)
            {
                AddParam(@params[i]);
            }

            stackentry = null;
            this.id = id;
        }

        protected void AddParam(IAExpression param)
        {
            param.Parent(this);
            @params.Add(param);
        }

        public IAExpression GetParam(int pos)
        {
            return @params[pos];
        }

        public string Action()
        {
            return action;
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append(action).Append("(");
            string prefix = "";
            int paramCount = @params.Count;
            for (int i = 0; i < paramCount; i++)
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

        public int GetId()
        {
            return id;
        }

        public override void Close()
        {
            base.Close();
            if (@params != null)
            {
                foreach (IAExpression param in @params)
                {
                    if (param is ScriptNode sn)
                    {
                        sn.Close();
                    }
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
