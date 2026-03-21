// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Identity cast; used when no parenting adjustments are needed.
    /// </summary>
    public sealed class NoCast<T> : ICast<T>
    {
        private static readonly NoCast<T> InstanceField = new NoCast<T>();

        private NoCast()
        {
        }

        public static NoCast<T> Instance
        {
            get { return InstanceField; }
        }

        public T CastObject(object o)
        {
            return (T)o;
        }
    }
}
