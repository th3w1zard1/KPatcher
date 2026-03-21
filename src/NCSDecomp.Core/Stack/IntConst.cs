// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    public sealed class IntConst : Const
    {
        private readonly long value;

        public IntConst(long value)
        {
            type = new DecompType(DecompType.VtInteger);
            this.value = value;
            size = 1;
        }

        public long Value()
        {
            return value;
        }

        public override string ToString()
        {
            const long mask = 0xFFFFFFFFL;
            return value == unchecked((long)mask) ? "0xFFFFFFFF" : value.ToString();
        }
    }
}
