using System;
using System.IO;

namespace lbx
{
    public class ArchiveContent
    {
        public string Name { get; }
        public string Description { get; }
        public byte[] Data { get; }
        public uint Offset { get; }

        public ArchiveContent(uint offset, string fName, string fDesc, byte[] data)
        {
            Offset = offset;
            Name = fName;
            Description = fDesc;
            Data = data;
        }

        internal void WriteData(string path)
        {
            File.WriteAllBytes(path, Data);
        }
    }
}