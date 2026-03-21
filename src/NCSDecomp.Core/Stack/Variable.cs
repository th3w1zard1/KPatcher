// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Variable or struct element on the logical variable stack (DeNCS Variable.java).
    /// </summary>
    public class Variable : StackEntry, IComparable<Variable>
    {
        protected const byte FcnNormal = 0;
        protected const byte FcnReturn = 1;
        protected const byte FcnParam = 2;

        private Dictionary<LocalVarStack, int> stackcounts;
        protected string name;
        protected bool assigned;
        protected VarStruct varstruct;
        protected byte function;

        public Variable(DecompType type)
        {
            this.type = type;
            varstruct = null;
            assigned = false;
            size = 1;
            function = 0;
            stackcounts = new Dictionary<LocalVarStack, int>();
        }

        public Variable(byte type)
            : this(new DecompType(type))
        {
        }

        public override void Close()
        {
            base.Close();
            stackcounts = null;
            varstruct = null;
        }

        public override void DoneParse()
        {
            stackcounts = null;
        }

        public override void DoneWithStack(LocalVarStack stack)
        {
            if (stackcounts != null)
            {
                stackcounts.Remove(stack);
            }
        }

        public void IsReturn(bool isreturn)
        {
            function = isreturn ? FcnReturn : FcnNormal;
        }

        public void IsParam(bool isparam)
        {
            function = isparam ? FcnParam : FcnNormal;
        }

        public bool IsReturn()
        {
            return function == FcnReturn;
        }

        public bool IsParam()
        {
            return function == FcnParam;
        }

        public virtual void Assigned()
        {
            assigned = true;
        }

        public bool IsAssigned()
        {
            return assigned;
        }

        public bool IsStruct()
        {
            return varstruct != null;
        }

        public void Varstruct(VarStruct vs)
        {
            varstruct = vs;
        }

        public VarStruct Varstruct()
        {
            return varstruct;
        }

        public override void AddedToStack(LocalVarStack stack)
        {
            if (stackcounts == null)
            {
                return;
            }

            if (!stackcounts.TryGetValue(stack, out int count))
            {
                stackcounts[stack] = 1;
            }
            else
            {
                stackcounts[stack] = count + 1;
            }
        }

        public override void RemovedFromStack(LocalVarStack stack)
        {
            if (stackcounts == null)
            {
                return;
            }

            if (!stackcounts.TryGetValue(stack, out int count))
            {
                return;
            }

            if (count != 0)
            {
                stackcounts[stack] = count - 1;
            }
            else
            {
                stackcounts.Remove(stack);
            }
        }

        public bool IsPlaceholder(LocalVarStack stack)
        {
            if (stackcounts == null || !stackcounts.TryGetValue(stack, out int count))
            {
                return true;
            }

            return count == 0 && !assigned;
        }

        public bool IsOnStack(LocalVarStack stack)
        {
            return stackcounts != null && stackcounts.TryGetValue(stack, out int count) && count > 0;
        }

        public virtual void Name(string prefix, byte hint)
        {
            name = prefix + type + hint;
        }

        public void Name(string infix, int hint)
        {
            name = type + infix + hint;
        }

        public void Name(string n)
        {
            name = n;
        }

        public override StackEntry GetElement(int stackpos)
        {
            if (stackpos != 1)
            {
                throw new InvalidOperationException("Position > 1 for var, not struct");
            }

            return this;
        }

        public string ToDebugString()
        {
            return "type: " + type + " name: " + name + " assigned: " + assigned;
        }

        public override string ToString()
        {
            if (varstruct != null)
            {
                varstruct.UpdateNames();
                return varstruct.Name() + "." + name;
            }

            return name;
        }

        public virtual string ToDeclString()
        {
            return type + " " + name;
        }

        public int CompareTo(Variable o)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            if (ReferenceEquals(this, o))
            {
                return 0;
            }

            if (name == null)
            {
                return -1;
            }

            if (o.name == null)
            {
                return 1;
            }

            return string.CompareOrdinal(name, o.name);
        }

        public void StackWasCloned(LocalVarStack oldstack, LocalVarStack newstack)
        {
            if (stackcounts != null && stackcounts.TryGetValue(oldstack, out int count) && count > 0)
            {
                stackcounts[newstack] = count;
            }
        }
    }
}
