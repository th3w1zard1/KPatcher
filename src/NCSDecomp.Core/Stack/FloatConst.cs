// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    public sealed class FloatConst : Const
    {
        private readonly float value;

        public FloatConst(float value)
        {
            type = new DecompType(DecompType.VtFloat);
            this.value = value;
            size = 1;
        }

        public float Value()
        {
            return value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
