using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Resources;

namespace KPatcher
{
    /// <summary>
    /// Command-line handling for KPatcher.
    /// </summary>
    internal static class KPatcherCLI
    {
        internal sealed class CommandLineArgs
        {
            [CanBeNull]
            public string GameDir { get; set; }
            [CanBeNull]
            public string TslPatchData { get; set; }
            public int? NamespaceOptionIndex { get; set; }
            public bool Console { get; set; }
            public bool Gui { get; set; }
            public bool Uninstall { get; set; }
            public bool Install { get; set; }
            public bool Validate { get; set; }
            public bool Help { get; set; }
            public bool ListNamespaces { get; set; }
            public bool DryRun { get; set; }
            public bool ParityReport { get; set; }
        }

        internal enum CliOperation
        {
            None = 0,
            Install = 1,
            Uninstall = 2,
            Validate = 3,
            ListNamespaces = 4,
            DryRun = 5,
            ParityReport = 6,
        }

        /// <summary>True if any argument suggests the user intended CLI usage (beyond bare <c>--console</c>).</summary>
        internal static bool HasCliWorkIndicators(CommandLineArgs a) =>
            a.Install || a.Uninstall || a.Validate || a.ListNamespaces || a.DryRun || a.ParityReport
            || !string.IsNullOrEmpty(a.GameDir)
            || !string.IsNullOrEmpty(a.TslPatchData)
            || a.NamespaceOptionIndex.HasValue;

        internal static bool HasRequestedCliOperation(CommandLineArgs a)
        {
            return CountRequestedCliOperations(a) > 0;
        }

        internal static int CountRequestedCliOperations(CommandLineArgs a)
        {
            return (a.Install ? 1 : 0)
                 + (a.Uninstall ? 1 : 0)
                 + (a.Validate ? 1 : 0)
                 + (a.ListNamespaces ? 1 : 0)
                 + (a.DryRun ? 1 : 0)
                 + (a.ParityReport ? 1 : 0);
        }

        internal static CliOperation GetRequestedCliOperation(CommandLineArgs a)
        {
            if (a.Install)
            {
                return CliOperation.Install;
            }
            if (a.Uninstall)
            {
                return CliOperation.Uninstall;
            }
            if (a.Validate)
            {
                return CliOperation.Validate;
            }
            if (a.ListNamespaces)
            {
                return CliOperation.ListNamespaces;
            }
            if (a.DryRun)
            {
                return CliOperation.DryRun;
            }
            if (a.ParityReport)
            {
                return CliOperation.ParityReport;
            }
            return CliOperation.None;
        }

        internal static CommandLineArgs ParseArgs(string[] args)
        {
            var result = new CommandLineArgs();
            var positional = new System.Collections.Generic.List<string>();

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
                    case "--gui":
                        result.Gui = true;
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
                    case "--list-namespaces":
                        result.ListNamespaces = true;
                        break;
                    case "--dry-run":
                        result.DryRun = true;
                        break;
                    case "--parity-report":
                        result.ParityReport = true;
                        break;
                    case "--help":
                    case "-h":
                        result.Help = true;
                        break;
                    default:
                        if (!args[i].StartsWith("--", System.StringComparison.Ordinal))
                        {
                            positional.Add(args[i]);
                        }
                        break;
                }
            }

            int positionalCount = positional.Count;
            if (positionalCount >= 2)
            {
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
