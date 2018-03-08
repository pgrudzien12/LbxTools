using Tool.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tool.Gfx
{
//    TGfxHeader =
//    packed record
//          Width, Height, Unknown1, BitmapCount, Unknown2, Unknown3,
//             Unknown4, PaletteInfoOffset, Unknown5: Word;
//  end;
    struct GfxHeader
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

            if (PaletteInfoOffset > 0)
            {
                reader.BaseStream.Seek(PaletteInfoOffset, SeekOrigin.Begin);
                paletteInfo = reader.ByteToType<GfxPaletteInfo>();
            }
            else
            {
                paletteInfo = new GfxPaletteInfo();
                paletteInfo.FirstPaletteColourIndex = 0;
                paletteInfo.PaletteColourCount = 255;
            }
            return paletteInfo;
        }
    }
}
