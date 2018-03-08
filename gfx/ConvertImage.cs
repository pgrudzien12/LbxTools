using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Tool.Core;

namespace Tool.Gfx
{
    public class ConvertImage : ICmdCommand
    {
        private string[] args;

        public ConvertImage(string[] args)
        {
            this.args = args;
        }

        public bool ValidateArgs => true;

        public void Execute()
        {
            GfxHeader header;
            string filename = args[0];
            using (var reader = new BinaryReader(File.OpenRead(filename), Encoding.Default, false))
            {
                header = reader.ByteToType<GfxHeader>();

                List<int> offsets = new List<int>();
                for (int i = 0; i <= header.BitmapCount; i++)
                {
                    offsets.Add(reader.ReadInt32());
                }
                GfxPaletteInfo paletteInfo = header.CreatePaletteInfo(reader);
                IPalette Palette = CreatePalette(header, reader, paletteInfo);

                using (Bitmap img = new Bitmap(header.Width, header.Height))
                    for (int i = 0; i < header.BitmapCount; i++)
                    {
                        var bmpStart = offsets[i];
                        var length = offsets[i + 1] - offsets[i];

                        reader.BaseStream.Seek(bmpStart, SeekOrigin.Begin);
                        byte[] imgBytes = reader.ReadBytes(length);

                        if (i == 0 && imgBytes[0] == 1)
                            ResetImage(header, img, Color.FromArgb(255, 0, 255));

                        int x = 0;
                        int bitmapIndex = 1;
                        int y = header.Height;
                        int next_ctl = 0;
                        int long_data = 0;
                        int n_r = 0;
                        int last_pos = 0;
                        int RLE_val;
                        int RleLength;
                        int RleCounter;
                        Color ColourValue;
                        while (x < header.Width && bitmapIndex < length)
                        {
                            y = 0;
                            if (imgBytes[bitmapIndex] == 0xff)
                            {
                                bitmapIndex++;

                                //{ Values of at least this indicate run length values }
                                RLE_val = paletteInfo.FirstPaletteColourIndex + paletteInfo.PaletteColourCount;
                            }
                            else
                            {
                                long_data = imgBytes[bitmapIndex + 2];
                                next_ctl = bitmapIndex + imgBytes[bitmapIndex + 1] + 2;

                                switch (imgBytes[bitmapIndex])
                                {
                                    case 0x0:
                                        RLE_val = paletteInfo.FirstPaletteColourIndex +
                                             paletteInfo.PaletteColourCount;
                                        break;
                                    case 0x80:
                                        RLE_val = 0xE0;
                                        break;
                                    default:
                                        throw new Exception("Unrecognized RLE value");
                                }

                                y = imgBytes[bitmapIndex + 3];
                                bitmapIndex += 4;

                                n_r = bitmapIndex;
                                while (n_r < next_ctl)
                                {
                                    while ((n_r < bitmapIndex + long_data) && (x < header.Width))
                                    {
                                        if (imgBytes[n_r] >= RLE_val)
                                        {
                                            last_pos = n_r + 1;
                                            RleLength = imgBytes[n_r] - RLE_val + 1;
                                            {
                                                if (RleLength + y > header.Height)
                                                    throw new Exception("RLE length overrun on y");
                                            }

                                            RleCounter = 0;
                                            while ((RleCounter < RleLength) && (y < header.Height))
                                            {
                                                if ((x < header.Width) && (y < header.Height) &&
                                                  (x >= 0) && (y >= 0))
                                                {
                                                    ColourValue = Palette[imgBytes[last_pos]];
                                                    if (ColourValue == Color.FromArgb(0xB4A0A0))
                                                        img.SetPixel(x, y, Color.FromArgb(0x00FF00));
                                                    else
                                                        img.SetPixel(x, y, ColourValue);
                                                }
                                                else
                                                    throw new Exception("RLE length overrun on output");

                                                y++;
                                                RleCounter++;
                                            }
                                            n_r += 2;
                                        }
                                        else
                                        {
                                            // { Regular single pixel }
                                            if ((x < header.Width) && (y < header.Height) &&
                                              (x >= 0) && (y >= 0))
                                            {
                                                ColourValue = Palette[imgBytes[n_r]];
                                                if (ColourValue == Color.FromArgb(0xB4A0A0))
                                                    img.SetPixel(x, y, Color.FromArgb(0x00FF00));
                                                else
                                                    img.SetPixel(x, y, ColourValue);
                                            }


                                            n_r++;
                                            y++;
                                        }
                                    }

                                    if (n_r < next_ctl)
                                    {
                                        //{
                                        //    On se trouve sur un autre RLE sur la ligne
                                        //                                      Some others data are here }
                                        y += imgBytes[n_r + 1];
                                        //{ next pos Y to write pixels }
                                        bitmapIndex = n_r + 2;
                                        long_data = imgBytes[n_r];
                                        //{ number of data to put }
                                        n_r += 2;

                                        //{
                                        //    if n_r >= next_ctl then
                                        //                                       throw new Exception('More RLE but lines too short');
                                        //}
                                    }
                                }

                                bitmapIndex = next_ctl; // { jump to next line }
                            }

                            x++;
                        }
                        img.Save($"{i}.bmp", ImageFormat.Bmp);
                        // save the bitmap!

                    }
            }

        }

        private static IPalette CreatePalette(GfxHeader header, BinaryReader reader, GfxPaletteInfo paletteInfo)
        {
            IPalette Palette;

            if (header.PaletteInfoOffset > 0)
            {
                //{ Read palette }
                Palette = ReadPalette(reader, paletteInfo);
            }
            else
            {
                Palette = new DefaultPalette();
            }

            return Palette;
        }

        private static IPalette ReadPalette(BinaryReader reader, GfxPaletteInfo paletteInfo)
        {
            IPalette Palette = new DefaultPalette();
            reader.BaseStream.Seek(paletteInfo.PaletteOffset, SeekOrigin.Begin);
            for (var ColourNo = 0; ColourNo < paletteInfo.PaletteColourCount; ColourNo++)
            {

                var paletteEntry = reader.ByteToType<GfxPaletteEntry>();

                paletteEntry.MultiplyBy(4);
                Palette[paletteInfo.FirstPaletteColourIndex + ColourNo] = paletteEntry.ToColor();
            }

            return Palette;
        }

        private static void ResetImage(GfxHeader header, Bitmap img, Color c)
        {
            for (int x = 0; x < header.Width; x++)
            {
                for (int y = 0; y < header.Height; y++)
                {
                    img.SetPixel(x, y, c);
                }
            }
        }

        public void PrintUsage()
        {
        }
    }
}