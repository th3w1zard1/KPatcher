// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Cast implementation that preserves parented <see cref="Node"/> instances (no reparenting).
    /// </summary>
    public sealed class NodeCast : ICast<Node>
    {
        public static readonly NodeCast Instance = new NodeCast();

        private NodeCast()
        {
        }

        public Node CastObject(object o)
        {
            if (!(o is Node))
            {
                throw new InvalidCastException("Expected Node but got: " + (o != null ? o.GetType().FullName : "null"));
            }

            return (Node)o;
        }
    }
}
