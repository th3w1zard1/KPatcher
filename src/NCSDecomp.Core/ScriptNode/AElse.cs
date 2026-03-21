// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Text;

namespace NCSDecomp.Core.ScriptNode
{
    public class AElse : ScriptRootNode
    {
        public AElse(int start, int end)
            : base(start, end)
        {
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            if (children.Count == 1 && GetChild(0) is AIf ifChild)
            {
                string cond;
                if (ifChild.Condition() == null)
                {
                    cond = " ()";
                }
                else
                {
                    string condStr = ifChild.Condition().ToString().Trim();
                    bool wrapped = condStr.StartsWith("(") && condStr.EndsWith(")");
                    cond = wrapped ? condStr : "(" + condStr + ")";
                    cond = " " + cond;
                }

                buff.Append(tabs).Append("else if").Append(cond).Append(" {").Append(newline);
                for (int i = 0; i < ifChild.Size(); i++)
                {
                    buff.Append(ifChild.GetChild(i).ToString());
                }

                buff.Append(tabs).Append("}").Append(newline);
            }
            else
            {
                buff.Append(tabs).Append("else {").Append(newline);
                for (int i = 0; i < children.Count; i++)
                {
                    buff.Append(GetChild(i).ToString());
                }

                buff.Append(tabs).Append("}").Append(newline);
            }

            return buff.ToString();
        }
    }
}
