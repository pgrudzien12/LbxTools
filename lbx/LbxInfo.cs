using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lbx
{
    internal class LbxInfo
    {
        public ushort FileCount { get; private set; }
        public byte[] MagicString { get; private set; }
        public ushort UnusedValue { get; private set; }
        public List<ArchiveContent> ContentInfo { get; } = new List<ArchiveContent>();

        public uint EndOffset { get; private set; }
        internal static LbxInfo Load(string filename)
        {
            using (var stream = new MemoryStream(File.ReadAllBytes(filename)))
            {
                return LbxInfo.Read(stream);
            }
        }
        internal static LbxInfo Read(MemoryStream stream)
        {
            var result = new LbxInfo();
            using (var reader = new BinaryReader(stream, Encoding.Default, true))
            {
                result.FileCount = reader.ReadUInt16();
                result.MagicString = reader.ReadBytes(4);
                result.UnusedValue = reader.ReadUInt16();
                List<uint> offsets = ReadOffsets(result, reader);

                var firstFileOffset = offsets[0];
                const int fileNamesStartOffset = 512;
                for (int i = 0; i < result.FileCount; i++)
                {
                    var contentLength = (int)(offsets[i + 1] - offsets[i]);
                    var recordStartOffset = fileNamesStartOffset + i * 32;
                    var recordEndOffset = fileNamesStartOffset + (i + 1) * 32;
                    string fName, fDesc;

                    stream.Seek(recordStartOffset, SeekOrigin.Begin);
                    if (recordEndOffset < firstFileOffset)
                    {
                        fName = reader.ReadNullTerminatedFixedString(9);
                        fDesc = reader.ReadNullTerminatedFixedString(23);
                    }
                    else
                    {
                        fName = $"Unnamed_{i}";
                        fDesc = $"Description_{i}";
                    }

                    stream.Seek(offsets[i], SeekOrigin.Begin);
                    var bytes = new byte[contentLength];
                    stream.Read(bytes, 0, contentLength);

                    result.ContentInfo.Add(new ArchiveContent(offsets[i], fName, fDesc, bytes));
                }
            }
            return result;
        }

        private static List<uint> ReadOffsets(LbxInfo result, BinaryReader reader)
        {
            var offsets = new List<uint>();
            for (int i = 0; i < result.FileCount; i++)
            {
                offsets.Add(reader.ReadUInt32());
            }
            result.EndOffset = reader.ReadUInt32();
            offsets.Add(result.EndOffset);
            return offsets;
        }
    }
}