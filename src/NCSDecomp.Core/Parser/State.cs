// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Parser
{
    /// <summary>
    /// Lightweight stack frame used by the generated parser shift/reduce engine.
    /// </summary>
    internal sealed class State
    {
        internal int state;
        internal AstNode node;

        internal State(int state, AstNode node)
        {
            this.state = state;
            this.node = node;
        }
    }
}
