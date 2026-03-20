using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Resources;
using KPatcher.UI;
using KPatcher.UI.ViewModels;
using AppCore = KPatcher.UI.Core;

namespace KPatcher
{

    class Program
    {
        // Register assembly resolve handler for RtfDomParserAv dependency
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static System.Reflection.Assembly OnAssemblyResolve(object sender, System.ResolveEventArgs args)
        {
            // Handle RtfDomParserAv dependency for Simplecto.Avalonia.RichTextBox
            if (args.Name.StartsWith("RtfDomParserAv", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Try to find the DLL in multiple locations
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string dllPath = Path.Combine(baseDir, "RtfDomParserAv.dll");

                    if (File.Exists(dllPath))
                    {
                        return System.Reflection.Assembly.LoadFrom(dllPath);
                    }

                    // Try to find it next to AvRichTextBox.dll (same directory)
                    string avRichTextBoxPath = Path.Combine(baseDir, "AvRichTextBox.dll");
                    if (File.Exists(avRichTextBoxPath))
                    {
                        string candidateNextToAvRich = Path.Combine(Path.GetDirectoryName(avRichTextBoxPath), "RtfDomParserAv.dll");
                        if (File.Exists(candidateNextToAvRich))
                        {
                            return System.Reflection.Assembly.LoadFrom(candidateNextToAvRich);
                        }
                    }

                    // Try to find it in the cloned repository (if available)
                    // Search multiple possible repository locations
                    string[] possibleRepoPaths = new[]
                    {
                        Path.Combine(baseDir, "..", "..", "..", "temp_avrichtextbox", "AvRichTextBox", "RtfDomParserAv.dll"),
                        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? baseDir, "..", "..", "..", "temp_avrichtextbox", "AvRichTextBox", "RtfDomParserAv.dll"),
                    };

                    foreach (string repoPath in possibleRepoPaths)
                    {
                        if (File.Exists(repoPath))
                        {
                            try
                            {
                                File.Copy(repoPath, dllPath, true);
                                Console.WriteLine($"[AssemblyResolve] Copied RtfDomParserAv.dll from repository to output directory");
                            }
                            catch (Exception copyEx)
                            {
                                Console.WriteLine($"[AssemblyResolve] Warning: Could not copy from repo: {copyEx.Message}");
                            }
                            if (File.Exists(dllPath))
                            {
                                Console.WriteLine($"[AssemblyResolve] Loading RtfDomParserAv.dll from: {dllPath}");
                                return System.Reflection.Assembly.LoadFrom(dllPath);
                            }
                            break;
                        }
                    }

                    // Try to find it in NuGet package cache
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string nugetPackages = Path.Combine(userProfile, ".nuget", "packages");

                    if (Directory.Exists(nugetPackages))
                    {
                        // Search in Simplecto package folder
                        string[] simplectoFolders = Directory.GetDirectories(nugetPackages, "simplecto.avalon*", SearchOption.TopDirectoryOnly);
                        foreach (string folder in simplectoFolders)
                        {
                            // Search in lib folders
                            string[] libDirs = Directory.GetDirectories(folder, "lib", SearchOption.AllDirectories);
                            foreach (string libDir in libDirs)
                            {
                                string candidatePath = Path.Combine(libDir, "RtfDomParserAv.dll");
                                if (File.Exists(candidatePath))
                                {
                                    // Copy to output directory for future use
                                    try
                                    {
                                        File.Copy(candidatePath, dllPath, true);
                                        Console.WriteLine($"[AssemblyResolve] Copied RtfDomParserAv.dll from NuGet cache to output directory");
                                    }
                                    catch (Exception copyEx)
                                    {
                                        Console.WriteLine($"[AssemblyResolve] Warning: Could not copy DLL: {copyEx.Message}");
                                    }
                                    Console.WriteLine($"[AssemblyResolve] Loading RtfDomParserAv.dll from: {candidatePath}");
                                    return System.Reflection.Assembly.LoadFrom(candidatePath);
                                }
                            }

                            // Also search directly in the package folder (sometimes DLLs are at the root)
                            string[] allDlls = Directory.GetFiles(folder, "RtfDomParserAv.dll", SearchOption.AllDirectories);
                            foreach (string dllFile in allDlls)
                            {
                                if (File.Exists(dllFile))
                                {
                                    try
                                    {
                                        File.Copy(dllFile, dllPath, true);
                                        Console.WriteLine($"[AssemblyResolve] Copied RtfDomParserAv.dll from NuGet package to output directory");
                                    }
                                    catch { }
                                    Console.WriteLine($"[AssemblyResolve] Loading RtfDomParserAv.dll from: {dllFile}");
                                    return System.Reflection.Assembly.LoadFrom(dllFile);
                                }
                            }
                        }
                    }

                    // Last resort: Try using RtfDomParser.dll as fallback (not ideal but might work)
                    string fallbackPath = Path.Combine(baseDir, "RtfDomParser.dll");
                    if (File.Exists(fallbackPath))
                    {
                        Console.WriteLine($"[AssemblyResolve] Attempting to use RtfDomParser.dll as fallback for RtfDomParserAv");
                        try
                        {
                            return System.Reflection.Assembly.LoadFrom(fallbackPath);
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"[AssemblyResolve] Fallback failed: {fallbackEx.Message}");
                        }
                    }

                    Console.WriteLine($"[AssemblyResolve] RtfDomParserAv.dll not found in any expected location");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AssemblyResolve] Error loading RtfDomParserAv: {ex.Message}");
                }
            }

            return null;
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Parse command line arguments
            CommandLineArgs cmdlineArgs = ParseArgs(args);

            // Determine if we should run in CLI mode
            bool forceCli = cmdlineArgs.Install || cmdlineArgs.Uninstall || cmdlineArgs.Validate;

            if (forceCli)
            {
                // CLI mode explicitly requested - no GUI
                ExecuteCli(cmdlineArgs);
            }
            else
            {
                // GUI mode by default - try GUI, fall back gracefully if unavailable
                try
                {
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    // If GUI is unavailable, print error and exit gracefully
                    Console.Error.WriteLine($"[Warning] Display driver not available, cannot run in GUI mode: {ex.Message}");
                    Console.Error.WriteLine("[Info] Use --help to see CLI options");
                    Environment.Exit(0);
                }
            }
        }

        private static CommandLineArgs ParseArgs(string[] args)
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
                        result.Help = true;
                        break;
                }
            }

            // Handle positional arguments (game_dir tslpatchdata [namespace_option_index])
            int positionalCount = args.Count(a => !a.StartsWith("--"));
            if (positionalCount >= 2)
            {
                string[] positional = args.Where(a => !a.StartsWith("--")).ToArray();
                result.GameDir = positional[0];
                result.TslPatchData = positional[1];
                if (positionalCount >= 3 && int.TryParse(positional[2], out int posIndex))
                {
                    result.NamespaceOptionIndex = posIndex;
                }
            }

            return result;
        }

        private static void ExecuteCli(CommandLineArgs args)
        {
            var logger = new PatchLogger();
            logger.VerboseLogged += (s, l) => Console.WriteLine($"[VERBOSE] {l.Message}");
            logger.NoteLogged += (s, l) => Console.WriteLine($"[NOTE] {l.Message}");
            logger.WarningLogged += (s, l) => Console.WriteLine($"[WARNING] {l.Message}");
            logger.ErrorLogged += (s, l) => Console.Error.WriteLine($"[ERROR] {l.Message}");

            // Load mod
            if (string.IsNullOrEmpty(args.TslPatchData))
            {
                Console.Error.WriteLine("[Error] No mod path specified. Use --tslpatchdata <path>");
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            AppCore.ModInfo modInfo;
            try
            {
                modInfo = AppCore.LoadMod(args.TslPatchData);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorFailedToLoadMod, ex.Message));
                Environment.Exit((int)AppCore.ExitCode.NamespacesIniNotFound);
                return;
            }

            // Select namespace
            string selectedNamespace;
            if (args.NamespaceOptionIndex.HasValue)
            {
                if (args.NamespaceOptionIndex.Value >= modInfo.Namespaces.Count)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorNamespaceIndexOutOfRange, args.NamespaceOptionIndex.Value, modInfo.Namespaces.Count - 1));
                    Environment.Exit((int)AppCore.ExitCode.NamespaceIndexOutOfRange);
                    return;
                }
                selectedNamespace = modInfo.Namespaces[args.NamespaceOptionIndex.Value].Name;
            }
            else
            {
                selectedNamespace = modInfo.Namespaces[0].Name;
            }

            // Validate game path
            if (string.IsNullOrEmpty(args.GameDir))
            {
                Console.Error.WriteLine(PatcherResources.CliErrorNoGameDirectory);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            string gamePath;
            try
            {
                gamePath = AppCore.ValidateGameDirectory(args.GameDir);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"[Error] Invalid game directory: {ex.GetType().Name}: {ex.Message}");
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            // Validate paths
            if (!AppCore.ValidateInstallPaths(modInfo.ModPath, gamePath))
            {
                Console.Error.WriteLine(PatcherResources.CliErrorInvalidModOrGamePaths);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            // Check which operation to perform
            int numActions = (args.Install ? 1 : 0) + (args.Uninstall ? 1 : 0) + (args.Validate ? 1 : 0);
            if (numActions > 1)
            {
                Console.Error.WriteLine(PatcherResources.CliErrorCannotRunMultipleOperations);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }
            if (numActions == 0)
            {
                Console.Error.WriteLine(PatcherResources.CliErrorMustSpecifyOperation);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            // Execute the requested operation
            try
            {
                if (args.Install)
                {
                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallingMod, modInfo.ModPath, gamePath));
                    var cancellationToken = new CancellationToken();
                    AppCore.InstallResult result = AppCore.InstallMod(
                        modInfo.ModPath,
                        gamePath,
                        modInfo.Namespaces,
                        selectedNamespace,
                        logger,
                        cancellationToken);

                    Console.WriteLine($"[Info] Install completed: {result.NumErrors} errors, {result.NumWarnings} warnings, {result.NumPatches} patches");
                    Console.WriteLine($"[Info] Install time: {AppCore.FormatInstallTime(result.InstallTime)}");

                    if (result.NumErrors > 0)
                    {
                        Environment.Exit((int)AppCore.ExitCode.InstallCompletedWithErrors);
                    }
                    Environment.Exit((int)AppCore.ExitCode.Success);
                }
                else if (args.Uninstall)
                {
                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoUninstallingMod, gamePath));
                    bool fullyRan = AppCore.UninstallMod(modInfo.ModPath, gamePath, logger);
                    if (fullyRan)
                    {
                        Console.WriteLine(PatcherResources.CliInfoUninstallCompletedSuccessfully);
                    }
                    else
                    {
                        Console.WriteLine(PatcherResources.CliWarningUninstallCompletedWithWarnings);
                    }
                    Environment.Exit((int)AppCore.ExitCode.Success);
                }
                else if (args.Validate)
                {
                    Console.WriteLine(PatcherResources.CliInfoValidatingMod);
                    AppCore.ValidateConfig(modInfo.ModPath, modInfo.Namespaces, selectedNamespace, logger);
                    Console.WriteLine(PatcherResources.CliInfoValidationCompletedSuccessfully);
                    Environment.Exit((int)AppCore.ExitCode.Success);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorFormat, ex.GetType().Name, ex.Message));
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInnerException, ex.InnerException.Message));
                }
                Environment.Exit((int)AppCore.ExitCode.ExceptionDuringInstall);
            }
        }

        private class CommandLineArgs
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

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<KPatcher.UI.App>()
                .UsePlatformDetect()
                //.WithInterFont()
                .LogToTrace();
    }
}
