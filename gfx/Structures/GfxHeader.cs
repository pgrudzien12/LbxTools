namespace Tool.Gfx
{
    using System.IO;
    using Tool.Core;

    internal struct GfxHeader
    {
        public short Width { get; set; }

        public short Height { get; set; }

        public short Unknown1 { get; set; }

        public short BitmapCount { get; set; }

        public short Unknown2 { get; set; }

        public short Unknown3 { get; set; }

        public short Unknown4 { get; set; }

        public short PaletteInfoOffset { get; set; }

        public short Unknown5 { get; set; }

        internal GfxPaletteInfo CreatePaletteInfo(BinaryReader reader)
        {
            GfxPaletteInfo paletteInfo;

            if (this.PaletteInfoOffset > 0)
            {
                reader.BaseStream.Seek(this.PaletteInfoOffset, SeekOrigin.Begin);
                paletteInfo = reader.ByteToType<GfxPaletteInfo>();
            }
            else
            {
                paletteInfo = default(GfxPaletteInfo);
                paletteInfo.FirstPaletteColourIndex = 0;
                paletteInfo.PaletteColourCount = 255;
            }

            return paletteInfo;
        }
    }
}
