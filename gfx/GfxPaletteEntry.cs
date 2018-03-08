using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Tool.Gfx
{
    struct GfxPaletteEntry
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        internal Color ToColor()
        {
            return Color.FromArgb(R, G, B);
        }

        internal void MultiplyBy(int v)
        {
            R = (byte)(R * v);
            G = (byte)(G * v);
            B = (byte)(B * v);
        }
    }
}
