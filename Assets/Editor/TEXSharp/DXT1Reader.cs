using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TEX
{
    internal static class DXT1Reader
    {
        public static Bitmap Read(Stream stream, int width, int height)
        {
            int blockCountX = (width + 3) / 4;
            int blockCountY = (height + 3) / 4;

            int len = blockCountX * blockCountY * 16 * 4;
            byte[] bytes = new byte[len];
            for (int z = 0; z < len; z++)
                bytes[z] = 0;

            int dataLen = blockCountX * blockCountY * 16;
            byte[] data = new byte[dataLen];
            stream.Read(data, 0, dataLen);

            byte[] pixel = new byte[4];
            int pos = 0;

            int temp;
            byte r0, g0, b0, r1, g1, b1;
            ushort c0, c1;
            uint code;

            int x, y, i, j;
            byte color;
            for (y = 0; y < blockCountY; y++)
            {
                for (x = 0; x < blockCountX; x++)
                {
                    c0 = (ushort)(data[pos + 0] | (data[pos + 1] << 8));
                    c1 = (ushort)(data[pos + 2] | (data[pos + 3] << 8));
                    
                    temp = (c0 >> 11) * 255 + 16;
                    r0 = (byte)((temp / 32 + temp) / 32);
                    temp = ((c0 & 0x07E0) >> 5) * 255 + 32;
                    g0 = (byte)((temp / 64 + temp) / 64);
                    temp = (c0 & 0x001F) * 255 + 16;
                    b0 = (byte)((temp / 32 + temp) / 32);

                    temp = (c1 >> 11) * 255 + 16;
                    r1 = (byte)((temp / 32 + temp) / 32);
                    temp = ((c1 & 0x07E0) >> 5) * 255 + 32;
                    g1 = (byte)((temp / 64 + temp) / 64);
                    temp = (c1 & 0x001F) * 255 + 16;
                    b1 = (byte)((temp / 32 + temp) / 32);
                    
                    code = (uint)(data[pos + 4] | (data[pos + 5] << 8) | (data[pos + 6] << 16) | (data[pos + 7] << 24));
                    
                    for (j = 0; j < 4; j++)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            if (x + i >= width)
                                continue;

                            color = (byte)((code >> 2 * (4 * j + i)) & 3);
                            
                            switch (color)
                            {
                                case 0:
                                    pixel[0] = b0;
                                    pixel[1] = g0;
                                    pixel[2] = r0;
                                    pixel[3] = 255;
                                    break;
                                case 1:
                                    pixel[0] = b1;
                                    pixel[1] = g1;
                                    pixel[2] = r1;
                                    pixel[3] = 255;
                                    break;
                                case 2:
                                    pixel[0] = (byte)((2 * b0 + b1) / 3);
                                    pixel[1] = (byte)((2 * g0 + g1) / 3);
                                    pixel[2] = (byte)((2 * r0 + r1) / 3);
                                    pixel[3] = 255;
                                    break;
                                case 3:
                                    pixel[0] = (byte)((b0 + 2 * b1) / 3);
                                    pixel[1] = (byte)((g0 + 2 * g1) / 3);
                                    pixel[2] = (byte)((r0 + 2 * r1) / 3);
                                    pixel[3] = 255;
                                    break;
                                default:
                                    pixel[0] = 0;
                                    pixel[1] = 0;
                                    pixel[2] = 0;
                                    pixel[3] = 0;
                                    break;
                            }
                    
                            Buffer.BlockCopy(pixel, 0, bytes, ((x * 4 * 4) + (i * 4) + (y * width * 4 * 4) + (j * width * 4)), 4);
                        }
                    }
                    pos += 8;
                }
            }

            Bitmap canvas = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0));
            canvas.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return canvas;
        }
    }
}
