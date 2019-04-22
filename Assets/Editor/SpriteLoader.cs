using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TEX;
using UnityEngine;
using UnityEngine.Profiling;
using Graphics = System.Drawing.Graphics;

public class SpriteLoader
{
    private Dictionary<AssetID, Bitmap> bitmapPool = new Dictionary<AssetID, Bitmap>();
    public Texture2D LoadUnitySprite(AssetsManager manager, AssetTypeValueField baseField, AssetsFileInstance spriteFileInst)
    {
        AssetTypeValueField m_RD = baseField.Get("m_RD");
        AssetTypeValueField texture = m_RD.Get("texture");
        AssetTypeValueField textureRect = m_RD.Get("textureRect");
        int texFileId = texture.Get("m_FileID").GetValue().AsInt();
        long texPathId = texture.Get("m_PathID").GetValue().AsInt64();
        AssetTypeValueField textureBaseField = manager.GetExtAsset(spriteFileInst, texture).instance.GetBaseField();
        int x = (int)Mathf.Floor(textureRect.Get("x").GetValue().AsFloat());
        int y = (int)Mathf.Floor(textureRect.Get("y").GetValue().AsFloat());
        int width = (int)Mathf.Ceil(textureRect.Get("width").GetValue().AsFloat());
        int height = (int)Mathf.Ceil(textureRect.Get("height").GetValue().AsFloat());

        Bitmap bitmap = GetBitmap(manager, textureBaseField, texFileId, texPathId, spriteFileInst);

        List<Point> points = GetUnityPoints(baseField, bitmap.Height);
        //because the section we are selecting has to be at 0,
        //we move the sprite from the bottom to the top
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Point(points[i].X, points[i].Y - (bitmap.Height - height));
        }
        AssetTypeValueField m_IndexBuffer = m_RD.Get("m_IndexBuffer").Get("Array");

        GraphicsPath gp = new GraphicsPath();
        for (uint i = 0; i < m_IndexBuffer.GetValue().AsArray().size; i += 6)
        {
            int pointA = (int)(m_IndexBuffer[i + 0].GetValue().AsUInt() | (m_IndexBuffer[i + 1].GetValue().AsUInt() << 8));
            int pointB = (int)(m_IndexBuffer[i + 2].GetValue().AsUInt() | (m_IndexBuffer[i + 3].GetValue().AsUInt() << 8));
            int pointC = (int)(m_IndexBuffer[i + 4].GetValue().AsUInt() | (m_IndexBuffer[i + 5].GetValue().AsUInt() << 8));
            Point[] triangle = new Point[] { points[pointA], points[pointB], points[pointC] };
            gp.AddPolygon(triangle);
        }

