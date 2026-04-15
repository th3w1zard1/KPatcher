// Copyright (c) 2021-2025 DeNCS contributors (DeNCS) / KPatcher
// DeNCS-derived portions: MIT License (see licenses/DeNCS-MIT.txt).
// KPatcher: LGPL as per project root.

using System;
using System.IO;
using NCSDecomp.Core;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Formats.NCS.Decompiler
{
    /// <summary>
    /// Managed full NCS->NSS decompilation (DeNCS port in <c>NCSDecomp.Core</c>).
    /// Requires embedded <c>k1_nwscript.nss</c> / <c>tsl_nwscript.nss</c> in the NCSDecomp.Core assembly
    /// (see <c>src/NCSDecomp.Core/Resources/</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="NCSDecompiler"/> remains the lightweight decoder token-string API (built into <c>KCompiler.Core</c>).
    /// This type lives in <c>KPatcher.Core</c> so callers reference one assembly without a project cycle.
    /// </remarks>
    public static class NCSManagedDecompiler
    {
        /// <summary>
        /// Decompile NCS bytes to NSS using the embedded nwscript table for the given game.
        /// </summary>
        /// <param name="ncsBytes">Raw .ncs file bytes.</param>
        /// <param name="tsl">True for KotOR 2 (TSL) action table, false for KotOR 1.</param>
        /// <exception cref="FileNotFoundException">Embedded nwscript resource is missing from NCSDecomp.Core.</exception>
        public static string DecompileToNss(byte[] ncsBytes, bool tsl)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            ActionsData actions = ActionsData.LoadFromEmbedded(tsl);
            FileDecompilerOptions.IsK2Selected = tsl;
            return new FileDecompiler(actions).DecompileToNss(ncsBytes);
        }

        /// <summary>
        /// Decompile using a caller-supplied action table (e.g. custom nwscript NSS text).
        /// </summary>
        public static string DecompileToNss(byte[] ncsBytes, ActionsData actions)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));
            return new FileDecompiler(actions).DecompileToNss(ncsBytes);
        }

        /// <summary>
        /// Serialize <paramref name="ncs"/> and run the full managed decompiler.
        /// </summary>
        public static string DecompileToNss(NCS ncs, bool tsl)
        {
            if (ncs == null)
                throw new ArgumentNullException(nameof(ncs));
            byte[] bytes = new NCSBinaryWriter(ncs).Write();
            return DecompileToNss(bytes, tsl);
        }
    }
}
