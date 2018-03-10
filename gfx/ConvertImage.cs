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
        private const int skipMarker = 0xff;
        private string[] args;

        public ConvertImage(string[] args)
        {
            this.args = args;
        }

        public bool ValidateArgs => true;

        public void Execute()
        {
            string filename = this.args[0];
            using (var stream = File.OpenRead(filename))
            {
                ConvertToImage(stream);
            }
        }

        private static void ConvertToImage(FileStream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.Default, false))
            {
                GfxHeader header = reader.ByteToType<GfxHeader>();

                List<int> offsets = new List<int>();
                for (int i = 0; i <= header.BitmapCount; i++)
                {
                    offsets.Add(reader.ReadInt32());
                }

                GfxPaletteInfo paletteInfo = header.CreatePaletteInfo(reader);
                IPalette palette = CreatePalette(header, reader, paletteInfo);

                using (Image<Rgba32> img = new Image<Rgba32>(header.Width, header.Height))
                {
                    for (int i = 0; i < header.BitmapCount; i++)
                    {
                        var bmpStart = offsets[i];
                        var imgLength = offsets[i + 1] - offsets[i];

                        reader.BaseStream.Seek(bmpStart, SeekOrigin.Begin);
                        byte[] imgBytes = reader.ReadBytes(imgLength);

                        if (ShouldResetImage(i, imgBytes))
                        {
                            ResetImage(img);
                        }

                        Decode(paletteInfo, palette, img, imgBytes);

                        img.Save($"{i}.bmp");
                    }
                }
            }
        }

        private static void Decode(GfxPaletteInfo paletteInfo, IPalette palette, Image<Rgba32> img, byte[] imgBytes)
        {
            int bmpPointer = 1;
            int x = 0;
            while (x < img.Width && bmpPointer < imgBytes.Length)
            {
                int y = 0;
                int rle_val;
                if (imgBytes[bmpPointer] == skipMarker)
                {
                    bmpPointer++;
                    x++;
                    continue;
                }

                int long_data = imgBytes[bmpPointer + 2];
                int next_ctl = bmpPointer + imgBytes[bmpPointer + 1] + 2;

                switch (imgBytes[bmpPointer])
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

                y = imgBytes[bmpPointer + 3];
                bmpPointer += 4;

                int n_r = bmpPointer;
                while (n_r < next_ctl)
                {
                    while ((n_r < bmpPointer + long_data) && (x < img.Width))
                    {
                        if (imgBytes[n_r] >= rle_val)
                        {
                            int last_pos = n_r + 1;
                            int rleLength = CalculateRleLength(img, imgBytes, y, rle_val, n_r);

                            PaintRle(palette, img, imgBytes, x, ref y, last_pos, rleLength);

                            n_r += 2;
                        }
                        else
                        {
                            // { Regular single pixel }
                            if ((x < img.Width) && (y < img.Height) &&
                              (x >= 0) && (y >= 0))
                            {
                                Rgba32 colourCalue = palette[imgBytes[n_r]];
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
                        bmpPointer = n_r + 2;
                        long_data = imgBytes[n_r];

                        // number of data to put
                        n_r += 2;
                    }
                }

                bmpPointer = next_ctl; // jump to next line

                x++;
            }
        }

        private static int CalculateRleLength(Image<Rgba32> img, byte[] imgBytes, int y, int rle_val, int n_r)
        {
            int rleLength = imgBytes[n_r] - rle_val + 1;
            {
                if (rleLength + y > img.Height)
                {
                    throw new Exception("RLE length overrun on y");
                }
            }

            return rleLength;
        }

        private static bool ShouldResetImage(int i, byte[] imgBytes)
        {
            return i == 0 && imgBytes[0] == 1;
        }

        private static void PaintRle(IPalette palette, Image<Rgba32> img, byte[] imgBytes, int x, ref int y, int last_pos, int rleLength)
        {
            Rgba32 colourCalue;

            int rleCounter = 0;
            while ((rleCounter < rleLength) && (y < img.Height))
            {
                if ((x < img.Width) && (y < img.Height) && (x >= 0) && (y >= 0))
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

        private static void ResetImage(Image<Rgba32> img)
        {
            Rgba32 resetColor = new Rgba32(255, 0, 255);
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    img.GetPixelReference(x, y) = resetColor;
                }
            }
        }
    }
}