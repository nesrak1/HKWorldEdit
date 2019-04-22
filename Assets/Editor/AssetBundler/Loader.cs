using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace BundleLoader
{
    public class Loader
    {
        public static byte[] CreateBundleFromLevel(AssetsManager am, AssetsFileInstance inst)
        {
            AssetsFile file = inst.file;
            AssetsFileTable table = inst.table;

            string folderName = Path.GetDirectoryName(inst.path);

            ulong pathId = 2;
            List<AssetsReplacer> assets = new List<AssetsReplacer>();
            Dictionary<AssetID, ulong> assetMap = new Dictionary<AssetID, ulong>();

            //get all sprites

            int i = 0;
            List<AssetFileInfoEx> infos = table.GetAssetsOfType(0xD4);
            List<ulong> spriteIds = new List<ulong>();
            foreach (AssetFileInfoEx info in infos)
            {
                //honestly this is a really trash way to do this
                //we have a better scene exporter but it would
                //take some work to fix it up and block certain assets

                EditorUtility.DisplayProgressBar("HKEdit", "Creating scene bundle", i / (float)infos.Count);

                AssetTypeValueField baseField = GetBaseField(am, file, info);
            
                AssetTypeValueField m_Sprite = baseField.Get("m_Sprite");
                AssetFileInfoEx spriteInfo = am.GetExtAsset(inst, m_Sprite, true).info;
                AssetsFileInstance spriteInst;
                if (m_Sprite.Get("m_FileID").GetValue().AsInt() == 0)
                    spriteInst = inst;
                else
                    spriteInst = inst.dependencies[m_Sprite.Get("m_FileID").GetValue().AsInt() - 1];
                
                int spriteFileId = m_Sprite.Get("m_FileID").GetValue().AsInt();
                long spritePathId = m_Sprite.Get("m_PathID").GetValue().AsInt64();
                if (assetMap.ContainsKey(new AssetID(Path.GetFileName(spriteInst.path), spritePathId)) || (spriteFileId == 0 && spritePathId == 0))
                {
                    i++;
                    continue;
                }

                AssetTypeValueField spriteBaseField = GetBaseField(am, spriteInst.file, spriteInfo);

                AssetTypeValueField m_RD = spriteBaseField.Get("m_RD");
            
                AssetTypeValueField texture = m_RD.Get("texture");
                AssetTypeValueField alphaTexture = m_RD.Get("alphaTexture");

                AssetsFileInstance textureInst, alphaTextureInst;
                if (texture.Get("m_FileID").GetValue().AsInt() == 0)
                    textureInst = spriteInst;
                else
                    textureInst = spriteInst.dependencies[texture.Get("m_FileID").GetValue().AsInt() - 1];

                if (alphaTexture.Get("m_FileID").GetValue().AsInt() == 0)
                    alphaTextureInst = spriteInst;
                else
                    alphaTextureInst = spriteInst.dependencies[alphaTexture.Get("m_FileID").GetValue().AsInt() - 1];

                AssetTypeInstance textureAti = am.GetExtAsset(spriteInst, texture, false).instance;
                AssetTypeInstance alphaTextureAti = am.GetExtAsset(spriteInst, alphaTexture, false).instance;
            
                ulong textureId = 0, alphaTextureId = 0;
            
                if (textureAti != null)
                {
                    AssetID id = new AssetID(Path.GetFileName(textureInst.path), texture.Get("m_PathID").GetValue().AsInt64());
                    if (!assetMap.ContainsKey(id))
                    {
                        textureId = pathId;
                        assetMap.Add(id, pathId);
                        assets.Add(TextureConverter.ConvertTexture(textureAti.GetBaseField(), pathId++, folderName));
                    }
                    else
                    {
                        textureId = assetMap[id];
                    }
                }
                if (alphaTextureAti != null)
                {
                    AssetID id = new AssetID(Path.GetFileName(alphaTextureInst.path), alphaTexture.Get("m_PathID").GetValue().AsInt64());
                    if (!assetMap.ContainsKey(id))
                    {
                        alphaTextureId = pathId;
                        assetMap.Add(id, pathId);
                        assets.Add(TextureConverter.ConvertTexture(alphaTextureAti.GetBaseField(), pathId++, folderName));
                    }
                    else
                    {
                        alphaTextureId = assetMap[id];
                    }
                }
                AssetTypeValueField m_Materials = baseField.Get("m_Materials").Get("Array");
                if (m_Materials.GetValue().AsArray().size > 0)
                {
                    AssetTypeValueField material = baseField.Get("m_Materials").Get("Array")[0];
                    AssetsFileInstance materialInst;

                    int materialFileId = material.Get("m_FileID").GetValue().AsInt();

                    if (materialFileId == 0)
                        materialInst = inst;
                    else
                        materialInst = inst.dependencies[materialFileId - 1];

                    AssetID materialId = new AssetID(Path.GetFileName(materialInst.path), material.Get("m_PathID").GetValue().AsInt64());
                    if (!assetMap.ContainsKey(materialId))
                    {
                        AssetTypeValueField materialBaseField = am.GetExtAsset(inst, material).instance.GetBaseField();

                        AssetTypeValueField shader = materialBaseField.Get("m_Shader");

                        ulong shaderPathId = RecurseShaderDependencies(am, materialInst, pathId, shader, assets, assetMap, out pathId);

                        assetMap.Add(materialId, pathId);
                        assets.Add(MaterialConverter.ConvertMaterial(materialBaseField, pathId++, shaderPathId));
                    }
                }

                assetMap.Add(new AssetID(Path.GetFileName(spriteInst.path), spritePathId), pathId);
                spriteIds.Add(pathId);
                assets.Add(SpriteConverter.ConvertSprite(spriteBaseField, pathId++, textureId, alphaTextureId));
                i++;
            }

            assetMap.Add(new AssetID(0), pathId);
            assets.Add(HeaderInformation.CreateHeaderInformation(assetMap, pathId++));

            assets.Insert(0, BundleMeta.CreateBundleInformation(assetMap, spriteIds, 1));
            //assets.Add(BundleMeta.CreateBundleInformation(assetMap, 1));

            //todo: pull from original assets file, cldb is not always update to date
            List<Type_0D> types = new List<Type_0D>
            {
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x8E)), //AssetBundle
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x1C)), //Texture2D
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x31)), //TextAsset
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0xD4)), //SpriteRenderer
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0xD5)), //Sprite
                //FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x31)), //TextAsset
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x15)), //Material
                FixTypeTree(C2T5.Cldb2TypeTree(am.classFile, 0x30))  //Shader
            };

            const string ver = "2017.4.10f1";
            //const string ver = "2018.2.1f1";

            byte[] blankData = BundleCreator.CreateBlankAssets(ver, types);
            AssetsFile blankFile = new AssetsFile(new AssetsFileReader(new MemoryStream(blankData)));

            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(ms))
            {
                blankFile.WriteFix(writer, 0, assets.ToArray(), 0);
                data = ms.ToArray();
            }

            EditorUtility.DisplayProgressBar("HKEdit", "Creating bundle", 1);
            using (MemoryStream ms = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(ms))
            {
                BundleCreator.CreateBlankBundle(ver, data.Length).Write(writer, data);
                return ms.ToArray();
            }
        }

        public static ulong RecurseShaderDependencies(AssetsManager am, AssetsFileInstance baseInst, ulong pathId, AssetTypeValueField shaderPPtr, List<AssetsReplacer> assets, Dictionary<AssetID, ulong> assetMap, out ulong outPathId)
        {
            //UnityEngine.Debug.Log("RSD Running");
            AssetsFileInstance shaderInst;
            int shaderFileId = shaderPPtr.Get("m_FileID").GetValue().AsInt();
            if (shaderFileId == 0)
                shaderInst = baseInst;
            else
                shaderInst = baseInst.dependencies[shaderFileId - 1];

            AssetID shaderId = new AssetID(Path.GetFileName(shaderInst.path), shaderPPtr.Get("m_PathID").GetValue().AsInt64());
            if (assetMap.ContainsKey(shaderId))
            {
                outPathId = pathId;
                return assetMap[shaderId];
            }
            AssetTypeValueField shaderBaseField = am.GetExtAsset(baseInst, shaderPPtr).instance.GetBaseField();

            List<ulong> dependencies = new List<ulong>();
            AssetTypeValueField m_Dependencies = shaderBaseField.Get("m_Dependencies").Get("Array");
            foreach (AssetTypeValueField m_Dependency in m_Dependencies.pChildren)
            {
                //UnityEngine.Debug.Log("from file " + Path.GetFileName(baseInst.path) + shaderPPtr.Get("m_FileID").GetValue().AsInt() + ":" + shaderPPtr.Get("m_PathID").GetValue().AsInt() + " getting pptr " + m_Dependency.Get("m_FileID").GetValue().AsInt() + ":" + m_Dependency.Get("m_PathID").GetValue().AsInt64());
                dependencies.Add(RecurseShaderDependencies(am, shaderInst, pathId, m_Dependency, assets, assetMap, out pathId));
            }

            ulong shaderPathid = pathId;
            assetMap.Add(shaderId, pathId);
            assets.Add(ShaderConverter.ConvertShader(shaderBaseField, pathId++, dependencies));

            outPathId = pathId;
            return shaderPathid;
        }

        public static AssetTypeValueField GetBaseField(AssetsManager am, AssetsFile file, AssetFileInfoEx info)
        {
            AssetTypeInstance ati = am.GetATI(file, info);
            return ati.GetBaseField();
        }

        public static Type_0D FixTypeTree(Type_0D tt)
        {
            //if basefield isn't 3, unity won't load the bundle
            tt.pTypeFieldsEx[0].version = 3;
            return tt;
        }
    }

    public class AssetID
    {
        public string fileName;
        public long pathId;
        public AssetID(long pathId)
        {
            this.fileName = "";
            this.pathId = pathId;
        }
        public AssetID(string fileName, long pathId)
        {
            this.fileName = fileName;
            this.pathId = pathId;
        }
        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(AssetID))
                return false;
            AssetID assetID = obj as AssetID;
            if (fileName == assetID.fileName &&
                pathId == assetID.pathId)
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            int hash = 17;
            
            hash = hash * 23 + fileName.GetHashCode();
            hash = hash * 23 + pathId.GetHashCode();
            return hash;
        }
    }
}
