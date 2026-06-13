using System;
using System.Globalization;
using System.IO;

namespace KEditChanges.Cli
{
    /// <summary>
    /// Agent-native CLI for changes.ini operations (non-interactive).
    /// </summary>
    public static class ChangesIniCli
    {
        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                PrintHelp();
                return 0;
            }

            string verb = args[0];
            string[] rest = Slice(args, 1);
            switch (verb)
            {
                case "validate":
                    return RunValidate(rest);
                case "summary":
                    return RunSummary(rest);
                case "serialize":
                    return RunSerialize(rest);
                case "reload":
                    return RunReload(rest);
                case "info":
                    Console.WriteLine(ChangeEditReMapping.Info);
                    return 0;
                case "capabilities":
                    return RunCapabilities(rest);
                default:
                    Console.Error.WriteLine("Unknown changes.ini command: " + verb);
                    PrintHelp();
                    return 1;
            }
        }

        private static int RunReload(string[] args)
        {
            string inputPath;
            string tslPatchDataPath;
            if (!TryParseInput(args, out inputPath, out tslPatchDataPath))
            {
                return 1;
            }

            try
            {
                var service = new ChangesIniService();
                ChangesIniDocument document = service.Load(inputPath, tslPatchDataPath);
                ChangesIniSummary summary = service.BuildSummary(document);
                Console.WriteLine("RELOADED " + document.SourcePath);
                Console.WriteLine("TotalPatches=" + summary.TotalPatches.ToString(CultureInfo.InvariantCulture));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static int RunValidate(string[] args)
        {
            string inputPath;
            string tslPatchDataPath;
            if (!TryParseInput(args, out inputPath, out tslPatchDataPath))
            {
                return 1;
            }

            try
            {
                var service = new ChangesIniService();
                ValidationResult result = service.ValidateFile(inputPath, tslPatchDataPath);
                foreach (string message in result.Messages)
                {
                    Console.WriteLine(message);
                }

                Console.WriteLine(result.IsValid ? "VALID" : "INVALID");
                return result.IsValid ? 0 : 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static int RunSummary(string[] args)
        {
            string inputPath;
            string tslPatchDataPath;
            if (!TryParseInput(args, out inputPath, out tslPatchDataPath))
            {
                return 1;
            }

            try
            {
                var service = new ChangesIniService();
                ChangesIniDocument document = service.Load(inputPath, tslPatchDataPath);
                ChangesIniSummary summary = service.BuildSummary(document);
                Console.WriteLine("WindowTitle=" + summary.WindowTitle);
                Console.WriteLine("LogLevel=" + summary.LogLevel.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("InstallerMode=" + (summary.InstallerMode ? "1" : "0"));
                Console.WriteLine("TlkModifiers=" + summary.TlkModifiers.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("InstallFiles=" + summary.InstallFiles.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("TwoDaFiles=" + summary.TwoDaFiles.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("GffFiles=" + summary.GffFiles.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("CompileEntries=" + summary.CompileEntries.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("HackEntries=" + summary.HackEntries.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("SsfFiles=" + summary.SsfFiles.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("TotalPatches=" + summary.TotalPatches.ToString(CultureInfo.InvariantCulture));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static int RunSerialize(string[] args)
        {
            string inputPath = null;
            string outputPath = null;
            string tslPatchDataPath = null;
            bool includeHeader = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "-i" || arg == "--input")
                {
                    inputPath = ReadNextArg(args, ref i, "input path");
                }
                else if (arg == "-o" || arg == "--output")
                {
                    outputPath = ReadNextArg(args, ref i, "output path");
                }
                else if (arg == "--tslpatchdata")
                {
                    tslPatchDataPath = ReadNextArg(args, ref i, "tslpatchdata path");
                }
                else if (arg == "--header")
                {
                    includeHeader = true;
                }
                else
                {
                    Console.Error.WriteLine("Unknown option: " + arg);
                    return 1;
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.Error.WriteLine("serialize requires -i <changes.ini>");
                return 1;
            }

            try
            {
                var service = new ChangesIniService();
                ChangesIniDocument document = service.Load(inputPath, tslPatchDataPath);
                string text = service.Serialize(document, includeHeader, verbose: true);
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    File.WriteAllText(outputPath, text);
                    Console.WriteLine("Wrote " + Path.GetFullPath(outputPath));
                }
                else
                {
                    Console.Write(text);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static bool TryParseInput(string[] args, out string inputPath, out string tslPatchDataPath)
        {
            inputPath = null;
            tslPatchDataPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "-i" || arg == "--input")
                {
                    inputPath = ReadNextArg(args, ref i, "input path");
                }
                else if (arg == "--tslpatchdata")
                {
                    tslPatchDataPath = ReadNextArg(args, ref i, "tslpatchdata path");
                }
                else
                {
                    Console.Error.WriteLine("Unknown option: " + arg);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.Error.WriteLine("requires -i <changes.ini>");
                return false;
            }

            return true;
        }

        private static string ReadNextArg(string[] args, ref int index, string name)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing value for " + name);
            }

            index++;
            return args[index];
        }

        private static string[] Slice(string[] args, int start)
        {
            if (start >= args.Length)
            {
                return new string[0];
            }

            var rest = new string[args.Length - start];
            Array.Copy(args, start, rest, 0, rest.Length);
            return rest;
        }

        private static int RunCapabilities(string[] args)
        {
            bool json = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--json")
                {
                    json = true;
                }
                else if (args[i] == "-h" || args[i] == "--help")
                {
                    Console.WriteLine("capabilities [--json]  List changes.ini CLI verbs for agent discovery");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("Unknown option: " + args[i]);
                    return 1;
                }
            }

            if (json)
            {
                Console.WriteLine(
                    "{\"domain\":\"changes.ini\",\"verbs\":[" +
                    "{\"name\":\"validate\",\"args\":\"-i <changes.ini> [--tslpatchdata <dir>]\"}," +
                    "{\"name\":\"summary\",\"args\":\"-i <changes.ini> [--tslpatchdata <dir>]\"}," +
                    "{\"name\":\"serialize\",\"args\":\"-i <changes.ini> [-o <out.ini>] [--header] [--tslpatchdata <dir>]\"}," +
                    "{\"name\":\"reload\",\"args\":\"-i <changes.ini> [--tslpatchdata <dir>]\"}," +
                    "{\"name\":\"info\",\"args\":\"\"}," +
                    "{\"name\":\"capabilities\",\"args\":\"[--json]\"}" +
                    "]}");
            }
            else
            {
                Console.WriteLine("changes.ini verbs: validate, summary, serialize, info, capabilities (--json)");
            }

            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("keditchanges-cli changes.ini commands (KEditChanges)");
            Console.WriteLine();
            Console.WriteLine("  validate -i <changes.ini> [--tslpatchdata <dir>]");
            Console.WriteLine("  summary  -i <changes.ini> [--tslpatchdata <dir>]");
            Console.WriteLine("  serialize -i <changes.ini> [-o <out.ini>] [--header] [--tslpatchdata <dir>]");
            Console.WriteLine("  reload    -i <changes.ini> [--tslpatchdata <dir>]  Re-read file from disk");
            Console.WriteLine("  info     Library status");
            Console.WriteLine("  capabilities [--json]  Agent capability discovery");
        }
    }
}
