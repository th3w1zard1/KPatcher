using System;
using System.Reflection;
using System.Text;
using KCompiler;
using KCompiler.Cli;
using NCSDecomp.Core;

namespace KEditChanges.Net
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                PrintRootHelp();
                return 0;
            }

            if (args[0] == "--version" || args[0] == "-V")
            {
                PrintVersion();
                return 0;
            }

            string verb = args[0];
            string[] rest = new string[args.Length - 1];
            if (rest.Length > 0)
            {
                Array.Copy(args, 1, rest, 0, rest.Length);
            }

            switch (verb)
            {
                case "compile":
                case "kcompiler":
                    return RunKCompiler(rest);
                case "ncsdecomp":
                case "decomp":
                    return NcsDecompCli.Run(rest);
                case "info":
                    Console.WriteLine(KEditChanges.ChangeEditPlaceholder.Info);
                    return 0;
                default:
                    Console.Error.WriteLine("Error: Unknown command: " + verb);
                    PrintRootHelp();
                    return 1;
            }
        }

        private static void PrintRootHelp()
        {
            Console.WriteLine("keditchanges-cli — umbrella CLI (KEditChanges + KCompiler + NCS decompile)");
            Console.WriteLine();
            Console.WriteLine("Usage: keditchanges-cli <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  compile | kcompiler   NSS→NCS (same flags as standalone kcompiler / nwnnsscomp).");
            Console.WriteLine("  ncsdecomp | decomp    NCS→NSS (same flags as NCSDecompCLI: -i -o [-g] ...).");
            Console.WriteLine("  info                  Show KEditChanges library status.");
            Console.WriteLine("  -h, --help            Show this help.");
            Console.WriteLine("  -V, --version         Print tool version.");
        }

        private static void PrintVersion()
        {
            Assembly a = typeof(Program).Assembly;
            string informational = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                Console.WriteLine("keditchanges-cli " + informational);
                return;
            }

            Version v = a.GetName().Version;
            Console.WriteLine("keditchanges-cli " + (v != null ? v.ToString(3) : "0.0.0"));
        }

        private static int RunKCompiler(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                NwnnsscompParseResult parsed = NwnnsscompCliParser.Parse(args);
                if (parsed.IsHelp)
                {
                    Console.Out.WriteLine(parsed.ErrorMessage.TrimEnd());
                    return 0;
                }

                if (!parsed.Success)
                {
                    if (!string.IsNullOrEmpty(parsed.ErrorMessage))
                    {
                        Console.Error.WriteLine(parsed.ErrorMessage.TrimEnd());
                    }

                    return 1;
                }

                ManagedNwnnsscomp.CompileFile(
                    parsed.SourcePath,
                    parsed.OutputPath,
                    parsed.Game,
                    parsed.Debug,
                    parsed.NwscriptPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }
    }
}
