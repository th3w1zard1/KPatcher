// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Base class for constant stack entries (DeNCS Const.java).
    /// </summary>
    public abstract class Const : StackEntry
    {
        public static Const NewConst(DecompType type, long intValue)
        {
            if (type.ByteValue() != DecompType.VtInteger)
            {
                throw new InvalidOperationException("Invalid const type for int value: " + type);
            }

            return new IntConst(intValue);
        }

        public static Const NewConst(DecompType type, float floatValue)
        {
            if (type.ByteValue() != DecompType.VtFloat)
            {
                throw new InvalidOperationException("Invalid const type for float value: " + type);
            }

            return new FloatConst(floatValue);
        }

        public static Const NewConst(DecompType type, string stringValue)
        {
            if (type.ByteValue() != DecompType.VtString)
            {
                throw new InvalidOperationException("Invalid const type for string value: " + type);
            }

            return new StringConst(stringValue);
        }

        public static Const NewConst(DecompType type, int objectValue)
        {
            if (type.ByteValue() != DecompType.VtObject)
            {
                throw new InvalidOperationException("Invalid const type for object value: " + type);
            }

            return new ObjectConst(objectValue);
        }

        public override void RemovedFromStack(LocalVarStack stack)
        {
        }

        public override void AddedToStack(LocalVarStack stack)
        {
        }

        public override void DoneParse()
        {
        }

        public override void DoneWithStack(LocalVarStack stack)
        {
        }

        public override string ToString()
        {
            return "";
        }

        public override StackEntry GetElement(int stackpos)
        {
            if (stackpos != 1)
            {
                throw new InvalidOperationException("Position > 1 for const, not struct");
            }

            return this;
        }
    }
}
