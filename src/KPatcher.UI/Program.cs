using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Avalonia;
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
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static System.Reflection.Assembly OnAssemblyResolve(object sender, System.ResolveEventArgs args)
        {
            if (args.Name.StartsWith("RtfDomParserAv", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string dllPath = Path.Combine(baseDir, "RtfDomParserAv.dll");

                    if (File.Exists(dllPath))
                    {
                        return System.Reflection.Assembly.LoadFrom(dllPath);
                    }

                    string avRichTextBoxPath = Path.Combine(baseDir, "AvRichTextBox.dll");
                    if (File.Exists(avRichTextBoxPath))
                    {
                        string candidateNextToAvRich = Path.Combine(Path.GetDirectoryName(avRichTextBoxPath), "RtfDomParserAv.dll");
                        if (File.Exists(candidateNextToAvRich))
                        {
                            return System.Reflection.Assembly.LoadFrom(candidateNextToAvRich);
                        }
                    }

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

                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string nugetPackages = Path.Combine(userProfile, ".nuget", "packages");

                    if (Directory.Exists(nugetPackages))
                    {
                        string[] simplectoFolders = Directory.GetDirectories(nugetPackages, "simplecto.avalon*", SearchOption.TopDirectoryOnly);
                        foreach (string folder in simplectoFolders)
                        {
                            string[] libDirs = Directory.GetDirectories(folder, "lib", SearchOption.AllDirectories);
                            foreach (string libDir in libDirs)
                            {
                                string candidatePath = Path.Combine(libDir, "RtfDomParserAv.dll");
                                if (File.Exists(candidatePath))
                                {
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

        /// <summary>Whether a graphical desktop session is likely available (before initializing Avalonia).</summary>
        private static bool DesktopDisplayLikelyAvailable()
        {
            if (OperatingSystem.IsWindows())
            {
                return Environment.UserInteractive;
            }

            if (OperatingSystem.IsLinux())
            {
                string d = Environment.GetEnvironmentVariable("DISPLAY");
                string w = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
                return !string.IsNullOrEmpty(d) || !string.IsNullOrEmpty(w);
            }

            if (OperatingSystem.IsMacOS())
            {
                return Environment.UserInteractive;
            }

            return Environment.UserInteractive;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            KPatcherCLI.CommandLineArgs cmdlineArgs = KPatcherCLI.ParseArgs(args);

            if (cmdlineArgs.Help)
            {
                KPatcherCLI.WriteHelp(Console.Out);
                Environment.Exit(0);
                return;
            }

            if (cmdlineArgs.Console && !KPatcherCLI.HasCliWorkIndicators(cmdlineArgs))
            {
                Console.Error.WriteLine(PatcherResources.CliErrorConsoleRequiresCliArgs);
                Environment.Exit(1);
                return;
            }

            bool forceCliOps = cmdlineArgs.Install || cmdlineArgs.Uninstall || cmdlineArgs.Validate;
            if (forceCliOps)
            {
                ExecuteCli(cmdlineArgs);
                return;
            }

            if (cmdlineArgs.Console)
            {
                ExecuteCli(cmdlineArgs);
                return;
            }

            if (DesktopDisplayLikelyAvailable())
            {
                try
                {
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliWarningDisplayDriverNotAvailable, ex.Message));
                    Console.Error.WriteLine(PatcherResources.CliInfoUseHelpForOptions);
                    Environment.Exit(1);
                }
            }
            else if (KPatcherCLI.HasCliWorkIndicators(cmdlineArgs))
            {
                ExecuteCli(cmdlineArgs);
            }
            else
            {
                Console.Error.WriteLine(PatcherResources.CliErrorNoDisplayUseCli);
                Console.Error.WriteLine(PatcherResources.CliInfoUseHelpForOptions);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
            }
        }

        private static void ExecuteCli(KPatcherCLI.CommandLineArgs args)
        {
            var logger = new PatchLogger();
            logger.DiagnosticLogged += (s, l) => Console.WriteLine($"[DIAG] {l.Message}");
            logger.VerboseLogged += (s, l) => Console.WriteLine($"[VERBOSE] {l.Message}");
            logger.NoteLogged += (s, l) => Console.WriteLine($"[NOTE] {l.Message}");
            logger.WarningLogged += (s, l) => Console.WriteLine($"[WARNING] {l.Message}");
            logger.ErrorLogged += (s, l) => Console.Error.WriteLine($"[ERROR] {l.Message}");

            if (string.IsNullOrEmpty(args.TslPatchData))
            {
                Console.Error.WriteLine("[Error] No mod path specified. Use --tslpatchdata <path>");
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            AppCore.ModInfo modInfo;
            try
            {
                modInfo = AppCore.LoadMod(args.TslPatchData, logger);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorFailedToLoadMod, ex.Message));
                Environment.Exit((int)AppCore.ExitCode.NamespacesIniNotFound);
                return;
            }

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

            if (string.IsNullOrEmpty(args.GameDir))
            {
                Console.Error.WriteLine(PatcherResources.CliErrorNoGameDirectory);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            string gamePath;
            try
            {
                gamePath = AppCore.ValidateGameDirectory(args.GameDir, logger);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"[Error] Invalid game directory: {ex.GetType().Name}: {ex.Message}");
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

            if (!AppCore.ValidateInstallPaths(modInfo.ModPath, gamePath, logger))
            {
                Console.Error.WriteLine(PatcherResources.CliErrorInvalidModOrGamePaths);
                Environment.Exit((int)AppCore.ExitCode.NumberOfArgs);
                return;
            }

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

                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallCompleted, result.NumErrors, result.NumWarnings, result.NumPatches));
                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallTime, AppCore.FormatInstallTime(result.InstallTime)));

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

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<KPatcher.UI.App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
