// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS RoundTripUtil.java — managed NCS→NSS helpers (Java used FileDecompiler + temp files).

using System;
using System.Globalization;
using System.IO;
using System.Text;
using KCompiler;
using KPatcher.Core.Common;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Result of <see cref="RoundTripUtil.CompareManagedRecompileToOriginalDecoderText"/> (managed KCompiler recompile vs original NCS).
    /// </summary>
    public sealed class ManagedRoundTripCompareResult
    {
        public ManagedRoundTripCompareResult(bool compileSucceeded, bool decoderOutputsMatch, string summary)
        {
            CompileSucceeded = compileSucceeded;
            DecoderOutputsMatch = decoderOutputsMatch;
            Summary = summary ?? string.Empty;
        }

        /// <summary>True if <see cref="KCompiler.ManagedNwnnsscomp.CompileSourceToBytes"/> produced NCS bytes.</summary>
        public bool CompileSucceeded { get; }

        /// <summary>True if decoder token text for original and recompiled NCS are identical (ignores 8 vs 13-byte file header differences).</summary>
        public bool DecoderOutputsMatch { get; }

        /// <summary>Human-readable status for logs or UI.</summary>
        public string Summary { get; }
    }

    /// <summary>
    /// Shared round-trip helpers: decompile NCS to NSS with the same game flag semantics as DeNCS Java / HoloPatcher
    /// (<c>k1</c>, <c>k2</c>, <c>tsl</c>, <c>2</c> for TSL). Matches responsibilities of BioWare.NET and KOTORModSync
    /// <c>RoundTripUtil</c> (path-based decompile), plus <see cref="CompareManagedRecompileToOriginalDecoderText"/> for
    /// managed KCompiler recompile vs decoder token stream (no external <c>nwnnsscomp</c>).
    /// </summary>
    public static class RoundTripUtil
    {
        /// <summary>
        /// Decompile <paramref name="ncsFilePath"/> to NSS source. <paramref name="gameFlag"/> is <c>k1</c>, <c>k2</c>, <c>tsl</c>, or <c>2</c>.
        /// </summary>
        /// <returns>Null if the NCS file is missing; otherwise decompiled text.</returns>
        /// <exception cref="DecompilerException">Actions load or decompile failure.</exception>
        public static string DecompileNcsToNss(string ncsFilePath, string gameFlag, string k1NwscriptPath = null, string k2NwscriptPath = null)
        {
            if (string.IsNullOrEmpty(ncsFilePath) || !File.Exists(ncsFilePath))
            {
                return null;
            }

            bool wasK2 = FileDecompilerOptions.IsK2Selected;
            try
            {
                bool k2 = ParseK2(gameFlag);
                FileDecompilerOptions.IsK2Selected = k2;
                ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath);
                var decompiler = new FileDecompiler(actions);
                byte[] bytes = File.ReadAllBytes(ncsFilePath);
                return decompiler.DecompileToNss(bytes);
            }
            catch (FileNotFoundException ex)
            {
                throw new DecompilerException("Failed to load actions data: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new DecompilerException("Failed to decompile: " + ex.Message, ex);
            }
            finally
            {
                FileDecompilerOptions.IsK2Selected = wasK2;
            }
        }

        /// <summary>
        /// Decompile NCS to an NSS file (creates parent directories).
        /// </summary>
        /// <exception cref="DecompilerException">Missing input, failed decompile, or no output written.</exception>
        public static void DecompileNcsToNssFile(
            string ncsFilePath,
            string nssOutputPath,
            string gameFlag,
            Encoding charset,
            string k1NwscriptPath = null,
            string k2NwscriptPath = null)
        {
            if (string.IsNullOrEmpty(ncsFilePath) || !File.Exists(ncsFilePath))
            {
                throw new DecompilerException("NCS file does not exist: " + (ncsFilePath ?? "null"));
            }

            if (charset == null)
            {
                charset = new UTF8Encoding(false);
            }

            bool wasK2 = FileDecompilerOptions.IsK2Selected;
            try
            {
                bool k2 = ParseK2(gameFlag);
                FileDecompilerOptions.IsK2Selected = k2;

                string parent = Path.GetDirectoryName(nssOutputPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath);
                var decompiler = new FileDecompiler(actions);
                byte[] bytes = File.ReadAllBytes(ncsFilePath);
                string nss = decompiler.DecompileToNss(bytes);
                File.WriteAllText(nssOutputPath, nss, charset);

                if (!File.Exists(nssOutputPath))
                {
                    throw new DecompilerException("Decompile did not produce output file: " + nssOutputPath);
                }
            }
            catch (DecompilerException)
            {
                throw;
            }
            catch (FileNotFoundException ex)
            {
                throw new DecompilerException("Failed to load actions data: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new DecompilerException("Failed to decompile file: " + ex.Message, ex);
            }
            finally
            {
                FileDecompilerOptions.IsK2Selected = wasK2;
            }
        }

        /// <summary>
        /// After external compile, the NCS usually sits beside the NSS with the same base name — decompile that NCS.
        /// </summary>
        public static string GetRoundTripDecompiledCode(string savedNssFilePath, string gameFlag, string k1NwscriptPath = null, string k2NwscriptPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(savedNssFilePath) || !File.Exists(savedNssFilePath))
                {
                    return null;
                }

                string dir = Path.GetDirectoryName(savedNssFilePath);
                string baseName = Path.GetFileNameWithoutExtension(savedNssFilePath);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName))
                {
                    return null;
                }

                string recompiledNcs = Path.Combine(dir, baseName + ".ncs");
                if (!File.Exists(recompiledNcs))
                {
                    return null;
                }

                return DecompileNcsToNss(recompiledNcs, gameFlag, k1NwscriptPath, k2NwscriptPath);
            }
            catch (DecompilerException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Overload using paths from <see cref="NcsDecompSettings"/>.</summary>
        public static string DecompileNcsToNss(string ncsFilePath, string gameFlag, NcsDecompSettings settings)
        {
            if (settings == null)
            {
                return DecompileNcsToNss(ncsFilePath, gameFlag);
            }

            return DecompileNcsToNss(ncsFilePath, gameFlag, settings.K1NwscriptPath, settings.K2NwscriptPath);
        }

        /// <summary>
        /// Recompile <paramref name="nss"/> with the managed compiler and compare
        /// <see cref="NcsParsePipeline.DecodeToTokenStream"/> output to that of <paramref name="originalNcs"/>.
        /// File layouts (8- vs 13-byte NCS headers) can differ while decoder text still matches.
        /// </summary>
        /// <remarks>
        /// <paramref name="k1NwscriptPath"/> / <paramref name="k2NwscriptPath"/> are passed to both
        /// <see cref="ActionsData.LoadForGame"/> and <see cref="ManagedNwnnsscomp.CompileSourceToBytes"/>.
        /// BioWare-style <c>k1_nwscript.nss</c> on disk is valid for DeNCS action tables but may not parse as KCompiler
        /// nwscript; use <see langword="null"/> paths here and on-disk nwscript only for
        /// <see cref="DecompileNcsToNss(string,string,string,string)"/> (see <c>NcsDecompNetStyleRoundTripTests</c>).
        /// </remarks>
        public static ManagedRoundTripCompareResult CompareManagedRecompileToOriginalDecoderText(
            byte[] originalNcs,
            string nss,
            bool k2,
            string k1NwscriptPath = null,
            string k2NwscriptPath = null)
        {
            if (originalNcs == null || originalNcs.Length == 0)
            {
                return new ManagedRoundTripCompareResult(false, false, "No original NCS bytes.");
            }

            if (string.IsNullOrEmpty(nss))
            {
                return new ManagedRoundTripCompareResult(false, false, "No NSS text to recompile.");
            }

            ActionsData actions = LoadActions(k2, k1NwscriptPath, k2NwscriptPath);
            Game game = k2 ? Game.K2 : Game.K1;
            string nwscriptPath = k2 ? k2NwscriptPath : k1NwscriptPath;

            byte[] recompiled;
            try
            {
                recompiled = ManagedNwnnsscomp.CompileSourceToBytes(nss, game, null, false, nwscriptPath);
            }
            catch (Exception ex)
            {
                return new ManagedRoundTripCompareResult(false, false, "Managed compile failed: " + ex.Message);
            }

            string decOrig;
            string decRound;
            try
            {
                decOrig = NcsParsePipeline.DecodeToTokenStream(originalNcs, actions);
                decRound = NcsParsePipeline.DecodeToTokenStream(recompiled, actions);
            }
            catch (Exception ex)
            {
                return new ManagedRoundTripCompareResult(true, false, "Decode for compare failed: " + ex.Message);
            }

            if (decOrig == decRound)
            {
                return new ManagedRoundTripCompareResult(true, true, "Decoder token streams match (managed round-trip).");
            }

            return new ManagedRoundTripCompareResult(true, false, BuildDecoderTextDiffSummary(decOrig, decRound));
        }

        private static string BuildDecoderTextDiffSummary(string a, string b)
        {
            if (a == null)
            {
                a = string.Empty;
            }

            if (b == null)
            {
                b = string.Empty;
            }

            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int i = 0;
            for (; i < minLen; i++)
            {
                if (a[i] != b[i])
                {
                    break;
                }
            }

            var sb = new StringBuilder();
            if (i < minLen)
            {
                sb.Append("Decoder outputs differ at char ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                sb.Append(" (lengths ");
                sb.Append(a.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(" vs ");
                sb.Append(b.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append("). ");
                AppendSnippet(sb, a, i);
                sb.Append(" | ");
                AppendSnippet(sb, b, i);
            }
            else if (a.Length != b.Length)
            {
                sb.Append("Decoder outputs match for ");
                sb.Append(minLen.ToString(CultureInfo.InvariantCulture));
                sb.Append(" chars but lengths differ (");
                sb.Append(a.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(" vs ");
                sb.Append(b.Length.ToString(CultureInfo.InvariantCulture));
                sb.Append(").");
            }
            else
            {
                sb.Append("Decoder outputs differ (unexpected).");
            }

            return sb.ToString();
        }

        private static void AppendSnippet(StringBuilder sb, string s, int index)
        {
            int start = index > 48 ? index - 48 : 0;
            int maxLen = 96;
            int take = s.Length - start;
            if (take > maxLen)
            {
                take = maxLen;
            }

            string part = s.Substring(start, take).Replace("\r", "\\r").Replace("\n", "\\n");
            sb.Append('[');
            sb.Append(part);
            if (start + take < s.Length)
            {
                sb.Append('…');
            }

            sb.Append(']');
        }

        private static bool ParseK2(string gameFlag)
        {
            if (string.IsNullOrEmpty(gameFlag))
            {
                return false;
            }

            string g = gameFlag.Trim().ToLowerInvariant();
            return g == "k2" || g == "tsl" || g == "2";
        }

        private static ActionsData LoadActions(bool k2, string k1Path, string k2Path)
        {
            return ActionsData.LoadForGame(k2, k1Path, k2Path);
        }
    }
}
