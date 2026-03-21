// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// No-op registry spoofer (DeNCS NoOpRegistrySpoofer.java).
    /// </summary>
    public sealed class NoOpRegistrySpoofer : IRegistrySpoofer
    {
        public IRegistrySpoofer Activate()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
