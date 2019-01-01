using AssetsTools.NET;
using System;
using System.IO;

namespace BundleLoader
{
    public class TextureConverter
    {
        public static AssetsReplacer ConvertTexture(AssetTypeValueField baseField, ulong pathId, string folderPath)
        {
            //byte[] data = null;
            AssetTypeValueField m_StreamData = baseField.Get("m_StreamData");
            //int offset = (int)m_StreamData.Get("offset").GetValue().AsUInt();
            //int size = (int)m_StreamData.Get("size").GetValue().AsUInt();
            //
            //if (size != 0)
            //{
            //    string path = m_StreamData.Get("path").GetValue().AsString();
            //    using (FileStream stream = new FileStream(Path.Combine(folderPath, path), FileMode.Open))
            //    using (MemoryStream memStream = new MemoryStream())
            //    {
            //        long fileSize = stream.Length;
            //        data = new byte[size];
            //        stream.Position = offset;
            //
            //        int bytesRead;
            //        var buffer = new byte[2048];
            //        while (((bytesRead = stream.Read(buffer, 0, Math.Min(2048, (offset + size) - (int)stream.Position))) > 0))
            //        {
            //            memStream.Write(buffer, 0, bytesRead);
            //            if (stream.Position >= offset + size)
            //            {
            //                break;
            //            }
            //        }
            //        data = memStream.ToArray();
            //    }
            //}

            //this may not be needed, instead change the path to a hardcoded path to the resS
            //m_StreamData.Get("offset").value.value.asUInt32 = 0;
            //m_StreamData.Get("size").value.value.asUInt32 = 0;
            m_StreamData.Get("path").value.value.asString = Path.Combine(folderPath, m_StreamData.Get("path").GetValue().AsString());
            //baseField.Get("image data").GetValue().type = EnumValueTypes.ValueType_ByteArray;
            //baseField.Get("image data").GetValue().Set(new AssetTypeByteArray() {
            //    data = data,
            //    size = (uint)data.Length
            //});
            //baseField.Get("image data").templateField.valueType = EnumValueTypes.ValueType_ByteArray;
            byte[] textureAsset;
            using (MemoryStream memStream = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
            {
                writer.bigEndian = false;
                baseField.Write(writer);
                textureAsset = memStream.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0x1C, 0xFFFF, textureAsset);
        }
    }
}
