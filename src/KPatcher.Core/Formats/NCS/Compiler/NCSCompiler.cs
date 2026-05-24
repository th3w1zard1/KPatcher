using System;
using System.Globalization;
using System.IO;
using System.Text;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Logger;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Compiles NSS to NCS for the patcher using managed KCompiler / NCSAuto on all platforms.
    /// Does not shell out to external <c>nwnnsscomp.exe</c>; on failure returns uncompiled NSS bytes.
    /// </summary>
    public class NCSCompiler
    {
        private readonly string _tempScriptFolder;
        private readonly PatchLogger _logger;

        public NCSCompiler(string tempScriptFolder, PatchLogger logger)
        {
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

            try
            {
                NCS ncs = NCSAuto.CompileNss(nssSource, game);
                return NCSAuto.BytesNcs(ncs);
            }
            catch (Exception managedEx)
            {
                _logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.ManagedCompilationFailedFormat, filename, managedEx.Message));
            }

            _logger.AddWarning(string.Format(CultureInfo.CurrentCulture, PatcherResources.CouldNotCompileReturningUncompiledFormat, filename));
            return Encoding.GetEncoding("windows-1252").GetBytes(nssSource);
        }
    }
}
