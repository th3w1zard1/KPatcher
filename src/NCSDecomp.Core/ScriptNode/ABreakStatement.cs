// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    public class ABreakStatement : ScriptNode
    {
        public override string ToString()
        {
            return tabs + "break;" + newline;
        }
    }
}
