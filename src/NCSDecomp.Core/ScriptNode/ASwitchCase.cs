// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;

namespace NCSDecomp.Core.ScriptNode
{
    public class ASwitchCase : ScriptRootNode
    {
        protected AConst val;

        public ASwitchCase(int start, AConst val)
            : base(start, -1)
        {
            Val(val);
        }

        public ASwitchCase(int start)
            : base(start, -1)
        {
        }

        public new void End(int endPos)
        {
            end = endPos;
        }

        private void Val(AConst value)
        {
            val = value;
            value.Parent(this);
        }

        public List<AUnkLoopControl> GetUnknowns()
        {
            var unks = new List<AUnkLoopControl>();
            foreach (ScriptNode node in children)
            {
                if (node is AUnkLoopControl unk)
                {
                    unks.Add(unk);
                }
            }

            return unks;
        }

        public void ReplaceUnknown(AUnkLoopControl unk, ScriptNode newnode)
        {
            newnode.Parent(this);
            ReplaceChild(unk, newnode);
            unk.Parent(null);
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            if (val == null)
            {
                buff.Append(tabs).Append("default:").Append(newline);
            }
            else
            {
                buff.Append(tabs).Append("case ").Append(val.ToString()).Append(":").Append(newline);
            }

            for (int i = 0; i < children.Count; i++)
            {
                buff.Append(GetChild(i).ToString());
            }

            return buff.ToString();
        }

        public override void Close()
        {
            base.Close();
            if (val != null)
            {
                val.Close();
            }

            val = null;
        }
    }
}
