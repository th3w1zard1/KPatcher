// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Hook used by <see cref="TypedLinkedList{T}"/> to enforce parent/ownership rules on insert.
    /// </summary>
    public interface ICast<T>
    {
        T CastObject(object o);
    }
}
