// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS KnownExternalCompilers.java — SHA256 -> nwnnsscomp / ncsdis argument templates.

using System;
using System.Collections.Generic;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Metadata for a known external NSS compiler or ncsdis (fingerprints + CLI templates).
    /// </summary>
    public sealed class KnownExternalCompiler
    {
        private readonly string[] compileArgs;
        private readonly string[] decompileArgs;

        internal KnownExternalCompiler(
            string sha256,
            string name,
            DateTime releaseDate,
            string author,
            string[] compileArgsTemplate,
            string[] decompileArgsTemplate)
        {
            Sha256 = sha256 ?? string.Empty;
            Name = name ?? string.Empty;
            ReleaseDate = releaseDate;
            Author = author ?? string.Empty;
            compileArgs = compileArgsTemplate ?? Array.Empty<string>();
            decompileArgs = decompileArgsTemplate ?? Array.Empty<string>();
        }

        /// <summary>Uppercase SHA256 fingerprint; empty when unknown / not keyed in lookup.</summary>
        public string Sha256 { get; }

        public string Name { get; }

        public DateTime ReleaseDate { get; }

        public string Author { get; }

        public bool SupportsDecompilation
        {
            get { return decompileArgs != null && decompileArgs.Length > 0; }
        }

        public string[] GetCompileArgs()
        {
            return (string[])compileArgs.Clone();
        }

        public string[] GetDecompileArgs()
        {
            return (string[])decompileArgs.Clone();
        }

        internal bool IsNcsdis
        {
            get { return string.Equals(Name, "ncsdis", StringComparison.OrdinalIgnoreCase); }
        }
    }

    /// <summary>
    /// Registry of known compilers (DeNCS <c>KnownExternalCompilers</c> enum).
    /// </summary>
    public static class KnownExternalCompilers
    {
        private static readonly Dictionary<string, KnownExternalCompiler> ByHash =
            new Dictionary<string, KnownExternalCompiler>(StringComparer.OrdinalIgnoreCase);

        /// <summary>KOTOR Tool nwnnsscomp.</summary>
        public static readonly KnownExternalCompiler KotorTool = Register(
            new KnownExternalCompiler(
                "E36AA3172173B654AE20379888EDDC9CF45C62FBEB7AB05061C57B52961C824D",
                "KOTOR Tool",
                new DateTime(2005, 1, 1),
                "Fred Tetra",
                new[] { "-c", "--outputdir", "{output_dir}", "-o", "{output_name}", "-g", "{game_value}", "{includes}", "{source}" },
                new[] { "-d", "--outputdir", "{output_dir}", "-o", "{output_name}", "-g", "{game_value}", "{source}" }));

        /// <summary>KOTOR Scripting Tool nwnnsscomp.</summary>
        public static readonly KnownExternalCompiler KotorScriptingTool = Register(
            new KnownExternalCompiler(
                "B7344408A47BE8780816CF68F5A171A09640AB47AD1A905B7F87DE30A50A0A92",
                "KOTOR Scripting Tool",
                new DateTime(2016, 5, 18),
                "James Goad",
                new[] { "-c", "--outputdir", "{output_dir}", "-o", "{output_name}", "-g", "{game_value}", "{includes}", "{source}" },
                new[] { "-d", "--outputdir", "{output_dir}", "-o", "{output_name}", "-g", "{game_value}", "{source}" }));

        /// <summary>Xoreos Tools (no fixed SHA in DeNCS; not in hash map).</summary>
        public static readonly KnownExternalCompiler Xoreos = new KnownExternalCompiler(
            string.Empty,
            "Xoreos Tools",
            new DateTime(2016, 1, 1),
            "DrMcFly",
            Array.Empty<string>(),
            Array.Empty<string>());

        /// <summary>knsscomp (hash TBD in Java).</summary>
        public static readonly KnownExternalCompiler Knsscomp = new KnownExternalCompiler(
            string.Empty,
            "knsscomp",
            new DateTime(2022, 1, 1),
            "Nick Hugi",
            new[] { "-c", "{source}", "-o", "{output}" },
            Array.Empty<string>());

        /// <summary>ncsdis.exe</summary>
        public static readonly KnownExternalCompiler Ncsdis = Register(
            new KnownExternalCompiler(
                "B1F398C2F64F4ACF2F39C417E7C7EB6F5483369BB95853C63A009F925A2E257C",
                "ncsdis",
                new DateTime(2020, 8, 3),
                "Unknown",
                Array.Empty<string>(),
                new[] { "{source}", "{output}" }));

        private static KnownExternalCompiler Register(KnownExternalCompiler c)
        {
            if (!string.IsNullOrEmpty(c.Sha256))
            {
                ByHash[c.Sha256.ToUpperInvariant()] = c;
            }

            return c;
        }

        /// <summary>Resolve by SHA256 (case-insensitive). Null if unknown or empty.</summary>
        public static KnownExternalCompiler FromSha256(string sha256)
        {
            if (string.IsNullOrEmpty(sha256))
            {
                return null;
            }

            KnownExternalCompiler c;
            if (ByHash.TryGetValue(sha256.Trim().ToUpperInvariant(), out c))
            {
                return c;
            }

            return null;
        }
    }
}
