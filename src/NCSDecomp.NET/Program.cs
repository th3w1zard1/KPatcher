// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core;

namespace NCSDecomp.NET
{
    /// <summary>
    /// CLI entry point for NCS decompilation. Port of DeNCS NCSDecompCLI.
    /// Usage: -i &lt;input.ncs&gt; -o &lt;output.nss&gt; -g k1|k2
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args) => NcsDecompCli.Run(args);
    }
}
