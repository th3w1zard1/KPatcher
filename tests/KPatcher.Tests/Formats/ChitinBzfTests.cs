using System;
using System.IO;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Common.LZMA;
using KPatcher.Core.Formats.Chitin;
using KPatcher.Core.Resources;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    public sealed class ChitinBzfTests
    {
        [Fact]
        public void WholeFileBzf_ChitinLoadsResourceBytes()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "kpatcher_bzf_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                byte[] payload = Encoding.ASCII.GetBytes("hello-from-bzf-chitin");
                byte[] bifBytes = BuildMinimalBif(0, ResourceType.TXT, payload);
                byte[] bzfBytes = BzfHelper.WrapWholeFile(bifBytes);

                string bifLogicalName = "data" + Path.DirectorySeparatorChar + "test.bif";
                string bzfPath = Path.Combine(tempRoot, Path.ChangeExtension(bifLogicalName, ".bzf"));
                Directory.CreateDirectory(Path.GetDirectoryName(bzfPath));
                File.WriteAllBytes(bzfPath, bzfBytes);

                string keyPath = Path.Combine(tempRoot, "chitin.key");
                File.WriteAllBytes(keyPath, BuildMinimalChitinKey(bifLogicalName, "testres", 0, ResourceType.TXT));

                var chitin = new Chitin(keyPath, tempRoot, Game.K1_IOS);
                chitin.Count.Should().Be(1);

                byte[] data = chitin.GetResource("testres", ResourceType.TXT);
                data.Should().Equal(payload);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Fact]
        public void PackedSegmentBzf_ChitinLoadsResourceBytes()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "kpatcher_packed_bzf_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                byte[] payload = Encoding.ASCII.GetBytes("packed-segment-bzf");
                byte[] compressedPayload = LzmaHelper.Compress(payload);
                byte[] bzfBytes = BuildPackedBzf(0, ResourceType.TXT, compressedPayload, payload.Length);

                string bifLogicalName = "data" + Path.DirectorySeparatorChar + "packed.bif";
                string bzfPath = Path.Combine(tempRoot, Path.ChangeExtension(bifLogicalName, ".bzf"));
                Directory.CreateDirectory(Path.GetDirectoryName(bzfPath));
                File.WriteAllBytes(bzfPath, bzfBytes);

                string keyPath = Path.Combine(tempRoot, "chitin.key");
                File.WriteAllBytes(keyPath, BuildMinimalChitinKey(bifLogicalName, "packed", 0, ResourceType.TXT));

                var chitin = new Chitin(keyPath, tempRoot, Game.K1_IOS);
                chitin.Count.Should().Be(1);

                byte[] data = chitin.GetResource("packed", ResourceType.TXT);
                data.Should().Equal(payload);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static byte[] BuildMinimalBif(uint resId, ResourceType restype, byte[] payload)
        {
            const int resourceOffset = 0x14;
            int dataOffset = resourceOffset + 16;

            using (var ms = new MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(Encoding.ASCII.GetBytes("BIFF"));
                writer.Write(Encoding.ASCII.GetBytes("V1  "));
                writer.Write((uint)1);
                writer.Write((uint)0);
                writer.Write((uint)resourceOffset);
                writer.Write(resId);
                writer.Write((uint)dataOffset);
                writer.Write((uint)payload.Length);
                writer.Write((uint)restype.TypeId);
                writer.Write(payload);
                return ms.ToArray();
            }
        }

        private static byte[] BuildPackedBzf(uint resId, ResourceType restype, byte[] compressedPayload, int uncompressedSize)
        {
            const int resourceOffset = 0x14;
            int dataOffset = resourceOffset + 16;
            int totalLength = dataOffset + compressedPayload.Length;

            using (var ms = new MemoryStream(totalLength))
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(Encoding.ASCII.GetBytes("BIFF"));
                writer.Write(Encoding.ASCII.GetBytes("V1  "));
                writer.Write((uint)1);
                writer.Write((uint)0);
                writer.Write((uint)resourceOffset);
                writer.Write(resId);
                writer.Write((uint)dataOffset);
                writer.Write((uint)uncompressedSize);
                writer.Write((uint)restype.TypeId);
                writer.Write(compressedPayload);
                return ms.ToArray();
            }
        }

        private static byte[] BuildMinimalChitinKey(string bifFilename, string resref, uint resId, ResourceType restype)
        {
            byte[] bifNameBytes = Encoding.ASCII.GetBytes(bifFilename);
            ushort bifNameLen = (ushort)bifNameBytes.Length;

            const int fileTableOffset = 64;
            int filenameOffset = fileTableOffset + 12;
            int keyOffset = filenameOffset + bifNameBytes.Length;
            int totalSize = keyOffset + 22;

            using (var ms = new MemoryStream(totalSize))
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(Encoding.ASCII.GetBytes("KEY "));
                writer.Write(Encoding.ASCII.GetBytes("V1  "));
                writer.Write((uint)1);
                writer.Write((uint)1);
                writer.Write((uint)fileTableOffset);
                writer.Write((uint)keyOffset);
                writer.Write(new byte[32]);

                writer.Seek(fileTableOffset, SeekOrigin.Begin);
                writer.Write((uint)0);
                writer.Write((uint)filenameOffset);
                writer.Write(bifNameLen);
                writer.Write((ushort)1);

                writer.Seek(filenameOffset, SeekOrigin.Begin);
                writer.Write(bifNameBytes);

                writer.Seek(keyOffset, SeekOrigin.Begin);
                byte[] resrefBytes = new byte[16];
                byte[] resrefAscii = Encoding.ASCII.GetBytes(resref);
                Array.Copy(resrefAscii, resrefBytes, Math.Min(16, resrefAscii.Length));
                writer.Write(resrefBytes);
                writer.Write((ushort)restype.TypeId);
                writer.Write(resId);

                return ms.ToArray();
            }
        }
    }
}
