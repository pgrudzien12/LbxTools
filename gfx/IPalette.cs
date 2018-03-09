namespace Tool.Gfx
{
    using ImageSharp;

    internal interface IPalette
    {
        Rgba32 this[int i]
        {
            get;
            set;
        }
    }
}
