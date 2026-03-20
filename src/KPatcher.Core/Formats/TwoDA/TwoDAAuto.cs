using System;
using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.TwoDA
{

    /// <summary>
    /// Auto-detection and convenience functions for TwoDA files.
    /// </summary>
    public static class TwoDAAuto
    {
        /// <summary>
        /// Writes the TwoDA data to the target location with the specified format.
        /// </summary>
        public static void WriteTwoDA(TwoDA twoda, string target, ResourceType fileFormat)
        {
            if (fileFormat == ResourceType.TwoDA)
            {
                var writer = new TwoDABinaryWriter(twoda);
                byte[] data = writer.Write();
                File.WriteAllBytes(target, data);
            }
            else
            {
                throw new ArgumentException("Unsupported format specified; use TwoDA, TwoDA_CSV or TwoDA_JSON.");
            }
        }

        /// <summary>
        /// Returns the TwoDA data as a byte array.
        /// </summary>
        public static byte[] BytesTwoDA(TwoDA twoda, [CanBeNull] ResourceType fileFormat = null)
        {
            var writer = new TwoDABinaryWriter(twoda);
            return writer.Write();
        }
    }
}

