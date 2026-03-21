// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    /// <summary>
    /// Expression fragment in emitted NSS (DeNCS AExpression.java).
    /// Parent chain is on <see cref="ScriptNode"/>.
    /// </summary>
    public interface IAExpression
    {
        ScriptNode Parent();

        void Parent(ScriptNode parent);

        StackEntry Stackentry();

        void Stackentry(StackEntry e);
    }
}
