namespace Tool.Gfx
{
    using ImageSharp;

    internal struct GfxPaletteEntry
    {
        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        internal Rgba32 ToColor()
        {
            return new Rgba32(R, G, B);
        }

        internal void MultiplyBy(int v)
        {
            R = (byte)(R * v);
            G = (byte)(G * v);
            B = (byte)(B * v);
        }
    }
}