        //todo, handle all this in unity (needs algo for clipping in poly)
        Bitmap croppedBitmap = new Bitmap(width, height);
        using (Graphics graphics = Graphics.FromImage(croppedBitmap))
        {
            graphics.Clip = new Region(gp);
            graphics.DrawImage(bitmap, -x, -(bitmap.Height - (y + height)));
        }
        //o nose
        croppedBitmap = ResizeImage(croppedBitmap, (int)(croppedBitmap.Width * 1.5625f), (int)(croppedBitmap.Height * 1.5625f));
        //bitmap.Dispose();
        //this is terribly inefficent please o please fix
        Texture2D image = new Texture2D(width, height);
        using (MemoryStream stream = new MemoryStream())
        {
            croppedBitmap.Save(stream, croppedBitmap.RawFormat);
            image.LoadImage(stream.ToArray());
        }
        return image;
    }
    //this is not the original version found in hkworldview and this one's probably ten times slower
    public Texture2D LoadTK2dSprite(AssetsManager manager, AssetTypeValueField baseField, AssetsFileInstance spriteFileInst, int spriteId)
    {
        AssetTypeValueField spriteDefinitions = baseField.Get("spriteDefinitions");
        
        AssetTypeValueField textures = baseField.Get("textures");
        AssetTypeValueField texture = textures[0];
        int texFileId = texture.Get("m_FileID").GetValue().AsInt();
        long texPathId = texture.Get("m_PathID").GetValue().AsInt64();
        AssetTypeValueField textureBaseField = manager.GetExtAsset(spriteFileInst, texture).instance.GetBaseField();

        Bitmap bitmap = GetBitmap(manager, textureBaseField, texFileId, texPathId, spriteFileInst);
        
        AssetTypeValueField spriteDefinition = spriteDefinitions[(uint)spriteId];
        AssetTypeValueField uvs = spriteDefinition.Get("uvs");

        double xn = int.MaxValue,
               xp = 0,
               yn = int.MaxValue,
               yp = 0;
        for (uint i = 0; i < 4; i++)
        {
            AssetTypeValueField uv = uvs[i];
            double uv_x = Math.Round(uv.Get("x").GetValue().AsFloat() * bitmap.Width);
            double uv_y = bitmap.Height - Math.Round(uv.Get("y").GetValue().AsFloat() * bitmap.Height);
            if (uv_x < xn)
                xn = uv_x;
            if (uv_x > xp)
                xp = uv_x;
            if (uv_y < yn)
                yn = uv_y;
            if (uv_y > yp)
                yp = uv_y;
        }

        int x = (int)xn;
        int y = (int)yn;
        int width = (int)(xp - xn);
        int height = (int)(yp - yn);

        Bitmap croppedBitmap = new Bitmap(width, height);
        using (Graphics graphics = Graphics.FromImage(croppedBitmap))
        {
            graphics.DrawImage(bitmap, -x, -y);
        }

        if (spriteDefinition.Get("flipped").GetValue().AsInt() == 1)
            croppedBitmap.RotateFlip(RotateFlipType.Rotate270FlipX);

        croppedBitmap = ResizeImage(croppedBitmap, (int)(croppedBitmap.Width * 1.5625f), (int)(croppedBitmap.Height * 1.5625f));

        Texture2D image = new Texture2D(width, height);
        using (MemoryStream stream = new MemoryStream())
        {
            croppedBitmap.Save(stream, croppedBitmap.RawFormat);
            image.LoadImage(stream.ToArray());
        }
        return image;
    }
    public Texture2D LoadTK2dSpriteNative(AssetsManager manager, AssetTypeValueField baseField, AssetsFileInstance spriteFileInst, int spriteId)
    {
        AssetTypeValueField spriteDefinitions = baseField.Get("spriteDefinitions");
        
        AssetTypeValueField textures = baseField.Get("textures");
        AssetTypeValueField texture = textures[0];
        int texFileId = texture.Get("m_FileID").GetValue().AsInt();
        long texPathId = texture.Get("m_PathID").GetValue().AsInt64();
        AssetTypeValueField textureBaseField = manager.GetExtAsset(spriteFileInst, texture).instance.GetBaseField();

        Bitmap bitmap = GetBitmapNative(manager, textureBaseField, texFileId, texPathId, spriteFileInst);
        
        AssetTypeValueField spriteDefinition = spriteDefinitions[(uint)spriteId];
        AssetTypeValueField uvs = spriteDefinition.Get("uvs");

        double xn = int.MaxValue,
               xp = 0,
               yn = int.MaxValue,
               yp = 0;
        for (uint i = 0; i < 4; i++)
        {
            AssetTypeValueField uv = uvs[i];
            double uv_x = Math.Round(uv.Get("x").GetValue().AsFloat() * bitmap.Width);
            double uv_y = bitmap.Height - Math.Round(uv.Get("y").GetValue().AsFloat() * bitmap.Height);
            if (uv_x < xn)
                xn = uv_x;
            if (uv_x > xp)
                xp = uv_x;
            if (uv_y < yn)
                yn = uv_y;
            if (uv_y > yp)
                yp = uv_y;
        }

        int x = (int)xn;
        int y = (int)yn;
        int width = (int)(xp - xn);
        int height = (int)(yp - yn);

        Bitmap croppedBitmap = new Bitmap(width, height);
        using (Graphics graphics = Graphics.FromImage(croppedBitmap))
        {
            graphics.DrawImage(bitmap, -x, -y);
        }

        if (spriteDefinition.Get("flipped").GetValue().AsInt() == 1)
            croppedBitmap.RotateFlip(RotateFlipType.Rotate270FlipX);

        croppedBitmap = ResizeImage(croppedBitmap, (int)(croppedBitmap.Width * 1.5625f), (int)(croppedBitmap.Height * 1.5625f));

        Texture2D image = new Texture2D(width, height);
        using (MemoryStream stream = new MemoryStream())
        {
            croppedBitmap.Save(stream, croppedBitmap.RawFormat);
            image.LoadImage(stream.ToArray());
        }
        return image;
    }
    public static Bitmap ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        image.Dispose();

        return destImage;
    }
    public List<Point> GetUnityPoints(AssetTypeValueField baseField, int imageHeight)
    {
        AssetTypeValueField m_RD = baseField.Get("m_RD");
        List<Point> points = new List<Point>();
        List<PointF> tempPoints = new List<PointF>();
        //step 1: get the points from m_DataSize
        //we'll follow m_Channels just in case
        //but it should always be verts and uv1
        AssetTypeValueField m_VertexData = m_RD.Get("m_VertexData");
        AssetTypeValueField m_Channels = m_VertexData.Get("m_Channels").Get("Array");
        uint channelsSize = m_Channels.GetValue().AsArray().size;
        int skipSize = 0;
        //start at 1 to skip verts
        for (uint i = 1; i < channelsSize; i++)
        {
            skipSize += (int)m_Channels.Get(i).Get("dimension").GetValue().AsUInt();
        }
        byte[] m_DataSize = GetByteData(m_VertexData.Get("m_DataSize"));
        uint m_VertexCount = m_VertexData.Get("m_VertexCount").GetValue().AsUInt();
        int pos = 0;
        for (uint i = 0; i < m_VertexCount; i++)
        {
            float x = ReadFloat(m_DataSize, pos);
            float y = ReadFloat(m_DataSize, pos + 4);
            tempPoints.Add(new PointF(x, y));
            pos += 12 + (skipSize * 4); //go past verts (vector3, not 2), then any remaining channel's data
        }
        //step 2: calculate the midpoint/centroid todo: not needed please remove
        float xTotal = 0;
        float yTotal = 0;
        foreach (PointF point in tempPoints)
        {
            xTotal += point.X;
            yTotal += point.Y;
        }
        float xMid = xTotal / m_VertexCount;
        float yMid = yTotal / m_VertexCount;
        //step 3: scale from the midpoint by m_PixelsToUnits
        float m_PixelsToUnits = baseField.Get("m_PixelsToUnits").GetValue().AsFloat();
        for (int i = 0; i < tempPoints.Count; i++)
        {
            PointF point = tempPoints[i];
            float offsetX = (point.X - xMid) * m_PixelsToUnits;
            float offsetY = (point.Y - yMid) * m_PixelsToUnits;
            tempPoints[i] = new PointF(point.X + offsetX, point.Y + offsetY);
        }
        //step 4: shift to textureRect start
        //we need to get the lowest x and y
        //of all of the points and place an
        //imaginary point there, then we need
        //to move that point to textureRect x and y
        float smallestX = float.MaxValue;
        float smallestY = float.MaxValue;
        foreach (PointF point in tempPoints)
        {
            if (point.X < smallestX)
                smallestX = point.X;
            if (point.Y < smallestY)
                smallestY = point.Y;
        }
        AssetTypeValueField textureRect = m_RD.Get("textureRect");
        float rectX = textureRect.Get("x").GetValue().AsFloat();
        float rectY = textureRect.Get("y").GetValue().AsFloat();
        float shiftX = 0 - smallestX;
        float shiftY = 0 - smallestY;

        for (int i = 0; i < tempPoints.Count; i++)
        {
            PointF point = tempPoints[i];
            point.X += shiftX;
            point.Y += shiftY;
            //step 5: flip all points by y
            //unity's canvas starts with the bottom y = 0
            //where the bitmap starts top y = 0
            point.Y = imageHeight - point.Y;
            //convert float to int (should be whole numbers by now anyway)
            points.Add(new Point((int)point.X, (int)point.Y));
        }
        return points;
    }
    private Bitmap GetBitmap(AssetsManager manager, AssetTypeValueField baseField, int fileId, long pathId, AssetsFileInstance fromInst)
    {
        AssetID assetTest = new AssetID(fromInst, fileId, pathId);
        if (bitmapPool.ContainsKey(assetTest))
            return bitmapPool[assetTest];

        //Profiler.BeginSample("Bitmap load");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int m_Width = baseField.Get("m_Width").GetValue().AsInt();
        int m_Height = baseField.Get("m_Height").GetValue().AsInt();
        int m_TextureFormat = baseField.Get("m_TextureFormat").GetValue().AsInt();
        if (m_TextureFormat != 3 && m_TextureFormat != 4 && m_TextureFormat != 10 && m_TextureFormat != 12)
            return SystemIcons.Exclamation.ToBitmap();
            //throw new Exception("TEX doesn't support format " + m_TextureFormat + " please contact nes");

        byte[] data = null;
        AssetTypeValueField m_StreamData = baseField.Get("m_StreamData");
        int offset = (int)m_StreamData.Get("offset").GetValue().AsUInt();
        int size = (int)m_StreamData.Get("size").GetValue().AsUInt();
        //check if texture is in resS (most likely is)
        if (size != 0)
        {
            string path = m_StreamData.Get("path").GetValue().AsString();
            string fullPath = Path.Combine(Path.GetDirectoryName(fromInst.path), path);
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream memStream = new MemoryStream())
            {
                long fileSize = stream.Length;
                data = new byte[size];
                stream.Position = offset;

                int bytesRead;
                var buffer = new byte[2048];
                while (((bytesRead = stream.Read(buffer, 0, 2048)) > 0) && (stream.Position < offset + size))
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                data = memStream.ToArray();
            }
        }
        else
        {
            data = GetByteData(baseField.Get("image data"));
        }

        Bitmap bitmap = null;
        
        //todo handle with unity, possibly Texture2D.CreateExternalTexture
        if (m_TextureFormat == 3)
        {
            bitmap = TEXMain.ReadRGB24(data, m_Width, m_Height);
        }
        else if (m_TextureFormat == 4)
        {
            bitmap = TEXMain.ReadRGBA32(data, m_Width, m_Height);
        }
        else if (m_TextureFormat == 10)
        {
            bitmap = TEXMain.ReadDXT1(data, m_Width, m_Height);
        }
        else if (m_TextureFormat == 12)
        {
            bitmap = TEXMain.ReadDXT5(data, m_Width, m_Height);
        }

        data = new byte[0];

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds + "ms to decode fmt " + m_TextureFormat);
        //Profiler.EndSample();

        GC.Collect(); //evil
        bitmapPool[assetTest] = bitmap;
        return bitmap;
    }
    private Bitmap GetBitmapNative(AssetsManager manager, AssetTypeValueField baseField, int fileId, long pathId, AssetsFileInstance fromInst)
    {
        AssetID assetTest = new AssetID(fromInst, fileId, pathId);
        if (bitmapPool.ContainsKey(assetTest))
            return bitmapPool[assetTest];

        //Profiler.BeginSample("Bitmap load");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int m_Width = baseField.Get("m_Width").GetValue().AsInt();
        int m_Height = baseField.Get("m_Height").GetValue().AsInt();
        int m_TextureFormat = baseField.Get("m_TextureFormat").GetValue().AsInt();
        if (m_TextureFormat != 3 && m_TextureFormat != 4 && m_TextureFormat != 10 && m_TextureFormat != 12)
            return SystemIcons.Exclamation.ToBitmap();
            //throw new Exception("TEX doesn't support format " + m_TextureFormat + " please contact nes");

        byte[] data = null;
        AssetTypeValueField m_StreamData = baseField.Get("m_StreamData");
        int offset = (int)m_StreamData.Get("offset").GetValue().AsUInt();
        int size = (int)m_StreamData.Get("size").GetValue().AsUInt();
        //check if texture is in resS (most likely is)
        if (size != 0)
        {
            string path = m_StreamData.Get("path").GetValue().AsString();
            string fullPath = Path.Combine(Path.GetDirectoryName(fromInst.path), path);
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream memStream = new MemoryStream())
            {
                long fileSize = stream.Length;
                data = new byte[size];
                stream.Position = offset;

                int bytesRead;
                var buffer = new byte[2048];
                while ((stream.Position < offset + size) && ((bytesRead = stream.Read(buffer, 0, 2048)) > 0))
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                data = memStream.ToArray();
            }
        }
        else
        {
            data = GetByteData(baseField.Get("image data"));
        }

        Bitmap bitmap = null;

        //todo handle with unity, possibly Texture2D.CreateExternalTexture
        //if (m_TextureFormat == 3)
        //{
        //    bitmap = TEXMain.ReadRGB24(data, m_Width, m_Height);
        //}
        //else if (m_TextureFormat == 4)
        //{
        //    bitmap = TEXMain.ReadRGBA32(data, m_Width, m_Height);
        //}
        //else if (m_TextureFormat == 10)
        //{
        //    bitmap = TEXMain.ReadDXT1(data, m_Width, m_Height);
        //}
        //else if (m_TextureFormat == 12)
        //{
        //    bitmap = TEXMain.ReadDXT5(data, m_Width, m_Height);
        //}
        
        Texture2D tex2d = new Texture2D(m_Width, m_Height, (TextureFormat)m_TextureFormat, false);
        tex2d.LoadRawTextureData(data);
        Color32[] colors = tex2d.GetPixels32();
        byte[] colorData = new byte[colors.Length * 4];
        for (int i = 0, j = 0; i < colors.Length; i++, j += 4)
        {
            colorData[j] = colors[i].b;
            colorData[j+1] = colors[i].g;
            colorData[j+2] = colors[i].r;
            colorData[j+3] = colors[i].a;
        }
        //bitmap = new Bitmap(new MemoryStream(tex2d.EncodeToPNG()));
        bitmap = new Bitmap(m_Width, m_Height, m_Width * 4, PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(colorData, 0));
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY); //todo make reader read flipped already
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds + "ms to decode fmt " + m_TextureFormat);
        //Profiler.EndSample();

        GC.Collect(); //evil
        bitmapPool[assetTest] = bitmap;
        return bitmap;
    }

    private float ReadFloat(byte[] data, int position)
    {
        byte[] bytes = new byte[] { data[position + 0], data[position + 1], data[position + 2], data[position + 3] };
        return BitConverter.ToSingle(bytes, 0);
    }

    private byte[] GetByteData(AssetTypeValueField field)
    {
        byte[] data = new byte[field.GetValue().AsArray().size];
        for (uint i = 0; i < data.Length; i++)
        {
            data[i] = (byte)field[i].GetValue().AsUInt();
        }
        return data;
    }

    private class AssetID
    {
        AssetsFileInstance fromInst;
        int fileId;
        long pathId;
        public AssetID(AssetsFileInstance fromInst, int fileId, long pathId)
        {
            this.fromInst = fromInst;
            this.fileId = fileId;
            this.pathId = pathId;
        }
        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(AssetID))
                return false;
            AssetID assetID = obj as AssetID;
            if (fromInst == assetID.fromInst &&
                fileId == assetID.fileId &&
                pathId == assetID.pathId)
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            int hash = 17;

            hash = hash * 23 + fromInst.path.GetHashCode();
            hash = hash * 23 + fileId.GetHashCode();
            hash = hash * 23 + pathId.GetHashCode();
            return hash;
        }
    }
}
