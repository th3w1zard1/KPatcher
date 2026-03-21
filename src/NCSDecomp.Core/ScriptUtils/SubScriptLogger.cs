// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Diagnostics;

namespace NCSDecomp.Core.ScriptUtils
{
    internal static class SubScriptLogger
    {
        public static void Trace(string message)
        {
            Debug.WriteLine("[SubScriptState] " + message);
        }
    }
}
