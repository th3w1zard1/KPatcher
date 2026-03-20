using System;
using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.RIM
{

    /// <summary>
    /// Auto-detection and convenience functions for RIM files.
    /// </summary>
    public static class RIMAuto
    {
        /// <summary>
        /// Returns an RIM instance from the source.
        /// </summary>
        public static RIM ReadRim(string source, int offset = 0, int size = 0)
        {
            return new RIMBinaryReader(source).Load();
        }

        /// <summary>
        /// Returns an RIM instance from byte data.
        /// </summary>
        public static RIM ReadRim(byte[] data, int offset = 0, int size = 0)
        {
            return new RIMBinaryReader(data).Load();
        }

        /// <summary>
        /// Writes the RIM data to the target location with the specified format (RIM only).
        /// </summary>
        public static void WriteRim(RIM rim, string target, ResourceType fileFormat)
        {
            if (fileFormat == ResourceType.RIM)
            {
                var writer = new RIMBinaryWriter(rim);
                byte[] data = writer.Write();
                File.WriteAllBytes(target, data);
            }
            else
            {
                throw new ArgumentException("Unsupported format specified; use RIM.");
            }
        }

        /// <summary>
        /// Returns the RIM data as a byte array.
        /// </summary>
        public static byte[] BytesRim(RIM rim, [CanBeNull] ResourceType fileFormat = null)
        {
            var writer = new RIMBinaryWriter(rim);
            return writer.Write();
        }
    }
}

