using System;
using System.IO;
using JetBrains.Annotations;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.SSF
{

    /// <summary>
    /// Auto-detection and convenience functions for SSF files.
    /// </summary>
    public static class SSFAuto
    {
        /// <summary>
        /// Writes the SSF data to the target location with the specified format (SSF or SSF_XML).
        /// </summary>
        public static void WriteSsf(SSF ssf, string target, ResourceType fileFormat)
        {
            if (fileFormat == ResourceType.SSF)
            {
                var writer = new SSFBinaryWriter(ssf);
                byte[] data = writer.Write();
                File.WriteAllBytes(target, data);
            }
            else
            {
                throw new ArgumentException("Unsupported format specified; use SSF or SSF_XML.");
            }
        }

        /// <summary>
        /// Returns the SSF data as a byte array.
        /// </summary>
        public static byte[] BytesSsf(SSF ssf, [CanBeNull] ResourceType fileFormat = null)
        {
            var writer = new SSFBinaryWriter(ssf);
            return writer.Write();
        }

        /// <summary>
        /// Reads an SSF file from a file path or byte array.
        /// </summary>
        public static SSF ReadSsf(object source)
        {
            if (source is string filepath)
            {
                var reader = new SSFBinaryReader(filepath);
                return reader.Load();
            }
            if (source is byte[] data)
            {
                var reader = new SSFBinaryReader(data);
                return reader.Load();
            }
            throw new ArgumentException("Source must be string or byte[]");
        }
    }
}

