// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Stack of <see cref="StackEntry"/> values (DeNCS LocalVarStack.java).
    /// </summary>
    public class LocalVarStack : LocalStack<StackEntry>
    {
        private int placeholderCounter;

        public override void Close()
        {
            if (stack != null)
            {
                foreach (StackEntry e in stack)
                {
                    e.Close();
                }
            }

            base.Close();
        }

        public void DoneParse()
        {
            if (stack != null)
            {
                foreach (StackEntry e in stack)
                {
                    e.DoneParse();
                }
            }

            stack = null;
        }

        public void DoneWithStack()
        {
            if (stack != null)
            {
                foreach (StackEntry e in stack)
                {
                    e.DoneWithStack(this);
                }
            }

            stack = null;
        }

        public new int Size()
        {
            int s = 0;
            if (stack == null)
            {
                return 0;
            }

            foreach (StackEntry e in stack)
            {
                s += e.Size();
            }

            return s;
        }

        public void Push(StackEntry entry)
        {
            stack.AddFirst(entry);
            entry.AddedToStack(this);
        }

        public StackEntry Get(int offset)
        {
            int pos = 0;
            foreach (StackEntry entry in stack)
            {
                pos += entry.Size();
                if (pos > offset)
                {
                    return entry.GetElement(pos - offset + 1);
                }

                if (pos == offset)
                {
                    return entry.GetElement(1);
                }
            }

            while (pos < offset)
            {
                Variable placeholder = NewPlaceholderVariable();
                stack.AddLast(placeholder);
                placeholder.AddedToStack(this);
                pos += placeholder.Size();
            }

            return stack.Last.Value;
        }

        public DecompType GetType(int offset)
        {
            return Get(offset).Type();
        }

        public StackEntry Remove()
        {
            if (stack == null || stack.Count == 0)
            {
                return NewPlaceholderVariable();
            }

            StackEntry entry = stack.First.Value;
            stack.RemoveFirst();
            entry.RemovedFromStack(this);
            return entry;
        }

        public void Destruct(int removesize, int savestart, int savesize, SubroutineAnalysisData subdata)
        {
            Structify(1, removesize, subdata);
            if (savesize > 1)
            {
                Structify(removesize - (savestart + savesize) + 1, savesize, subdata);
            }

            if (stack == null || stack.Count == 0)
            {
                throw new InvalidOperationException("Stack is empty in destruct()");
            }

            StackEntry firstEntry = stack.First.Value;
            Variable structVar = firstEntry as Variable;
            if (structVar == null)
            {
                throw new InvalidOperationException("Expected Variable but got: " + firstEntry.GetType().Name);
            }

            StackEntry elementEntry = structVar.GetElement(removesize - (savestart + savesize) + 1);
            Variable element = elementEntry as Variable;
            if (element == null)
            {
                throw new InvalidOperationException("Expected Variable but got: " + elementEntry.GetType().Name);
            }

            stack.RemoveFirst();
            stack.AddFirst(element);
        }

        public VarStruct Structify(int firstelement, int count, SubroutineAnalysisData subdata)
        {
            var list = new List<StackEntry>();
            foreach (StackEntry e in stack)
            {
                list.Add(e);
            }

            int pos = 0;
            for (int i = 0; i < list.Count; i++)
            {
                StackEntry entry = list[i];
                pos += entry.Size();
                if (pos == firstelement)
                {
                    Variable v = entry as Variable;
                    if (v == null)
                    {
                        throw new InvalidOperationException("Expected Variable but got: " + entry.GetType().Name);
                    }

                    var varstruct = new VarStruct();
                    varstruct.AddVarStackOrder(v);
                    list[i] = varstruct;
                    int j = i + 1;
                    entry = j < list.Count ? list[j] : null;
                    for (int var8 = entry != null ? pos + entry.Size() : pos;
                         entry != null && var8 <= firstelement + count - 1;
                         var8 += entry.Size())
                    {
                        Variable v2 = entry as Variable;
                        if (v2 == null)
                        {
                            throw new InvalidOperationException("Expected Variable but got: " + entry.GetType().Name);
                        }

                        list.RemoveAt(j);
                        varstruct.AddVarStackOrder(v2);
                        entry = j < list.Count ? list[j] : null;
                    }

                    RebuildStackFromList(list);
                    subdata.AddStruct(varstruct);
                    return varstruct;
                }

                if (pos == firstelement + count - 1)
                {
                    VarStruct vs = entry as VarStruct;
                    if (vs == null)
                    {
                        throw new InvalidOperationException("Expected VarStruct but got: " + entry.GetType().Name);
                    }

                    return vs;
                }

                if (pos > firstelement + count - 1)
                {
                    VarStruct vs = entry as VarStruct;
                    if (vs == null)
                    {
                        throw new InvalidOperationException("Expected VarStruct but got: " + entry.GetType().Name);
                    }

                    return vs.Structify(firstelement - (pos - entry.Size()), count, subdata);
                }
            }

            return null;
        }

        private void RebuildStackFromList(List<StackEntry> list)
        {
            stack.Clear();
            for (int k = list.Count - 1; k >= 0; k--)
            {
                stack.AddFirst(list[k]);
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            int max = stack != null ? stack.Count : 0;
            buffer.Append("---stack, size ").Append(max).Append("---\r\n");
            int i = 0;
            foreach (StackEntry entry in stack)
            {
                buffer.Append("-->").Append(i).Append(entry).Append("\r\n");
                i++;
            }

            return buffer.ToString();
        }

        public new LocalVarStack CloneStack()
        {
            var newStack = new LocalVarStack();
            newStack.stack = new LinkedList<StackEntry>(stack);
            newStack.placeholderCounter = placeholderCounter;
            foreach (StackEntry entry in newStack.stack)
            {
                Variable v = entry as Variable;
                v?.StackWasCloned(this, newStack);
            }

            return newStack;
        }

        private Variable NewPlaceholderVariable()
        {
            var placeholder = new Variable(new DecompType(DecompType.VtInvalid));
            placeholder.Name("__unknown_param_" + ++placeholderCounter);
            placeholder.IsParam(true);
            return placeholder;
        }
    }
}
