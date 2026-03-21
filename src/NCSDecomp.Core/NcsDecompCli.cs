// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;
using System.Text;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Shared CLI implementation for NCS→NSS (used by NCSDecompCLI and umbrella tools).
    /// </summary>
    public static class NcsDecompCli
    {
        public static int Run(string[] args)
        {
            string inputPath = null;
            string outputPath = null;
            string game = "k1";
            bool verbose = false;
            bool noConfig = false;
            bool gameFromCli = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i" && i + 1 < args.Length) { inputPath = args[++i]; continue; }
                if (args[i] == "-o" && i + 1 < args.Length) { outputPath = args[++i]; continue; }
                if (args[i] == "-g" && i + 1 < args.Length) { game = args[++i]; gameFromCli = true; continue; }
                if (args[i] == "-v" || args[i] == "--verbose") { verbose = true; continue; }
                if (args[i] == "--no-config") { noConfig = true; continue; }
                if (args[i] == "--help" || args[i] == "-h")
                {
                    PrintHelp();
                    return 0;
                }
            }

            if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
            {
                Console.Error.WriteLine("Error: -i <input.ncs> and -o <output.nss> are required.");
                PrintHelp();
                return 1;
            }

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine("Error: Input file not found: " + inputPath);
                return 1;
            }

            var log = NcsDecompLogger.Default;
            try
            {
                if (verbose)
                {
                    log.StartNcsDecompSection();
                }

                NcsDecompSettings settings = null;
                if (!noConfig)
                {
                    settings = NcsDecompSettings.Load(NcsDecompSettings.GetDefaultAppBaseDirectory(), true);
                    if (verbose && settings.ConfigLoadedFromPath != null)
                    {
                        log.Info("Config: " + settings.ConfigLoadedFromPath);
                    }
                }

                byte[] ncsBytes = File.ReadAllBytes(inputPath);
                bool k2;
                if (gameFromCli)
                {
                    k2 = string.Equals(game, "k2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(game, "tsl", StringComparison.OrdinalIgnoreCase);
                    FileDecompilerOptions.IsK2Selected = k2;
                }
                else if (settings != null)
                {
                    k2 = FileDecompilerOptions.IsK2Selected;
                }
                else
                {
                    k2 = string.Equals(game, "k2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(game, "tsl", StringComparison.OrdinalIgnoreCase);
                    FileDecompilerOptions.IsK2Selected = k2;
                }

                if (verbose)
                {
                    log.Info("Input: " + inputPath + " (" + ncsBytes.Length + " bytes), game: " + (k2 ? "TSL" : "K1"));
                }

                ActionsData actions;
                try
                {
                    string k1p = settings != null ? settings.K1NwscriptPath : null;
                    string k2p = settings != null ? settings.K2NwscriptPath : null;
                    actions = ActionsData.LoadForGame(k2, k1p, k2p);
                }
                catch (FileNotFoundException ex)
                {
                    Console.Error.WriteLine("Error: Embedded nwscript action table not found. Add k1_nwscript.nss / tsl_nwscript.nss under NCSDecomp.Core/Resources/.");
                    Console.Error.WriteLine(ex.Message);
                    return 1;
                }

                var decompiler = new FileDecompiler(actions);
                string nss = decompiler.DecompileToNss(ncsBytes);
                Encoding enc = ResolveOutputEncoding(settings);
                File.WriteAllText(outputPath, nss, enc);
                if (verbose)
                {
                    log.Success("Wrote NSS: " + outputPath + " (" + nss.Length + " chars)");
                    log.EndSection();
                }
                else
                {
                    Console.WriteLine("Wrote NSS: " + outputPath);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("NCSDecomp.NET - KotOR NCS script decompiler (CLI)");
            Console.WriteLine("Usage: NCSDecompCLI -i <input.ncs> -o <output.nss> [-g k1|k2] [-v] [--no-config]");
            Console.WriteLine("  -i    Input .ncs file");
            Console.WriteLine("  -o    Output .nss path");
            Console.WriteLine("  -g    Game: k1 (KotOR 1) or k2 (TSL) — overrides Game Variant in config");
            Console.WriteLine("  -v    Verbose / colored progress on stderr");
            Console.WriteLine("  By default loads config/ncsdecomp.conf next to this exe (Java-compatible keys).");
            Console.WriteLine("  --no-config   Skip config file; use defaults except -g / embedded nwscript.");
        }

        private static Encoding ResolveOutputEncoding(NcsDecompSettings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.EncodingName))
            {
                return new UTF8Encoding(false);
            }

            try
            {
                return Encoding.GetEncoding(settings.EncodingName);
            }
            catch
            {
                return new UTF8Encoding(false);
            }
        }
    }
}
