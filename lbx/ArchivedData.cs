namespace Tool.Lbx
{
    using System.IO;

    internal class ArchivedData
    {
        public ArchivedData(MemoryStream stream, int length)
        {
            this.Bytes = new byte[length];
            stream.Read(this.Bytes, 0, length);
        }

        public byte[] Bytes { get; }
    }
}