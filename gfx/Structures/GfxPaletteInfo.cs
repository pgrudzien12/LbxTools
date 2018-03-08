using System;
using System.Collections.Generic;
using System.Text;

namespace Tool.Gfx
{
//    TGfxPaletteInfo =
//    packed record
//          PaletteOffset, FirstPaletteColourIndex, PaletteColourCount,
//             Unknown: Word;
//end;
    struct GfxPaletteInfo
    {
        public short PaletteOffset { get; set; }
        public short FirstPaletteColourIndex { get; set; }
        public short PaletteColourCount { get; set; }
        public short Unknown { get; set; }

    }
}
