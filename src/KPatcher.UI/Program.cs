using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Avalonia;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Namespaces;
using KPatcher.Core.Resources;
using KPatcher.UI;
using KPatcher.UI.Parity;
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

            bool forceCliOps = KPatcherCLI.HasRequestedCliOperation(cmdlineArgs);
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
            int exitCode = RunCli(args, Console.Out, Console.Error);
            Environment.Exit(exitCode);
        }

        internal static int RunCli(KPatcherCLI.CommandLineArgs args, TextWriter output, TextWriter error)
        {
            var logger = new PatchLogger();
            logger.DiagnosticLogged += (s, l) => output.WriteLine("[DIAG] " + l.Message);
            logger.VerboseLogged += (s, l) => output.WriteLine("[VERBOSE] " + l.Message);
            logger.NoteLogged += (s, l) => output.WriteLine("[NOTE] " + l.Message);
            logger.WarningLogged += (s, l) => output.WriteLine("[WARNING] " + l.Message);
            logger.ErrorLogged += (s, l) => error.WriteLine("[ERROR] " + l.Message);

            int operationCount = KPatcherCLI.CountRequestedCliOperations(args);
            if (operationCount > 1)
            {
                error.WriteLine(PatcherResources.CliErrorCannotRunMultipleOperations);
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            KPatcherCLI.CliOperation operation = KPatcherCLI.GetRequestedCliOperation(args);
            if (operation == KPatcherCLI.CliOperation.None)
            {
                error.WriteLine(PatcherResources.CliErrorMustSpecifyOperation);
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            if (operation == KPatcherCLI.CliOperation.ParityReport)
            {
                output.WriteLine(ParityLedger.BuildReport());
                return (int)AppCore.ExitCode.Success;
            }

            if (string.IsNullOrEmpty(args.TslPatchData))
            {
                error.WriteLine(PatcherResources.CliErrorNoModPath);
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            AppCore.ModInfo modInfo;
            try
            {
                modInfo = AppCore.LoadMod(args.TslPatchData, logger);
            }
            catch (FileNotFoundException ex)
            {
                error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorFailedToLoadMod, ex.Message));
                return (int)AppCore.ExitCode.NamespacesIniNotFound;
            }

            if (operation == KPatcherCLI.CliOperation.ListNamespaces)
            {
                WriteNamespaces(output, modInfo.Namespaces);
                return (int)AppCore.ExitCode.Success;
            }

            PatcherNamespace selectedNamespaceOption;
            if (!TryGetSelectedNamespace(args, modInfo.Namespaces, error, out selectedNamespaceOption))
            {
                return (int)AppCore.ExitCode.NamespaceIndexOutOfRange;
            }

            if (operation == KPatcherCLI.CliOperation.DryRun)
            {
                try
                {
                    var config = AppCore.LoadNamespaceConfigReadOnly(
                        modInfo.ModPath,
                        modInfo.Namespaces,
                        selectedNamespaceOption.Name,
                        logger);
                    string resolvedChangesPath = AppCore.GetResolvedChangesDisplayPath(
                        modInfo.ModPath,
                        modInfo.Namespaces,
                        selectedNamespaceOption.Name,
                        logger);
                    output.WriteLine(AppCore.BuildConfigurationSummary(
                        resolvedChangesPath,
                        selectedNamespaceOption.RtfFilePath(),
                        config));
                    return (int)AppCore.ExitCode.Success;
                }
                catch (Exception ex)
                {
                    WriteCliException(error, ex);
                    return (int)AppCore.ExitCode.ExceptionDuringInstall;
                }
            }

            if (string.IsNullOrEmpty(args.GameDir))
            {
                error.WriteLine(PatcherResources.CliErrorNoGameDirectory);
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            string gamePath;
            try
            {
                gamePath = AppCore.ValidateGameDirectory(args.GameDir, logger);
            }
            catch (ArgumentException ex)
            {
                error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorInvalidGameDirectory, ex.GetType().Name, ex.Message));
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            if (!AppCore.ValidateInstallPaths(modInfo.ModPath, gamePath, logger))
            {
                error.WriteLine(PatcherResources.CliErrorInvalidModOrGamePaths);
                return (int)AppCore.ExitCode.NumberOfArgs;
            }

            try
            {
                if (operation == KPatcherCLI.CliOperation.Install)
                {
                    output.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallingMod, modInfo.ModPath, gamePath));
                    var cancellationToken = new CancellationToken();
                    AppCore.InstallResult result = AppCore.InstallMod(
                        modInfo.ModPath,
                        gamePath,
                        modInfo.Namespaces,
                        selectedNamespaceOption.Name,
                        logger,
                        cancellationToken);

                    output.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallCompleted, result.NumErrors, result.NumWarnings, result.NumPatches));
                    output.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoInstallTime, AppCore.FormatInstallTime(result.InstallTime)));

                    if (result.NumErrors > 0)
                    {
                        return (int)AppCore.ExitCode.InstallCompletedWithErrors;
                    }
                    return (int)AppCore.ExitCode.Success;
                }
                else if (operation == KPatcherCLI.CliOperation.Uninstall)
                {
                    output.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInfoUninstallingMod, gamePath));
                    bool fullyRan = AppCore.UninstallMod(modInfo.ModPath, gamePath, logger);
                    if (fullyRan)
                    {
                        output.WriteLine(PatcherResources.CliInfoUninstallCompletedSuccessfully);
                    }
                    else
                    {
                        output.WriteLine(PatcherResources.CliWarningUninstallCompletedWithWarnings);
                    }
                    return (int)AppCore.ExitCode.Success;
                }
                else if (operation == KPatcherCLI.CliOperation.Validate)
                {
                    output.WriteLine(PatcherResources.CliInfoValidatingMod);
                    AppCore.ValidateConfig(modInfo.ModPath, modInfo.Namespaces, selectedNamespaceOption.Name, logger);
                    output.WriteLine(PatcherResources.CliInfoValidationCompletedSuccessfully);
                    return (int)AppCore.ExitCode.Success;
                }
            }
            catch (Exception ex)
            {
                WriteCliException(error, ex);
                return (int)AppCore.ExitCode.ExceptionDuringInstall;
            }

            return (int)AppCore.ExitCode.Success;
        }

        private static bool TryGetSelectedNamespace(
            KPatcherCLI.CommandLineArgs args,
            System.Collections.Generic.List<PatcherNamespace> namespaces,
            TextWriter error,
            out PatcherNamespace selectedNamespace)
        {
            if (args.NamespaceOptionIndex.HasValue)
            {
                if (args.NamespaceOptionIndex.Value < 0 || args.NamespaceOptionIndex.Value >= namespaces.Count)
                {
                    error.WriteLine(string.Format(
                        CultureInfo.CurrentCulture,
                        PatcherResources.CliErrorNamespaceIndexOutOfRange,
                        args.NamespaceOptionIndex.Value,
                        namespaces.Count - 1));
                    selectedNamespace = null;
                    return false;
                }

                selectedNamespace = namespaces[args.NamespaceOptionIndex.Value];
                return true;
            }

            selectedNamespace = namespaces[0];
            return true;
        }

        private static void WriteNamespaces(TextWriter output, System.Collections.Generic.List<PatcherNamespace> namespaces)
        {
            for (int i = 0; i < namespaces.Count; i++)
            {
                PatcherNamespace patcherNamespace = namespaces[i];
                string displayName = string.IsNullOrWhiteSpace(patcherNamespace.Name)
                    ? patcherNamespace.ChangesFilePath()
                    : patcherNamespace.Name;
                output.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}: {1}", i, displayName));
            }
        }

        private static void WriteCliException(TextWriter error, Exception ex)
        {
            error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliErrorFormat, ex.GetType().Name, ex.Message));
            if (ex.InnerException != null)
            {
                error.WriteLine(string.Format(CultureInfo.CurrentCulture, PatcherResources.CliInnerException, ex.InnerException.Message));
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<KPatcher.UI.App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
