using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TEX
{
    internal static class RGB24Reader
    {
        public static Bitmap Read(Stream stream, int width, int height)
        {
            int len = width * height * 3;
            byte[] bytes = new byte[len];
            stream.Read(bytes, 0, len);
            byte[] pixel = new byte[4];
            pixel[3] = 255;
            byte[] data = new byte[width*height*4];
            for (int i = 0; i < len; i += 3)
            {
                pixel[0] = bytes[i + 2];
                pixel[1] = bytes[i + 1];
                pixel[2] = bytes[i];
                //pixel = new byte[] { bytes[i+2], bytes[i+1], bytes[i], 255 };
                Buffer.BlockCopy(pixel, 0, data, (i/3)*4, 4);
            }
            Bitmap canvas = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            canvas.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return canvas;
        }
    }
}
