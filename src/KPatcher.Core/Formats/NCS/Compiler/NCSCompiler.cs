using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Logger;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Compiles NSS to NCS for the patcher: prefers managed KCompiler / NCSAuto on all platforms,
    /// falls back to nwnnsscomp.exe on Windows only if managed compilation fails.
    /// </summary>
    public class NCSCompiler
    {
        [CanBeNull]
        private readonly string _nwnnsscompPath;
        private readonly string _tempScriptFolder;
        private readonly PatchLogger _logger;

        public NCSCompiler([CanBeNull] string nwnnsscompPath, string tempScriptFolder, PatchLogger logger)
        {
            _nwnnsscompPath = nwnnsscompPath;
            _tempScriptFolder = tempScriptFolder ?? throw new ArgumentNullException(nameof(tempScriptFolder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Compiles an NSS script to NCS bytecode.
        /// </summary>
        /// <param name="nssSource">The NSS source code to compile</param>
        /// <param name="filename">The filename for the script (without path)</param>
        /// <param name="game">The game being patched (K1 or K2)</param>
        /// <returns>The compiled NCS bytecode, or the NSS source bytes if compilation failed</returns>
        public byte[] Compile(string nssSource, string filename, Game game)
        {
            if (string.IsNullOrEmpty(nssSource))
            {
                throw new ArgumentException("NSS source cannot be null or empty", nameof(nssSource));
            }

            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
            }

            // Ensure temp folder exists
            Directory.CreateDirectory(_tempScriptFolder);

            // Write NSS source to temp file
            string tempNssPath = Path.Combine(_tempScriptFolder, filename);
            File.WriteAllText(tempNssPath, nssSource, Encoding.GetEncoding("windows-1252"));

            // Managed KCompiler first (cross-platform). Optional Windows nwnnsscomp.exe only as fallback.
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool nwnnsscompExists = !string.IsNullOrEmpty(_nwnnsscompPath) && File.Exists(_nwnnsscompPath);

            try
            {
                NCS ncs = NCSAuto.CompileNss(nssSource, game);
                return NCSAuto.BytesNcs(ncs);
            }
            catch (Exception managedEx)
            {
                _logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.ManagedCompilationFailedFormat, filename, managedEx.Message));
            }

            if (isWindows && nwnnsscompExists)
            {
                try
                {
                    byte[] compiledBytes = CompileWithExternal(tempNssPath, filename, game);
                    if (compiledBytes != null)
                    {
                        _logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.CompiledUsingExternalNwnnsscompFormat, filename));
                        return compiledBytes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExternalNwnnsscompAlsoFailedFormat, filename, ex.Message));
                }
            }
            else if (!nwnnsscompExists && isWindows)
            {
                _logger.AddVerbose("nwnnsscomp.exe not present; no external fallback.");
            }

            _logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotCompileReturningUncompiledFormat, filename));
            return Encoding.GetEncoding("windows-1252").GetBytes(nssSource);
        }

        /// <summary>
        /// Attempts to compile using external nwnnsscomp.exe.
        /// </summary>
        [CanBeNull]
        private byte[] CompileWithExternal(string nssPath, string filename, Game game)
        {
            if (string.IsNullOrEmpty(_nwnnsscompPath))
            {
                return null;
            }

            string ncsFilename = Path.ChangeExtension(filename, ".ncs");
            string outputPath = Path.Combine(_tempScriptFolder, ncsFilename);

            // Delete existing NCS file if it exists
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            // Build command line arguments
            NwnnsscompConfig config = new ExternalNCSCompiler(_nwnnsscompPath)
                .Config(nssPath, outputPath, game);

            var startInfo = new ProcessStartInfo
            {
                FileName = _nwnnsscompPath,
                WorkingDirectory = _tempScriptFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (string arg in config.GetCompileArgs(_nwnnsscompPath).Skip(1))
            {
                startInfo.ArgumentList.Add(arg);
            }

            using (var process = new Process { StartInfo = startInfo })
            {

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        output.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        error.AppendLine(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for compilation with timeout (30 seconds)
                bool finished = process.WaitForExit(30000);

                if (!finished)
                {
                    process.Kill();
                    throw new TimeoutException($"Compilation of '{filename}' timed out after 30 seconds");
                }

                // Check if compilation succeeded
                if (process.ExitCode != 0)
                {
                    string errorOutput = error.ToString();
                    if (string.IsNullOrWhiteSpace(errorOutput))
                    {
                        errorOutput = output.ToString();
                    }

                    throw new InvalidOperationException($"nwnnsscomp.exe failed with exit code {process.ExitCode}:\n{errorOutput}");
                }

                // Read the compiled NCS file
                if (!File.Exists(outputPath))
                {
                    throw new FileNotFoundException($"Compilation succeeded but output file not found: {outputPath}");
                }

                byte[] compiledBytes = File.ReadAllBytes(outputPath);

                // Log success
                _logger.AddVerbose($"Successfully compiled '{filename}' to NCS ({compiledBytes.Length} bytes)");

                return compiledBytes;
            }
        }

        /// <summary>
        /// Validates that the nwnnsscomp.exe is the KPatcher version.
        /// </summary>
        public bool ValidateCompiler()
        {
            if (string.IsNullOrEmpty(_nwnnsscompPath) || !File.Exists(_nwnnsscompPath))
            {
                return false;
            }

            try
            {
                // Try to get version info
                var fileInfo = FileVersionInfo.GetVersionInfo(_nwnnsscompPath);
                string productName = fileInfo.ProductName;

                if (productName.Contains("KPATCHER", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // If not the expected version, log a warning but still return true (let it work)
                _logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.NwnnsscompNotExpectedVersionFormat, productName ?? "UNKNOWN"));
                return true;
            }
            catch
            {
                // Couldn't validate, but don't fail
                return true;
            }
        }
    }
}
