// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Base element on the decompiler logical stacks (DeNCS StackEntry.java).
    /// </summary>
    public abstract class StackEntry
    {
        protected DecompType type;
        protected int size;

        public DecompType Type()
        {
            return type;
        }

        public int Size()
        {
            return size;
        }

        public abstract void RemovedFromStack(LocalVarStack stack);

        public abstract void AddedToStack(LocalVarStack stack);

        public abstract override string ToString();

        public abstract StackEntry GetElement(int pos);

        public virtual void Close()
        {
            if (type != null)
            {
                type.Close();
            }

            type = null;
        }

        public abstract void DoneParse();

        public abstract void DoneWithStack(LocalVarStack stack);
    }
}
