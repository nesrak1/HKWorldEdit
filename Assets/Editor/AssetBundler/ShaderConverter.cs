using AssetsTools.NET;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundleLoader
{
    public class ShaderConverter
    {
        public static AssetsReplacer ConvertShader(AssetTypeValueField baseField, ulong pathId, List<ulong> dependencies)
        {
            AssetTypeValueField m_Dependencies = baseField.Get("m_Dependencies").Get("Array");

            for (int i = 0; i < m_Dependencies.GetValue().AsArray().size; i++)
            {
                m_Dependencies[(uint)i].Get("m_FileID").GetValue().Set(0);
                m_Dependencies[(uint)i].Get("m_PathID").GetValue().Set(dependencies[i]);
            }

            byte[] shaderAsset;
            using (MemoryStream memStream = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
            {
                writer.bigEndian = false;
                baseField.Write(writer);
                shaderAsset = memStream.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0x30, 0xFFFF, shaderAsset);
        }
    }
}