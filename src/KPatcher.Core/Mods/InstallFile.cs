using System;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;

namespace KPatcher.Core.Mods
{

    /// <summary>
    /// Represents a file to be installed/copied during patching.
    /// </summary>
    public class InstallFile : PatcherModifications
    {
        public InstallFile(
            string filename,
            bool? replaceExisting = null,
            [CanBeNull] string destination = null)
            : base(filename, replaceExisting, destination)
        {
            Action = "Copy ";
            SkipIfNotReplace = true;
        }

        /// <summary>
        /// HACK(th3w1zard1): organize this into PatcherModifications class later, this is only used for nwscript.nss currently.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Destination, SaveAs, ReplaceFile);
        }

        public override object PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            Apply(source, memory, logger, game);
            return source;
        }

        public override void Apply(
            object mutableData,
            PatcherMemory memory,
            PatchLogger logger,
            Game game)
        {
            // InstallFile doesn't modify the file, just copies it
        }
    }
}
