// Copyright (c) 2021-2025 DeNCS contributors (DeNCS)
// Port to C# for KPatcher. Facade for NCS decompilation.
// Decoder token stream (KCompiler.Core). Full NSS: see NCSManagedDecompiler in this assembly.
// Licensed under the MIT License (see NOTICE and licenses/DeNCS-MIT.txt).

using System;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Formats.NCS.Decompiler
{
    /// <summary>
    /// Decoder facade: NCS bytecode → tokenized command string (DeNCS lexer/parser input).
    /// Full managed NSS: <see cref="NCSManagedDecompiler"/> (NCSDecomp.Core pipeline).
    /// Parse tree only: <c>NCSDecomp.Core.NcsParsePipeline</c>.
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
