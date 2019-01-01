using AssetsTools.NET;
using System;
using System.IO;

namespace BundleLoader
{
    public class SpriteRendererConverter
    {
        public static AssetsReplacer ConvertSpriteRenderer(AssetTypeValueField baseField, ulong pathId, AssetPPtr goPPtr, AssetPPtr matPPtr, AssetPPtr spritePPtr)
        {
            AssetTypeValueField m_GameObject = baseField.Get("m_GameObject");
            AssetTypeValueField m_Materials = baseField.Get("m_Materials").Get("Array");
            if (m_Materials.GetValue().AsArray().size != 1)
                Console.WriteLine("warning, sprite material is not 1! (" + m_Materials.GetValue().AsArray().size + ")");
            AssetTypeValueField m_Sprite = baseField.Get("m_Sprite");

            //GameObject refs
            m_GameObject.Get("m_FileID").GetValue().Set((int)goPPtr.fileID);
            m_GameObject.Get("m_PathID").GetValue().Set((long)goPPtr.pathID);

            //Material refs
            m_Materials.value.value.asArray.size = 1;
            m_Materials.pChildren = new AssetTypeValueField[1];
            AssetTypeValueField m_FileID = new AssetTypeValueField()
            {
                templateField = m_Materials.templateField.children[1].children[0],
                childrenCount = 0,
                pChildren = new AssetTypeValueField[0],
                value = new AssetTypeValue(EnumValueTypes.ValueType_Int32, (int)matPPtr.fileID)
            };
            AssetTypeValueField m_PathID = new AssetTypeValueField()
            {
                templateField = m_Materials.templateField.children[1].children[1],
                childrenCount = 0,
                pChildren = new AssetTypeValueField[0],
                value = new AssetTypeValue(EnumValueTypes.ValueType_Int64, (long)matPPtr.pathID)
            };
            AssetTypeValueField m_Material = new AssetTypeValueField()
            {
                templateField = m_Materials.templateField.children[1],
                childrenCount = 2,
                pChildren = new AssetTypeValueField[2]
                {
                    m_FileID,
                    m_PathID
                },
                value = new AssetTypeValue(EnumValueTypes.ValueType_Array, 0)
            };

            //Sprite refs
            m_Sprite.Get("m_FileID").GetValue().Set((int)spritePPtr.fileID);
            m_Sprite.Get("m_PathID").GetValue().Set((long)spritePPtr.pathID);

            byte[] spriteRendererAsset; 
            using (MemoryStream memStream = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
            {
                writer.bigEndian = false;
                baseField.Write(writer);
                spriteRendererAsset = memStream.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0xD4, 0xFFFF, spriteRendererAsset);
        }
    }
}
