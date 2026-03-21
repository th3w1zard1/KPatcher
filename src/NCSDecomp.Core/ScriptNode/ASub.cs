// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.ScriptNode
{
    public class ASub : ScriptRootNode
    {
        private DecompType returnType;
        private readonly byte id;
        private List<AVarRef> @params;
        private string name;
        private bool ismain;

        public ASub(DecompType returnType, byte id, List<AVarRef> paramVars, int start, int end)
            : base(start, end)
        {
            this.returnType = returnType;
            this.id = id;
            @params = new List<AVarRef>();
            tabs = "";
            for (int i = 0; i < paramVars.Count; i++)
            {
                AddParam(paramVars[i]);
            }

            name = "sub" + id;
        }

        public ASub(int start, int end)
            : base(start, end)
        {
            returnType = new DecompType(0);
            @params = null;
            tabs = "";
        }

        protected void AddParam(AVarRef param)
        {
            param.Parent(this);
            @params.Add(param);
        }

        public override string ToString()
        {
            return GetHeader() + " {" + newline + GetBody() + "}" + newline;
        }

        public string GetBody()
        {
            var buff = new StringBuilder();
            for (int i = 0; i < children.Count; i++)
            {
                buff.Append(GetChild(i).ToString());
            }

            return buff.ToString();
        }

        public string GetHeader()
        {
            var buff = new StringBuilder();
            buff.Append(returnType).Append(" ").Append(name).Append("(");
            string link = "";
            for (int i = 0; i < @params.Count; i++)
            {
                AVarRef param = @params[i];
                DecompType ptype = param.Type();
                buff.Append(link).Append(ptype).Append(" ").Append(param.ToString());
                link = ", ";
            }

            buff.Append(")");
            return buff.ToString();
        }

        public void IsMain(bool value)
        {
            ismain = value;
            if (ismain)
            {
                if (returnType.Equals(DecompType.VtInteger))
                {
                    name = "StartingConditional";
                }
                else
                {
                    name = "main";
                }
            }
        }

        public bool IsMain()
        {
            return ismain;
        }

        public DecompType ReturnType()
        {
            return returnType;
        }

        public void Name(string value)
        {
            name = value;
        }

        public string Name()
        {
            return name;
        }

        public List<Variable> GetParamVars()
        {
            var vars = new List<Variable>();
            if (@params != null)
            {
                foreach (AVarRef p in @params)
                {
                    vars.Add(p.Var());
                }
            }

            return vars;
        }

        public override void Close()
        {
            base.Close();
            if (@params != null)
            {
                foreach (ScriptNode param in @params)
                {
                    param.Close();
                }

                @params = null;
            }

            if (returnType != null)
            {
                returnType.Close();
            }

            returnType = null;
        }
    }
}
