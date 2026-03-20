using System;
using System.IO;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Formats.GFF
{

    /// <summary>
    /// Auto-detection and convenience functions for GFF files.
    /// </summary>
    public static class GFFAuto
    {
        /// <summary>
        /// Writes the GFF data to the target location with the specified format (GFF or GFF_XML).
        /// </summary>
        public static void WriteGff(GFF gff, string target, ResourceType fileFormat)
        {
            if (fileFormat.IsGff())
            {
                // Set content type from filename if not already set
                if (gff.Content == GFFContent.GFF && !string.IsNullOrEmpty(target))
                {
                    gff.Content = GFFContentExtensions.FromResName(target);
                }

                var writer = new GFFBinaryWriter(gff);
                byte[] data = writer.Write();
                File.WriteAllBytes(target, data);
            }
            else
            {
                throw new ArgumentException("Unsupported format specified; use GFF, GFF_XML, or GFF_JSON.");
            }
        }

        /// <summary>
        /// Returns the GFF data as a byte array.
        /// </summary>
        public static byte[] BytesGff(GFF gff, ResourceType fileFormat)
        {
            var writer = new GFFBinaryWriter(gff);
            return writer.Write();
        }
    }
}

