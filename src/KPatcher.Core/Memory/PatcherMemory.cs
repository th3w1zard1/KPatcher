using System.Collections.Generic;
using System.Linq;

namespace KPatcher.Core.Memory
{

    /// <summary>
    /// Stores memory tokens used during patching
    /// </summary>
    public class PatcherMemory
    {
        /// <summary>
        /// 2DAMemory# (token) -> string value
        /// </summary>
        public Dictionary<int, string> Memory2DA { get; } = new Dictionary<int, string>();

        /// <summary>
        /// StrRef# (token) -> dialog.tlk index
        /// </summary>
        public Dictionary<int, int> MemoryStr { get; } = new Dictionary<int, int>();

        /// <summary>
        /// TSLPatcher GetMemoryToken parity: substitute 2DAMEMORY# keys/values with stored memory strings.
        /// </summary>
        public string ResolveMemoryToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length <= 9 || !value.StartsWith("2DAMEMORY", System.StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            string suffix = value.Substring(9);
            if (suffix.Length == 0 || !suffix.All(char.IsDigit))
            {
                return value;
            }

            int tokenId = int.Parse(suffix);
            if (Memory2DA.TryGetValue(tokenId, out string resolved))
            {
                return resolved;
            }

            return value;
        }

        public override string ToString()
        {
            return $"PatcherMemory(memory_2da={Memory2DA.Count} items, memory_str={MemoryStr.Count} items)";
        }
    }
}

