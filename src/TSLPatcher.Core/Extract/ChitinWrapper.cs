using System.Collections.Generic;
using System.Linq;
using TSLPatcher.Core.Formats.Chitin;
using TSLPatcher.Core.Resources;

namespace TSLPatcher.Core.Extract
{
    // Thin wrapper matching PyKotor extract.chitin.Chitin semantics (read-only).
    public class ChitinWrapper
    {
        private readonly Chitin _chitin;

        public ChitinWrapper(string keyPath, string basePath = null)
        {
            _chitin = new Chitin(keyPath, basePath);
        }

        public List<FileResource> Resources => _chitin.ToList();
    }
}

