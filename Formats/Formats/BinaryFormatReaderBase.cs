using System.IO;
using System.Text;
using Andastra.Formats;
using JetBrains.Annotations;

namespace Andastra.Formats.Formats
{

    /// <summary>
    /// Base class for binary format readers to eliminate duplicate constructor patterns.
    /// </summary>
    public abstract class BinaryFormatReaderBase
    {
        protected readonly byte[] Data;
        protected readonly Andastra.Formats.BinaryReader Reader;

        protected BinaryFormatReaderBase(byte[] data, [CanBeNull] Encoding encoding = null)
        {
            Data = data;
            Reader = Andastra.Formats.BinaryReader.FromBytes(data, 0, null);
        }

        protected BinaryFormatReaderBase(string filepath, [CanBeNull] Encoding encoding = null)
        {
            Data = File.ReadAllBytes(filepath);
            Reader = Andastra.Formats.BinaryReader.FromBytes(Data, 0, null);
        }

        protected BinaryFormatReaderBase(Stream source, [CanBeNull] Encoding encoding = null)
        {
            using (var ms = new MemoryStream())
            {
                source.CopyTo(ms);
                Data = ms.ToArray();
                Reader = Andastra.Formats.BinaryReader.FromBytes(Data, 0, null);
            }
        }
    }
}

