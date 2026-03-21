// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Text;

namespace NCSDecomp.Core.ScriptNode
{
    public class AIf : AControlLoop
    {
        public AIf(int start, int end, IAExpression condition)
            : base(start, end)
        {
            Condition(condition);
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            string cond = FormattedCondition();
            buff.Append(tabs).Append("if").Append(cond).Append(" {").Append(newline);
            for (int i = 0; i < children.Count; i++)
            {
                buff.Append(GetChild(i).ToString());
            }

            buff.Append(tabs).Append("}").Append(newline);
            return buff.ToString();
        }
    }
}
