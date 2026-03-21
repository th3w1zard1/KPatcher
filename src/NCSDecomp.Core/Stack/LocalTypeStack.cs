// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Stack that tracks only type metadata during prototyping (DeNCS LocalTypeStack.java).
    /// </summary>
    public class LocalTypeStack : LocalStack<DecompType>
    {
        public void Push(DecompType type)
        {
            stack.AddFirst(type);
        }

        public DecompType Get(int offset)
        {
            int pos = 0;
            foreach (DecompType t in stack)
            {
                pos += t.Size();
                if (pos > offset)
                {
                    return t.GetElement(pos - offset + 1);
                }

                if (pos == offset)
                {
                    return t.GetElement(1);
                }
            }

            return new DecompType(DecompType.VtInvalid);
        }

        public DecompType Get(int offset, SubroutineState state)
        {
            int pos = 0;
            foreach (DecompType t in stack)
            {
                pos += t.Size();
                if (pos > offset)
                {
                    return t.GetElement(pos - offset + 1);
                }

                if (pos == offset)
                {
                    return t.GetElement(1);
                }
            }

            if (state.IsPrototyped())
            {
                DecompType typex = state.GetParamType(offset - pos);
                if (!typex.Equals(DecompType.VtNone))
                {
                    return typex;
                }
            }

            return new DecompType(DecompType.VtInvalid);
        }

        public void Remove(int count)
        {
            int actualCount = count < stack.Count ? count : stack.Count;
            for (int i = 0; i < actualCount; i++)
            {
                stack.RemoveFirst();
            }
        }

        public void RemoveParams(int count, SubroutineState state)
        {
            var paramList = new LinkedList<DecompType>();
            for (int i = 0; i < count; i++)
            {
                DecompType t = stack.Count == 0 ? new DecompType(DecompType.VtInvalid) : stack.First.Value;
                if (stack.Count > 0)
                {
                    stack.RemoveFirst();
                }

                paramList.AddFirst(t);
            }

            state.UpdateParams(paramList);
        }

        public int RemovePrototyping(int count)
        {
            int paramCount = 0;
            int i = 0;
            while (i < count)
            {
                if (stack.Count == 0)
                {
                    paramCount++;
                    i++;
                }
                else
                {
                    DecompType t = stack.First.Value;
                    stack.RemoveFirst();
                    i += t.Size();
                }
            }

            return paramCount;
        }

        public void Remove(int start, int removeCount)
        {
            int loc = start - 1;
            for (int i = 0; i < removeCount; i++)
            {
                var node = stack.First;
                for (int j = 0; j < loc && node != null; j++)
                {
                    node = node.Next;
                }

                if (node != null)
                {
                    stack.Remove(node);
                }
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            int max = stack.Count;
            buffer.Append("---stack, size ").Append(max).Append("---\r\n");
            int i = 1;
            foreach (DecompType t in stack)
            {
                buffer.Append("-->").Append(i).Append(" is type ").Append(t).Append("\r\n");
                i++;
            }

            return buffer.ToString();
        }

        public new LocalTypeStack CloneStack()
        {
            var newStack = new LocalTypeStack();
            newStack.stack = new LinkedList<DecompType>(stack);
            return newStack;
        }
    }
}
