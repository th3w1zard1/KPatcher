using System;
using System.IO;
using System.Linq;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Tools;

namespace KPatcher.Core.Uninstall
{

    /// <summary>
    /// Helper functions for uninstalling mods.
    /// </summary>
    public static class UninstallHelpers
    {
        /// <summary>
        /// Checks if a filename represents a .MOD file.
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the file is a .MOD file, False otherwise</returns>
        private static bool IsModFile(string filename)
        {
            return filename.EndsWith(".mod", StringComparison.OrdinalIgnoreCase);
        }
    }
}

