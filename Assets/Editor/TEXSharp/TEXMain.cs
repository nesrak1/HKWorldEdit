using System.Drawing;
using System.IO;

namespace TEX
{
    public static class TEXMain
    {
        public static Bitmap ReadRGB24(Stream stream, int width, int height)
        {
            return RGB24Reader.Read(stream, width, height);
        }
        public static Bitmap ReadRGB24(byte[] data, int width, int height)
        {
            return RGB24Reader.Read(new MemoryStream(data), width, height);
        }
        public static Bitmap ReadRGBA32(Stream stream, int width, int height)
        {
            return RGBA32Reader.Read(stream, width, height);
        }
        public static Bitmap ReadRGBA32(byte[] data, int width, int height)
        {
            return RGBA32Reader.Read(new MemoryStream(data), width, height);
        }
        public static Bitmap ReadDXT1(Stream stream, int width, int height)
        {
            return DXT1Reader.Read(stream, width, height);
        }
        public static Bitmap ReadDXT1(byte[] data, int width, int height)
        {
            return DXT1Reader.Read(new MemoryStream(data), width, height);
        }
        public static Bitmap ReadDXT5(Stream stream, int width, int height)
        {
            return DXT5Reader.Read(stream, width, height);
        }
        public static Bitmap ReadDXT5(byte[] data, int width, int height)
        {
            return DXT5Reader.Read(new MemoryStream(data), width, height);
        }
    }
}
