using System;
using System.IO;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.TLK
{

    /// <summary>
    /// Auto-detection and convenience functions for TLK files.
    /// </summary>
    public static class TLKAuto
    {
        /// <summary>
        /// Writes the TLK data to the target location with the specified format (TLK, TLK_XML or TLK_JSON).
        /// </summary>
        public static void WriteTlk(TLK tlk, string target, ResourceType fileFormat)
        {
            if (fileFormat == ResourceType.TLK)
            {
                var writer = new TLKBinaryWriter(tlk);
                byte[] data = writer.Write();
                File.WriteAllBytes(target, data);
            }
            else
            {
                throw new ArgumentException("Unsupported format specified; use TLK or TLK_XML.");
            }
        }

        /// <summary>
        /// Returns the TLK data as a byte array.
        /// </summary>
        public static byte[] BytesTlk(TLK tlk, ResourceType fileFormat)
        {
            var writer = new TLKBinaryWriter(tlk);
            return writer.Write();
        }

        /// <summary>
        /// Returns the TLK data as a byte array (defaults to TLK format).
        /// </summary>
        public static byte[] BytesTlk(TLK tlk)
        {
            return BytesTlk(tlk, ResourceType.TLK);
        }

        /// <summary>
        /// Reads a TLK file from a file path or byte array.
        /// </summary>
        public static TLK ReadTlk(object source)
        {
            if (source is string filepath)
            {
                var reader = new TLKBinaryReader(filepath);
                return reader.Load();
            }
            if (source is byte[] data)
            {
                var reader = new TLKBinaryReader(data);
                return reader.Load();
            }
            throw new ArgumentException("Source must be string or byte[]");
        }
    }
}

