// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Decompilation or IO failures that should be shown to the user (DeNCS DecompilerException.java).
    /// </summary>
    public class DecompilerException : Exception
    {
        public DecompilerException(string message)
            : base(message)
        {
        }

        public DecompilerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
