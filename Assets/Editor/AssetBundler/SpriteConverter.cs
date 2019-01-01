using AssetsTools.NET;
using System;
using System.IO;

namespace BundleLoader
{
    public class SpriteConverter
    {
        public static AssetsReplacer ConvertSprite(AssetTypeValueField baseField, ulong pathId, ulong mainTex, ulong alphaTex)
        {
            AssetTypeValueField m_SpriteAtlas = baseField.Get("m_SpriteAtlas");
            if (m_SpriteAtlas.Get("m_PathID").GetValue().AsInt64() != 0)
                Console.WriteLine("warning, sprite atlas isn't null! (" + m_SpriteAtlas.GetValue().AsInt64() + ")");
            AssetTypeValueField m_RD = baseField.Get("m_RD");
            AssetTypeValueField m_VertexData = m_RD.Get("m_VertexData");
            AssetTypeValueField m_Channels = m_VertexData.Get("m_Channels").Get("Array");
            float m_PixelsToUnits = baseField.Get("m_PixelsToUnits").GetValue().AsFloat();
            AssetTypeValueField m_Rect = baseField.Get("m_Rect");
            float width = m_Rect.Get("width").GetValue().AsFloat();
            float height = m_Rect.Get("height").GetValue().AsFloat();
            AssetTypeValueField texture = m_RD.Get("texture");
            AssetTypeValueField alphaTexture = m_RD.Get("alphaTexture");

            uint channelsSize = m_Channels.GetValue().AsArray().size;
            int skipSizeBefore = 0;
            int skipSizeAfter = 0;
            for (uint k = 0; k < 3; k++)
            {
                skipSizeBefore += (int)m_Channels[k].Get("dimension").GetValue().AsUInt();
            }

            for (uint k = 4; k < channelsSize; k++)
            {
                skipSizeAfter += (int)m_Channels[k].Get("dimension").GetValue().AsUInt();
            }

            //spriteRange.Get("stream").GetValue().Set((byte)0);
            //spriteRange.Get("offset").GetValue().Set((byte)0);
            //spriteRange.Get("format").GetValue().Set((byte)0);
            //spriteRange.Get("dimension").GetValue().Set((byte)0);
            //
            //byte[] data = GetByteData(m_VertexData.Get("m_DataSize"));
            //using (MemoryStream rms = new MemoryStream(data))
            //using (MemoryStream wms = new MemoryStream())
            //using (BinaryReader r = new BinaryReader(rms))
            //using (BinaryWriter w = new BinaryWriter(wms))
            //{
            //    while (r.BaseStream.Position < r.BaseStream.Length)
            //    {
            //        if (skipSizeBefore > 0)
            //            w.Write(r.ReadBytes(skipSizeBefore * 4));
            //        r.BaseStream.Position += 8;
            //        if (skipSizeAfter > 0)
            //            w.Write(r.ReadBytes(skipSizeAfter * 4));
            //    }
            //    data = wms.ToArray();
            //}

            int position = 0;
            int triType = 0;
            byte[] data = GetByteData(m_VertexData.Get("m_DataSize"));

            while (position < data.Length)
            {
                float x = ReadFloat(data, position);
                float y = ReadFloat(data, position + 4);
                position += skipSizeBefore * 4;
                SetFloat(data, (x * m_PixelsToUnits + width / 2) / width, position);
                SetFloat(data, (y * m_PixelsToUnits + height / 2) / height, position + 4);
                position += 8;
                position += skipSizeAfter * 4;
                triType++;
            }
            
            m_VertexData.Get("m_DataSize").GetValue().type = EnumValueTypes.ValueType_ByteArray;
            m_VertexData.Get("m_DataSize").GetValue().Set(new AssetTypeByteArray() {
                data = data,
                size = (uint)data.Length
            });
            m_VertexData.Get("m_DataSize").templateField.valueType = EnumValueTypes.ValueType_ByteArray;

            //Texture refs
            texture.Get("m_FileID").GetValue().Set(0);
            texture.Get("m_PathID").GetValue().Set((long)mainTex);

            //AlphaTexture refs
            alphaTexture.Get("m_FileID").GetValue().Set(0);
            alphaTexture.Get("m_PathID").GetValue().Set((long)alphaTex);

            byte[] spriteAsset;
            using (MemoryStream memStream = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
            {
                writer.bigEndian = false;
                baseField.Write(writer);
                spriteAsset = memStream.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0xD5, 0xFFFF, spriteAsset);
        }

        private static float ReadFloat(byte[] data, int position)
        {
            byte[] bytes = new byte[] { data[position + 0], data[position + 1], data[position + 2], data[position + 3] };
            return BitConverter.ToSingle(bytes, 0);
        }

        private static void SetFloat(byte[] data, float value, int position)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            //UnityEngine.Debug.Log("Setting " + bytes[0].ToString("X2") + bytes[1].ToString("X2") + bytes[2].ToString("X2") + bytes[3].ToString("X2") + " for " + value);
            data[position + 0] = bytes[0];
            data[position + 1] = bytes[1];
            data[position + 2] = bytes[2];
            data[position + 3] = bytes[3];
        }

        private static byte[] GetByteData(AssetTypeValueField field)
        {
            byte[] data = new byte[field.GetValue().AsArray().size];
            for (uint i = 0; i < data.Length; i++)
            {
                data[i] = (byte)field[i].GetValue().AsUInt();
            }
            return data;
        }
    }
}
