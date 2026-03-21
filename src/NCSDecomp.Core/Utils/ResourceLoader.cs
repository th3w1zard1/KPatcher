// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;
using System.Reflection;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Loads embedded resources with fallback to file system. Uses consistent
    /// naming: NCSDecomp.Core.Resources.&lt;name&gt;.
    /// </summary>
    public static class ResourceLoader
    {
        private const string ResourcePrefix = "NCSDecomp.Core.Resources.";

        /// <summary>
        /// Opens a stream for the given resource: tries manifest first, then fallback path.
        /// </summary>
        /// <param name="resourceName">Short name (e.g. "lexer.dat", "k1_nwscript.nss").</param>
        /// <param name="fallbackPath">Optional file path if embedded resource is missing.</param>
        /// <returns>Stream; caller must dispose.</returns>
        public static Stream OpenResourceOrFile(string resourceName, string fallbackPath = null)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string fullName = ResourcePrefix + resourceName;
            Stream stream = asm.GetManifestResourceStream(fullName);
            if (stream != null)
            {
                return stream;
            }

            if (!string.IsNullOrEmpty(fallbackPath) && File.Exists(fallbackPath))
            {
                return File.OpenRead(fallbackPath);
            }

            throw new FileNotFoundException("Resource or file not found: " + resourceName + (string.IsNullOrEmpty(fallbackPath) ? "" : " or " + fallbackPath));
        }

        public static Stream OpenLexerDat(string fallbackPath = null)
        {
            return OpenResourceOrFile("lexer.dat", fallbackPath);
        }

        public static Stream OpenParserDat(string fallbackPath = null)
        {
            return OpenResourceOrFile("parser.dat", fallbackPath);
        }

        public static Stream OpenK1Nwscript(string fallbackPath = null)
        {
            return OpenResourceOrFile("k1_nwscript.nss", fallbackPath);
        }

        public static Stream OpenTslNwscript(string fallbackPath = null)
        {
            return OpenResourceOrFile("tsl_nwscript.nss", fallbackPath);
        }
    }
}
