// Copyright (c) 2021-2025 DeNCS contributors (DeNCS)
// Port to C# for KPatcher. Shared contract for Decoder ACTION opcode lookup.
// Licensed under the MIT License (see NOTICE and licenses/DeNCS-MIT.txt).

namespace KPatcher.Core.Formats.NCS.Decompiler
{
    /// <summary>
    /// Minimal interface for ACTION opcode lookup (action index -> string).
    /// Port of DeNCS ActionsData.getAction(int). Implemented by <see cref="Decoder"/> consumers;
    /// full nwscript.nss parsing lives in NCSDecomp.Core <c>ActionsData</c> (MIT).
    /// </summary>
    public interface IActionsData
    {
        string GetAction(int index);
    }
}
