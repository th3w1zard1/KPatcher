// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    public sealed class StringConst : Const
    {
        private readonly string value;

        public StringConst(string value)
        {
            type = new DecompType(DecompType.VtString);
            this.value = value;
            size = 1;
        }

        public string Value()
        {
            return value;
        }

        public override string ToString()
        {
            return value;
        }
    }
}
