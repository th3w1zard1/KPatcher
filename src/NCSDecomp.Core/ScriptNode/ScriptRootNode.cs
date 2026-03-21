// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;

namespace NCSDecomp.Core.ScriptNode
{
    /// <summary>
    /// Script-level container with ordered children (DeNCS ScriptRootNode.java).
    /// </summary>
    public abstract class ScriptRootNode : ScriptNode
    {
        protected LinkedList<ScriptNode> children = new LinkedList<ScriptNode>();
        protected int start;
        protected int end;

        protected ScriptRootNode(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public void AddChild(ScriptNode child)
        {
            children.AddLast(child);
            child.Parent(this);
        }

        public void AddChildren(IEnumerable<ScriptNode> toAdd)
        {
            foreach (ScriptNode c in toAdd)
            {
                AddChild(c);
            }
        }

        public List<ScriptNode> RemoveChildren(int first, int last)
        {
            var removed = new List<ScriptNode>(last - first + 1);
            for (int i = 0; i <= last - first; i++)
            {
                removed.Add(RemoveChild(first));
            }

            return removed;
        }

        public List<ScriptNode> RemoveChildren(int first)
        {
            return RemoveChildren(first, children.Count - 1);
        }

        public List<ScriptNode> RemoveChildren()
        {
            return RemoveChildren(0, children.Count - 1);
        }

        public ScriptNode RemoveLastChild()
        {
            ScriptNode child = children.Last.Value;
            children.RemoveLast();
            child.Parent(null);
            return child;
        }

        public void RemoveChild(ScriptNode child)
        {
            children.Remove(child);
            child.Parent(null);
        }

        public ScriptNode RemoveChild(int index)
        {
            ScriptNode node = children.NodeAt(index).Value;
            children.Remove(node);
            node.Parent(null);
            return node;
        }

        public ScriptNode GetLastChild()
        {
            return children.Last.Value;
        }

        public ScriptNode GetPreviousChild(int pos)
        {
            return children.Count < pos ? null : children.NodeFromEnd(pos).Value;
        }

        public bool HasChildren()
        {
            return children.Count > 0;
        }

        public int GetEnd()
        {
            return end;
        }

        public int GetStart()
        {
            return start;
        }

        public LinkedList<ScriptNode> GetChildren()
        {
            return children;
        }

        public int GetChildLocation(ScriptNode child)
        {
            int i = 0;
            foreach (ScriptNode n in children)
            {
                if (ReferenceEquals(n, child))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public void ReplaceChild(ScriptNode oldchild, ScriptNode newchild)
        {
            LinkedListNode<ScriptNode> node = children.Find(oldchild);
            if (node == null)
            {
                throw new System.InvalidOperationException("Child not found");
            }

            node.Value = newchild;
            newchild.Parent(this);
            oldchild.Parent(null);
        }

        public override string ToString()
        {
            var buff = new StringBuilder();
            foreach (ScriptNode c in children)
            {
                buff.Append(c.ToString());
            }

            return buff.ToString();
        }

        public int Size()
        {
            return children.Count;
        }

        public ScriptNode GetChild(int index)
        {
            return children.NodeAt(index).Value;
        }

        public void End(int endPos)
        {
            end = endPos;
        }

        public override void Close()
        {
            base.Close();
            if (children != null)
            {
                foreach (ScriptNode c in children)
                {
                    c.Close();
                }

                children = null;
            }
        }
    }

    internal static class LinkedListExtensions
    {
        public static LinkedListNode<ScriptNode> NodeAt(this LinkedList<ScriptNode> list, int index)
        {
            LinkedListNode<ScriptNode> n = list.First;
            for (int i = 0; i < index && n != null; i++)
            {
                n = n.Next;
            }

            return n;
        }

        public static LinkedListNode<ScriptNode> NodeFromEnd(this LinkedList<ScriptNode> list, int pos)
        {
            LinkedListNode<ScriptNode> n = list.Last;
            for (int i = 1; i < pos && n != null; i++)
            {
                n = n.Previous;
            }

            return n;
        }
    }
}
