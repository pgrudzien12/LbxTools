namespace Tool.Lbx
{
    using System.IO;

    public class ArchiveContent
    {
        public string Name { get; }

        public string Description { get; }

        public byte[] Data { get; }

        public uint Offset { get; }

        public ArchiveContent(uint offset, string fName, string fDesc, byte[] data)
        {
            this.Offset = offset;
            this.Name = fName;
            this.Description = fDesc;
            this.Data = data;
        }

        internal void WriteData(string path)
        {
            File.WriteAllBytes(path, this.Data);
        }
    }
}