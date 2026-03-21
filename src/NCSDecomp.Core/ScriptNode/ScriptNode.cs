// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.ScriptNode
{
    public abstract class ScriptNode
    {
        private ScriptNode parent;
        protected string tabs = "";
        protected string newline = Environment.NewLine;

        public ScriptNode Parent()
        {
            return parent;
        }

        public virtual void Parent(ScriptNode value)
        {
            parent = value;
            if (value != null)
            {
                tabs = value.tabs + "\t";
            }
        }

        public virtual void Close()
        {
            parent = null;
        }
    }
}
