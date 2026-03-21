// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Base class for all SableCC AST nodes; provides parent linkage, cloning, and helpers.
    /// </summary>
    public abstract class Node : Switchable, ICloneable
    {
        private Node _parent;

        public Node Parent()
        {
            return _parent;
        }

        internal void SetParent(Node parent)
        {
            _parent = parent;
        }

        internal abstract void RemoveChild(Node child);
        internal abstract void ReplaceChild(Node oldChild, Node newChild);

        public abstract void Apply(Switch sw);

        public void ReplaceBy(Node node)
        {
            if (_parent != null)
            {
                _parent.ReplaceChild(this, node);
            }
        }

        protected static string ToString(Node node)
        {
            return node != null ? node.ToString() : "";
        }

        protected static string ToStringList(IList<object> list)
        {
            if (list == null)
            {
                return "";
            }

            var s = new System.Text.StringBuilder();
            foreach (object o in list)
            {
                s.Append(o);
            }

            return s.ToString();
        }

        protected static string ToStringEnumerable(System.Collections.IEnumerable sequence)
        {
            if (sequence == null)
            {
                return "";
            }

            var s = new System.Text.StringBuilder();
            foreach (object o in sequence)
            {
                s.Append(o);
            }

            return s.ToString();
        }

        protected static Node CloneNode(Node node)
        {
            return node != null ? (Node)node.Clone() : null;
        }

        protected static List<T> CloneList<T>(List<T> list) where T : Node
        {
            var clone = new List<T>();
            foreach (T n in list)
            {
                clone.Add((T)n.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Clone a <see cref="TypedLinkedList{T}"/> preserving its cast hook (DeNCS / SableCC).
        /// </summary>
        protected static TypedLinkedList<T> CloneTypedList<T>(TypedLinkedList<T> list) where T : Node
        {
            var clone = new TypedLinkedList<T>(list.GetCast());
            foreach (T n in list)
            {
                clone.AddLast((T)n.Clone());
            }

            return clone;
        }

        public abstract object Clone();
    }
}
