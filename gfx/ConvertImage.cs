namespace Tool.Gfx
{
    using ImageSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Tool.Core;

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
            string filename = this.args[0];
            using (var reader = new BinaryReader(File.OpenRead(filename), Encoding.Default, false))
            {
                header = reader.ByteToType<GfxHeader>();

                List<int> offsets = new List<int>();
                for (int i = 0; i <= header.BitmapCount; i++)
                {
                    offsets.Add(reader.ReadInt32());
                }

                GfxPaletteInfo paletteInfo = header.CreatePaletteInfo(reader);
                IPalette palette = CreatePalette(header, reader, paletteInfo);

                using (Image <Rgba32> img = new Image<Rgba32>(header.Width, header.Height))
                {
                    for (int i = 0; i < header.BitmapCount; i++)
                    {
                        var bmpStart = offsets[i];
                        var length = offsets[i + 1] - offsets[i];

                        reader.BaseStream.Seek(bmpStart, SeekOrigin.Begin);
                        byte[] imgBytes = reader.ReadBytes(length);

                        if (i == 0 && imgBytes[0] == 1)
                        {
                            ResetImage(header, img, new Rgba32(255, 0, 255));
                        }

                        int x = 0;
                        int bitmapIndex = 1;
                        int y = header.Height;
                        int next_ctl = 0;
                        int long_data = 0;
                        int n_r = 0;
                        int last_pos = 0;
                        int rle_val;
                        int rleLength;
                        int rleCounter;
                        Rgba32 colourCalue;
                        while (x < header.Width && bitmapIndex < length)
                        {
                            y = 0;
                            if (imgBytes[bitmapIndex] == 0xff)
                            {
                                bitmapIndex++;

                                // { Values of at least this indicate run length values }
                                rle_val = paletteInfo.FirstPaletteColourIndex + paletteInfo.PaletteColourCount;
                            }
                            else
                            {
                                long_data = imgBytes[bitmapIndex + 2];
                                next_ctl = bitmapIndex + imgBytes[bitmapIndex + 1] + 2;

                                switch (imgBytes[bitmapIndex])
                                {
                                    case 0x0:
                                        rle_val = paletteInfo.FirstPaletteColourIndex +
                                             paletteInfo.PaletteColourCount;
                                        break;
                                    case 0x80:
                                        rle_val = 0xE0;
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
                                        if (imgBytes[n_r] >= rle_val)
                                        {
                                            last_pos = n_r + 1;
                                            rleLength = imgBytes[n_r] - rle_val + 1;
                                            {
                                                if (rleLength + y > header.Height)
                                                {
                                                    throw new Exception("RLE length overrun on y");
                                                }
                                            }

                                            rleCounter = 0;
                                            while ((rleCounter < rleLength) && (y < header.Height))
                                            {
                                                if ((x < header.Width) && (y < header.Height) && (x >= 0) && (y >= 0))
                                                {
                                                    colourCalue = palette[imgBytes[last_pos]];
                                                    if (colourCalue == new Rgba32(0xFFB4A0A0))
                                                    {
                                                        img.GetPixelReference(x, y) = new Rgba32(0xFF00FF00);
                                                    }
                                                    else
                                                    {
                                                        img.GetPixelReference(x, y) = colourCalue;
                                                    }
                                                }
                                                else
                                                {
                                                    throw new Exception("RLE length overrun on output");
                                                }

                                                y++;
                                                rleCounter++;
                                            }

                                            n_r += 2;
                                        }
                                        else
                                        {
                                            // { Regular single pixel }
                                            if ((x < header.Width) && (y < header.Height) &&
                                              (x >= 0) && (y >= 0))
                                            {
                                                colourCalue = palette[imgBytes[n_r]];
                                                if (colourCalue == new Rgba32(0xFFB4A0A0))
                                                {
                                                    img.GetPixelReference(x, y) = new Rgba32(0xFF00FF00);
                                                }
                                                else
                                                {
                                                    img.GetPixelReference(x, y) = colourCalue;
                                                }
                                            }

                                            n_r++;
                                            y++;
                                        }
                                    }

                                    if (n_r < next_ctl)
                                    {
                                        // Some others data are here
                                        y += imgBytes[n_r + 1];

                                        // next pos Y to write pixels
                                        bitmapIndex = n_r + 2;
                                        long_data = imgBytes[n_r];

                                        // number of data to put
                                        n_r += 2;
                                    }
                                }

                                bitmapIndex = next_ctl; // jump to next line
                            }

                            x++;
                        }

                        img.Save($"{i}.bmp");
                    }
                }
            }
        }

        public void PrintUsage()
        {
        }

        private static IPalette CreatePalette(GfxHeader header, BinaryReader reader, GfxPaletteInfo paletteInfo)
        {
            IPalette palette;

            if (header.PaletteInfoOffset > 0)
            {
                palette = ReadPalette(reader, paletteInfo);
            }
            else
            {
                palette = new DefaultPalette();
            }

            return palette;
        }

        private static IPalette ReadPalette(BinaryReader reader, GfxPaletteInfo paletteInfo)
        {
            IPalette palette = new DefaultPalette();
            reader.BaseStream.Seek(paletteInfo.PaletteOffset, SeekOrigin.Begin);
            for (var i = 0; i < paletteInfo.PaletteColourCount; i++)
            {
                var paletteEntry = reader.ByteToType<GfxPaletteEntry>();

                paletteEntry.MultiplyBy(4);
                palette[paletteInfo.FirstPaletteColourIndex + i] = paletteEntry.ToColor();
            }

            return palette;
        }

        private static void ResetImage(GfxHeader header, Image<Rgba32> img, Rgba32 c)
        {
            for (int x = 0; x < header.Width; x++)
            {
                for (int y = 0; y < header.Height; y++)
                {
                    img.GetPixelReference(x, y) = c;
                }
            }
        }
    }
}