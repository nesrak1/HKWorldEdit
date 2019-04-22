using AssetsTools.NET;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundleLoader
{
    public class MaterialConverter
    {
        public static AssetsReplacer ConvertMaterial(AssetTypeValueField baseField, ulong pathId, ulong shaderPathId)
        {
            AssetTypeValueField m_Shader = baseField.Get("m_Shader");
            m_Shader.Get("m_FileID").GetValue().Set(0);
            m_Shader.Get("m_PathID").GetValue().Set((long)shaderPathId);

            AssetTypeValueField m_TexEnvs = baseField.Get("m_SavedProperties").Get("m_TexEnvs").Get("Array");
            foreach (AssetTypeValueField m_TexEnv in m_TexEnvs.pChildren)
            {
                AssetTypeValueField m_Texture = m_TexEnv.Get("second").Get("m_Texture");
                m_Texture.Get("m_FileID").GetValue().Set(0);
                m_Texture.Get("m_PathID").GetValue().Set((long)0);
            }

            byte[] materialAsset;
            using (MemoryStream memStream = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
            {
                writer.bigEndian = false;
                baseField.Write(writer);
                materialAsset = memStream.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0x15, 0xFFFF, materialAsset);
        }
    }
}