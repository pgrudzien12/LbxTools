namespace Tool.Gfx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using ImageSharp;
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
                ImgSize size = new ImgSize(header.Width, header.Height);

                using (Image<Rgba32> img = new Image<Rgba32>(header.Width, header.Height))
                {
                    Painter painter = CreatePainter(img, reader, paletteInfo);

                    for (int i = 0; i < header.BitmapCount; i++)
                    {
                        var bmpStart = offsets[i];
                        var imgLength = offsets[i + 1] - offsets[i];

                        reader.BaseStream.Seek(bmpStart, SeekOrigin.Begin);
                        byte[] imgBytes = reader.ReadBytes(imgLength);

                        if (ShouldResetImage(i, imgBytes))
                        {
                            painter.ResetImage();
                        }

                        Decode(painter, size, imgBytes);

                        img.Save($"{i}.bmp");
                    }
                }
            }
        }

        private static void Decode(Painter painter, ImgSize size, byte[] imgBytes)
        {
            int bmpPointer = 1;
            for (int x = 0; x < size.Width; x++)
            {
                if (bmpPointer == imgBytes.Length)
                {
                    // we skip all the remaining lines
                    // that happens if lines from reset or previous image are the same as in this one
                    break;
                }

                int rle_val;
                if (imgBytes[bmpPointer] == skipMarker)
                {
                    bmpPointer++;
                    continue;
                }

                // RleHeader
                // +0 rle value
                // +1 next record (counting from the end of this record)
                // +2 long_data - the length of rle/single pixels values (paint instructions)
                // +3 y value
                int paintInstructionsLength = imgBytes[bmpPointer + 2];
                int next_ctl = (bmpPointer + 2) + imgBytes[bmpPointer + 1];
                int y = imgBytes[bmpPointer + 3];

                switch (imgBytes[bmpPointer])
                {
                    case 0x0:
                        rle_val = painter.DefaultRleVal;
                        break;
                    case 0x80:
                        rle_val = 0xE0;
                        break;
                    default:
                        throw new Exception("Unrecognized RLE value");
                }

                bmpPointer += 4;

                int n_r = bmpPointer;
                while (n_r < next_ctl)
                {
                    // here i cut (&& x < size.Width) which shouldn't happen
                    ExecutePaintInstructions(painter, size, imgBytes, bmpPointer, x, rle_val, paintInstructionsLength, ref y, ref n_r);

                    if (n_r < next_ctl)
                    {
                        UpdatePaintInstructions(imgBytes, ref bmpPointer, ref paintInstructionsLength, ref y, ref n_r);
                    }
                }

                bmpPointer = next_ctl; // jump to next line
            }
        }

        private static void UpdatePaintInstructions(byte[] imgBytes, ref int bmpPointer, ref int paintInstructionsLength, ref int y, ref int n_r)
        {
            // Some others data are here
            y += imgBytes[n_r + 1];

            // next pos Y to write pixels
            bmpPointer = n_r + 2;
            paintInstructionsLength = imgBytes[n_r];

            // number of data to put
            n_r += 2;
        }

        private static void ExecutePaintInstructions(Painter painter, ImgSize size, byte[] imgBytes, int bmpPointer, int x, int rle_val, int paintInstructionsLength, ref int y, ref int n_r)
        {
            while (n_r < bmpPointer + paintInstructionsLength)
            {
                if (imgBytes[n_r] >= rle_val)
                {
                    int last_pos = n_r + 1;
                    int rleLength = CalculateRleLength(size, imgBytes, y, rle_val, n_r);

                    PaintRle(painter, size, imgBytes, x, ref y, last_pos, rleLength);

                    n_r += 2;
                }
                else
                {
                    // { Regular single pixel }
                    if (size.InRange(x, y))
                    {
                        painter[x, y] = imgBytes[n_r];
                    }

                    n_r++;
                    y++;
                }
            }
        }

        private static int CalculateRleLength(ImgSize size, byte[] imgBytes, int y, int rle_val, int n_r)
        {
            int rleLength = imgBytes[n_r] - rle_val + 1;
            {
                if (rleLength + y > size.Height)
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

        private static void PaintRle(Painter painter, ImgSize size, byte[] imgBytes, int x, ref int y, int last_pos, int rleLength)
        {
            int rleCounter = 0;
            while ((rleCounter < rleLength) && (y < size.Height))
            {
                if (size.InRange(x, y))
                {
                    byte currentByte = imgBytes[last_pos];
                    painter[x, y] = currentByte;
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

        private static Painter CreatePainter(Image<Rgba32> image, BinaryReader reader, GfxPaletteInfo paletteInfo)
        {
            Painter painter = new Painter(image);

            if (paletteInfo.PaletteOffset > 0)
            {
                painter.ReadPalette(reader, paletteInfo);
            }

            painter.DefaultRleVal = paletteInfo.FirstPaletteColourIndex + paletteInfo.PaletteColourCount;
            return painter;
        }
    }
}