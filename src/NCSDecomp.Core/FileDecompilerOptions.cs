// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core
{
    /// <summary>
    /// Global options mirroring DeNCS <c>FileDecompiler</c> static fields (for MainPass / future FileDecompiler port).
    /// NCSDecomp.NET stays self-contained: no external compiler paths are used by KPatcher tooling here.
    /// </summary>
    public static class FileDecompilerOptions
    {
        public static bool IsK2Selected { get; set; }

        public static bool PreferSwitches { get; set; }

        public static bool StrictSignatures { get; set; }
    }
}
