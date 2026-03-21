// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    public sealed class ObjectConst : Const
    {
        private readonly int value;

        public ObjectConst(int value)
        {
            type = new DecompType(DecompType.VtObject);
            this.value = value;
            size = 1;
        }

        public int Value()
        {
            return value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
