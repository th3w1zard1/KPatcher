// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    public class AControlLoop : ScriptRootNode
    {
        protected IAExpression condition;

        public AControlLoop(int start, int end)
            : base(start, end)
        {
        }

        public void Condition(IAExpression value)
        {
            ((ScriptNode)value).Parent(this);
            condition = value;
        }

        public IAExpression Condition()
        {
            return condition;
        }

        protected string FormattedCondition()
        {
            if (condition == null)
            {
                return " ()";
            }

            string cond = condition.ToString().Trim();
            bool wrapped = IsWrappedInParens(cond);
            string wrappedCond = wrapped ? cond : "(" + cond + ")";
            return " " + wrappedCond;
        }

        private static bool IsWrappedInParens(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2)
            {
                return false;
            }

            if (s[0] != '(' || s[s.Length - 1] != ')')
            {
                return false;
            }

            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0 && i < s.Length - 1)
                    {
                        return false;
                    }
                }
            }

            return depth == 0;
        }

        public override void Close()
        {
            base.Close();
            if (condition != null)
            {
                ((ScriptNode)condition).Close();
                condition = null;
            }
        }
    }
}
