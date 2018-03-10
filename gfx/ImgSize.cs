namespace Tool.Gfx
{
    internal class ImgSize
    {
        private short width;
        private short height;

        public ImgSize(short width, short height)
        {
            this.Width = width;
            this.Height = height;
        }

        public short Width { get => this.width; set => this.width = value; }
        public short Height { get => this.height; set => this.height = value; }

        public bool InRange(int x, int y)
        {
            return (x < this.Width) && (y < this.Height) && (x >= 0) && (y >= 0);
        }
    }
}