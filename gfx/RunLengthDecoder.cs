namespace Tool.Gfx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using ImageSharp;
    using Tool.Core;

    public class RunLengthDecoder
    {
        private const int SkipMarker = 0xff;

        public static IEnumerable<Image<Rgba32>> ConvertToBmps(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead || stream.CanSeek)
            {
                throw new ArgumentException($"Argument '{nameof(stream)}' must support reading and seeking");
            }

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

                        yield return new Image<Rgba32>(img);
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
                if (imgBytes[bmpPointer] == SkipMarker)
                {
                    bmpPointer++;
                    continue;
                }

                // RleHeader
                // +0 rle value
                // +1 next record (counting from the end of this record)
                // +2 long_data - the length of rle/single pixels values (paint instructions)
                // +3 y value
                int rle_indicator = imgBytes[bmpPointer];
                int paintInstructionsLength = imgBytes[bmpPointer + 2];
                int next_ctl = (bmpPointer + 2) + imgBytes[bmpPointer + 1];
                int y = imgBytes[bmpPointer + 3];

                switch (rle_indicator)
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
                    while (n_r < bmpPointer + paintInstructionsLength)
                    {
                        ExecutePaintInstruction(painter, size, imgBytes, x, rle_val, paintInstructionsLength, ref y, ref n_r);
                    }

                    if (n_r < next_ctl)
                    {
                        UpdatePaintInstruction(imgBytes, ref bmpPointer, ref paintInstructionsLength, ref y, ref n_r);
                    }
                }

                bmpPointer = next_ctl; // jump to next line
            }
        }

        private static void UpdatePaintInstruction(byte[] imgBytes, ref int bmpPointer, ref int paintInstructionsLength, ref int y, ref int n_r)
        {
            // Some others data are here
            y += imgBytes[n_r + 1];

            // next pos Y to write pixels
            bmpPointer = n_r + 2;
            paintInstructionsLength = imgBytes[n_r];

            // number of data to put
            n_r += 2;
        }

        private static void ExecutePaintInstruction(Painter painter, ImgSize size, byte[] imgBytes, int x, int rle_val, int paintInstructionsLength, ref int y, ref int n_r)
        {
            if (imgBytes[n_r] >= rle_val)
            {
                // { Run Length Encoding }
                int last_pos = n_r + 1;
                int rleLength = CalculateRleLength(size, imgBytes, y, rle_val, n_r);

                y += PaintRle(painter, size, imgBytes, x, y, last_pos, rleLength);

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

        private static int PaintRle(Painter painter, ImgSize size, byte[] imgBytes, int x, int y, int last_pos, int rleLength)
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

            return rleCounter;
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