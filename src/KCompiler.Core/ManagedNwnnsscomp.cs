using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;

namespace KCompiler
{
    /// <summary>
    /// Cross-platform managed NSS→NCS compilation (no nwnnsscomp.exe).
    /// Use from KPatcher, tools, or the KCompiler.NET CLI.
    /// </summary>
    public static class ManagedNwnnsscomp
    {
        /// <summary>Compile NSS file to NCS (game: 1 = K1, 2 = TSL).</summary>
        public static void CompileFile(string sourcePath, string outputPath, int gameNumber, bool debug = false, string nwscriptPath = null)
        {
            CompileFile(sourcePath, outputPath, GameNumberToGame(gameNumber), debug, nwscriptPath);
        }

        /// <summary>Compile NSS file to NCS; defaults to KOTOR 1.</summary>
        public static void CompileFile(string sourcePath, string outputPath, bool debug = false, string nwscriptPath = null)
        {
            CompileFile(sourcePath, outputPath, Game.K1, debug, nwscriptPath);
        }

        /// <summary>Compile NSS file to NCS on disk using the same pipeline as KPatcher's inbuilt compiler.</summary>
        public static void CompileFile(
            string sourcePath,
            string outputPath,
            Game game,
            bool debug = false,
            string nwscriptPath = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Source path is required.", nameof(sourcePath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("NSS source not found.", sourcePath);
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            byte[] nssData = File.ReadAllBytes(sourcePath);
            string nssContents = DecodeBytesWithFallbacks(nssData);
            string parentDir = Path.GetDirectoryName(sourcePath);
            var libraryLookup = parentDir != null ? new List<string> { parentDir } : new List<string>();
            NCS ncs = NCSAuto.CompileNss(
                nssContents,
                game,
                library: null,
                optimizers: null,
                libraryLookup: libraryLookup,
                errorlog: null,
                debug: debug,
                nwscriptPath: nwscriptPath);
            string outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            NCSAuto.WriteNcs(ncs, outputPath);
        }

        /// <summary>Compile raw NSS source to NCS bytes (game: 1 = K1, 2 = TSL).</summary>
        public static byte[] CompileSourceToBytes(
            string nssContents,
            int gameNumber,
            IReadOnlyList<string> libraryLookupPaths = null,
            bool debug = false,
            string nwscriptPath = null)
        {
            return CompileSourceToBytes(nssContents, GameNumberToGame(gameNumber), libraryLookupPaths, debug, nwscriptPath);
        }

        /// <summary>Compile raw NSS source to NCS bytes.</summary>
        public static byte[] CompileSourceToBytes(
            string nssContents,
            Game game,
            IReadOnlyList<string> libraryLookupPaths = null,
            bool debug = false,
            string nwscriptPath = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var lookup = libraryLookupPaths != null
                ? new List<string>(libraryLookupPaths)
                : new List<string>();
            NCS ncs = NCSAuto.CompileNss(
                nssContents,
                game,
                library: null,
                optimizers: null,
                libraryLookup: lookup,
                errorlog: null,
                debug: debug,
                nwscriptPath: nwscriptPath);
            return NCSAuto.BytesNcs(ncs);
        }

        private static Game GameNumberToGame(int gameNumber)
        {
            if (gameNumber == 2)
            {
                return Game.K2;
            }

            return Game.K1;
        }

        private static string DecodeBytesWithFallbacks(byte[] data)
        {
            try
            {
                string result = Encoding.UTF8.GetString(data);
                byte[] reencoded = Encoding.UTF8.GetBytes(result);
                if (reencoded.Length == data.Length)
                {
                    bool same = true;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (reencoded[i] != data[i])
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same)
                    {
                        return result;
                    }
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                return Encoding.GetEncoding("windows-1252").GetString(data);
            }
            catch
            {
                return Encoding.ASCII.GetString(data);
            }
        }
    }
}
