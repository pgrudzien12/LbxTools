using System;
using System.IO;
using System.Text;

namespace lbx
{
    internal class ArchivedData
    {
        public ArchivedData(MemoryStream stream, int length)
        {
            Bytes = new byte[length];
            stream.Read(Bytes, 0, length);
        }

        public byte[] Bytes { get; }
    }
}