// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;

namespace NCSDecomp.Core.ScriptNode
{
    public class ASwitch : ScriptNode
    {
        protected IAExpression switchexp;
        protected List<ASwitchCase> cases;
        protected ASwitchCase defaultcase;
        protected int start;
        protected int end;

        public ASwitch(int start, IAExpression switchexp)
        {
            this.start = start;
            cases = new List<ASwitchCase>();
            SwitchExp(switchexp);
        }

        public void SwitchExp(IAExpression value)
        {
            ((ScriptNode)value).Parent(this);
            switchexp = value;
        }

        public IAExpression SwitchExp()
        {
            return switchexp;
        }

        public void End(int endPos)
        {
            end = endPos;
            if (defaultcase != null)
            {
                defaultcase.End(endPos);
            }
            else if (cases.Count > 0)
            {
                cases[cases.Count - 1].End(endPos);
            }
        }

        public int End()
        {
            return end;
        }

        public void AddCase(ASwitchCase acase)
        {
            acase.Parent(this);
            cases.Add(acase);
        }

        public void AddDefaultCase(ASwitchCase acase)
        {
            acase.Parent(this);
            defaultcase = acase;
        }

        public ASwitchCase GetLastCase()
        {
            return cases[cases.Count - 1];
        }

        public ASwitchCase GetNextCase(ASwitchCase lastcase)
        {
            if (lastcase == null)
            {
                return GetFirstCase();
            }

            if (ReferenceEquals(lastcase, defaultcase))
            {
                return null;
            }

            int index = cases.IndexOf(lastcase) + 1;
            if (index == 0)
            {
                throw new System.InvalidOperationException("invalid last case passed in");
            }

            return cases.Count > index ? cases[index] : defaultcase;
        }

        public ASwitchCase GetFirstCase()
        {
            return cases.Count > 0 ? cases[0] : defaultcase;
        }

        public int GetFirstCaseStart()
        {
            if (cases.Count > 0)
            {
                return cases[0].GetStart();
            }

            return defaultcase != null ? defaultcase.GetStart() : -1;
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append(tabs).Append("switch(").Append(switchexp).Append(") {").Append(newline);
            for (int i = 0; i < cases.Count; i++)
            {
                buff.Append(cases[i].ToString());
            }

            if (defaultcase != null)
            {
                buff.Append(defaultcase.ToString());
            }

            buff.Append(tabs).Append("}").Append(newline);
            return buff.ToString();
        }

        public override void Close()
        {
            base.Close();
            if (cases != null)
            {
                foreach (ScriptNode param in cases)
                {
                    param.Close();
                }

                cases = null;
            }

            if (switchexp != null)
            {
                ((ScriptNode)switchexp).Close();
                switchexp = null;
            }

            if (defaultcase != null)
            {
                defaultcase.Close();
            }

            defaultcase = null;
        }
    }
}
