// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    public class AErrorComment : ScriptNode
    {
        private readonly string message;

        public AErrorComment(string message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return tabs + "/* " + message + " */" + newline;
        }
    }
}
