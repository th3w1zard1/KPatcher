// Copyright 2021-2025 NCSDecomp (DeNCS)
// Port to C# for KPatcher. Facade for NCS decompilation.
// Full NSS code generation requires parser/analysis/codegen (future work); this exposes the decoder token stream.

using System;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Formats.NCS.Decompiler
{
    /// <summary>
    /// Public API for NCS decompilation. Port of DeNCS FileDecompiler decode pipeline.
    /// Decodes NCS bytecode to a tokenized command string (consumable by the DeNCS parser).
    /// Full NSS source emission requires the full Java pipeline (parser, analysis, MainPass, codegen) to be ported.
    /// </summary>
    public static class NCSDecompiler
    {
        /// <summary>
        /// Decompile NCS bytes to the tokenized command string (decoder output).
        /// Supports both 8-byte header (DeNCS) and 13-byte header (KPatcher/reone) NCS format.
        /// </summary>
        /// <param name="ncsBytes">Raw NCS file bytes.</param>
        /// <param name="actions">Optional actions table for ACTION opcode metadata; not required for decoder output.</param>
        /// <returns>Token string (e.g. "CPDOWNSP 13 1 -4 4 ; MOVSP 19 0 ; ...").</returns>
        public static string Decompile(byte[] ncsBytes, IActionsData actions = null)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            var decoder = new Decoder(ncsBytes);
            return decoder.Decode(actions);
        }

        /// <summary>
        /// Decompile an in-memory NCS to the tokenized command string. Serializes the NCS to bytes using
        /// KPatcher's NCSBinaryWriter (13-byte header + opcode+qualifier per instruction) then runs the decoder.
        /// </summary>
        public static string Decompile(NCS ncs, IActionsData actions = null)
        {
            if (ncs == null)
                throw new ArgumentNullException(nameof(ncs));
            byte[] bytes = new NCSBinaryWriter(ncs).Write();
            return Decompile(bytes, actions);
        }
    }
}
