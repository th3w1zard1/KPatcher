using System.IO;
using System.Linq;
using JetBrains.Annotations;
using KPatcher.Core.Resources;

namespace KPatcher
{
    /// <summary>
    /// Command-line parsing aligned with PyKotor HoloPatcher (<c>holopatcher/core.py</c>).
    /// </summary>
    internal static class HoloPatcherCli
    {
        internal sealed class CommandLineArgs
        {
            [CanBeNull]
            public string GameDir { get; set; }
            [CanBeNull]
            public string TslPatchData { get; set; }
            public int? NamespaceOptionIndex { get; set; }
            public bool Console { get; set; }
            public bool Uninstall { get; set; }
            public bool Install { get; set; }
            public bool Validate { get; set; }
            public bool Help { get; set; }
        }

        internal static CommandLineArgs ParseArgs(string[] args)
        {
            var result = new CommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--game-dir" when i + 1 < args.Length:
                        result.GameDir = args[++i];
                        break;
                    case "--tslpatchdata" when i + 1 < args.Length:
                        result.TslPatchData = args[++i];
                        break;
                    case "--namespace-option-index" when i + 1 < args.Length:
                        if (int.TryParse(args[++i], out int index))
                        {
                            result.NamespaceOptionIndex = index;
                        }
                        break;
                    case "--console":
                        result.Console = true;
                        break;
                    case "--uninstall":
                        result.Uninstall = true;
                        break;
                    case "--install":
                        result.Install = true;
                        break;
                    case "--validate":
                        result.Validate = true;
                        break;
                    case "--help":
                    case "-h":
                        result.Help = true;
                        break;
                }
            }

            int positionalCount = args.Count(a => !a.StartsWith("--", System.StringComparison.Ordinal));
            if (positionalCount >= 2)
            {
                string[] positional = args.Where(a => !a.StartsWith("--", System.StringComparison.Ordinal)).ToArray();
                result.GameDir = positional[0];
                result.TslPatchData = positional[1];
                if (positionalCount >= 3 && int.TryParse(positional[2], out int posIndex))
                {
                    result.NamespaceOptionIndex = posIndex;
                }
            }

            return result;
        }

        internal static void WriteHelp(TextWriter output)
        {
            output.WriteLine(PatcherResources.CliHelpUsage);
        }
    }
}
