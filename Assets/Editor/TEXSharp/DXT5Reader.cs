using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TEX
{
    internal static class DXT5Reader
    {
        //https://github.com/Benjamin-Dobell/s3tc-dxt-decompression
        //todo: check out https://github.com/hglm/detex/blob/master/decompress-bc.c
        //(color is messed up but has 2x speed)
        public static Bitmap Read(Stream stream, int width, int height)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;

            int len = blockCountX * blockCountY * 16 * 4;
            byte[] bytes = new byte[len];
            for (int z = 0; z < len; z++)
                bytes[z] = 0;

            int dataLen = blockCountX * blockCountY * 16;
            byte[] data = new byte[dataLen];
            stream.Read(data, 0, dataLen);

            byte[] pixel = new byte[4];
            int pos = 0;

            int a0, a1;
            uint alpha1;
            ushort alpha2;
            uint colors, code;
            uint[] cr = new uint[4];
            uint[] cg = new uint[4];
            uint[] cb = new uint[4];
            int ca;

            int alphaIdx;
            uint pix;
            int alphaCode;

            int x, y, i;

            for (y = 0; y < blockCountY; y++)
            {
                for (x = 0; x < blockCountX; x++)
                {
                    a0 = data[pos];
                    a1 = data[pos + 1];

                    alpha1 = (uint)(data[pos + 4] | (data[pos + 5] << 8) | (data[pos + 6] << 16) | (data[pos + 7] << 24));
                    alpha2 = (ushort)(data[pos + 2] | (data[pos + 3] << 8));
                    //alpha = (ulong)(data[pos + 2] | (data[pos + 3] << 8) | (data[pos + 4] << 16) | (data[pos + 5] << 24) | (data[pos + 6] << 32) | (data[pos + 7] << 40));
                    colors = (uint)(data[pos + 8] | (data[pos + 9] << 8) | (data[pos + 10] << 16) | (data[pos + 11] << 24));
                    code = (uint)(data[pos + 12] | (data[pos + 13] << 8) | (data[pos + 14] << 16) | (data[pos + 15] << 24));

                    cb[0] = (colors & 0x0000001F) << 3;
                    cg[0] = (colors & 0x000007E0) >> (5 - 2);
                    cr[0] = (colors & 0x0000F800) >> (11 - 3);
                    cb[1] = (colors & 0x001F0000) >> (16 - 3);
                    cg[1] = (colors & 0x07E00000) >> (21 - 2);
                    cr[1] = (colors & 0xF8000000) >> (27 - 3);
                    cr[2] = DivisionTable.DivideBy3[(cr[0] << 1) + cr[1]];
                    cg[2] = DivisionTable.DivideBy3[(cg[0] << 1) + cg[1]];
                    cb[2] = DivisionTable.DivideBy3[(cb[0] << 1) + cb[1]];
                    cr[3] = DivisionTable.DivideBy3[cr[0] + (cr[1] << 1)];
                    cg[3] = DivisionTable.DivideBy3[cg[0] + (cg[1] << 1)];
                    cb[3] = DivisionTable.DivideBy3[cb[0] + (cb[1] << 1)];

                    for (i = 0; i < 16; i++)
                    {
                        pix = (code >> (i * 2)) & 0x3;
                        //alphaCode = (alpha >> (i * 3)) & 0x7;

                        //tried to use long here but it gave a weird output
                        alphaIdx = i * 3;
                        if (alphaIdx <= 12)
                            alphaCode = (alpha2 >> alphaIdx) & 7;
                        else if (alphaIdx == 15)
                            alphaCode = (int)((uint)(alpha2 >> 15) | ((alpha1 << 1) & 6));
                        else
                            alphaCode = (int)(alpha1 >> (alphaIdx - 16)) & 7;

                        if (a0 > a1)
                        {
                            switch (alphaCode)
                            {
                                case 0: ca = a0; break;
                                case 1: ca = a1; break;
                                case 2: ca = DivisionTable.DivideBy7[6 * a0 + 1 * a1]; break;
                                case 3: ca = DivisionTable.DivideBy7[5 * a0 + 2 * a1]; break;
                                case 4: ca = DivisionTable.DivideBy7[4 * a0 + 3 * a1]; break;
                                case 5: ca = DivisionTable.DivideBy7[3 * a0 + 4 * a1]; break;
                                case 6: ca = DivisionTable.DivideBy7[2 * a0 + 5 * a1]; break;
                                case 7: ca = DivisionTable.DivideBy7[1 * a0 + 6 * a1]; break;
                                default: ca = 0; break;
                            }
                        }
                        else
                        {
                            switch (alphaCode)
                            {
                                case 0: ca = a0; break;
                                case 1: ca = a1; break;
                                case 2: ca = DivisionTable.DivideBy5[4 * a0 + 1 * a1]; break;
                                case 3: ca = DivisionTable.DivideBy5[3 * a0 + 2 * a1]; break;
                                case 4: ca = DivisionTable.DivideBy5[2 * a0 + 3 * a1]; break;
                                case 5: ca = DivisionTable.DivideBy5[1 * a0 + 4 * a1]; break;
                                case 6: ca = 0; break;
                                case 7: ca = 0xFF; break;
                                default: ca = 0; break;
                            }
                        }
                        pixel[0] = (byte)cb[pix];
                        pixel[1] = (byte)cg[pix];
                        pixel[2] = (byte)cr[pix];
                        pixel[3] = (byte)ca;
                        Buffer.BlockCopy(pixel, 0, bytes, (x * 4 * 4) + (i % 4 * 4) + (y * width * 4 * 4) + ((i >> 2) * width * 4), 4);
                    }
                    pos += 16;
                }
            }

            Bitmap canvas = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0));
            canvas.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return canvas;
        }
    }
}
