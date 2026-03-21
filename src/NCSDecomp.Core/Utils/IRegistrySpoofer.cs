// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// DeNCS compatibility hook: Java used <c>RegistrySpoofer</c> for external <c>nwnnsscomp</c>. KPatcher product code always uses <see cref="NoOpRegistrySpoofer"/>.
    /// Managed decompilation does not use this; provided for future optional external-compiler integration.
    /// </summary>
    public interface IRegistrySpoofer : IDisposable
    {
        /// <summary>Apply spoof (no-op for <see cref="NoOpRegistrySpoofer"/>).</summary>
        IRegistrySpoofer Activate();
    }
}
