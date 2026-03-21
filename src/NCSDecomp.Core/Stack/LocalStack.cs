// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Lightweight generic stack with clone support for analysis passes (DeNCS LocalStack.java).
    /// </summary>
    public class LocalStack<T>
    {
        protected LinkedList<T> stack = new LinkedList<T>();

        public int Size()
        {
            return stack.Count;
        }

        public virtual LocalStack<T> CloneStack()
        {
            var newStack = new LocalStack<T>();
            newStack.stack = new LinkedList<T>(stack);
            return newStack;
        }

        public virtual void Close()
        {
            stack = null;
        }
    }
}
