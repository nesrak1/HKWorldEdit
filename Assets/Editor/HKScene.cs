using Assets.Editor;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BundleLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HKScene
{
    private static readonly uint GAMEOBJECT = 0x01;
    private static readonly uint SPRITERENDERER = 0xD4;
    private static readonly uint MESHFILTER = 0x21;
    private static readonly uint MONOBEHAVIOUR = 0x72;

    public static string diffFile = "";

    SpriteLoader spriteLoader = null;

    [MenuItem("HKEdit/Open Scene", priority = 0)]
    public static void OpenScene()
    {
        string path = EditorUtility.OpenFilePanel("Open level file", "", "");
        if (path.Length != 0)
        {
            HKScene scene = new HKScene(path);
        }
    }

    [MenuItem("HKEdit/Set Active Scene", priority = 22)]
    public static void SetActiveScene()
    {
        EditorUtility.DisplayDialog("HKEdit", "This option is to set the active diff file in case you already had a level loaded without the diff set.", "OK");
        string path = EditorUtility.OpenFilePanel("Open level file", "", "");
        if (path.Length != 0)
        {
            diffFile = path;
        }
    }

    [MenuItem("HKDebug/Load the Triangle", priority = 50)]
    public static void LoadTriangle()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(@"C:\Users\karse\Documents\HKEdit\test.unity3d");
        Sprite s = bundle.LoadAsset<Sprite>("3") as Sprite;

        GameObject go = UnityEngine.Object.Instantiate(new GameObject()) as GameObject;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
    }

    AssetsFileInstance assetsFileInstance;
    AssetsFile assetsFile;
    AssetsFileTable assetsTable;
    AssetsManager am;
    AssetBundle bundle;
    UnityEngine.Object[] bundleAssets;
    Dictionary<AssetID, int> assetMap = new Dictionary<AssetID, int>();
    Dictionary<int, string> monoBehaviourIds = new Dictionary<int, string>();
    public HKScene(string path)
    {
        //using (FileStream assetStream = new FileStream(path, FileMode.Open))
        //{
        EditorUtility.DisplayProgressBar("HKEdit", "Loading level file", 0);
        diffFile = path;
        string folderName = Path.GetDirectoryName(path);
        am = new AssetsManager();
        assetsFileInstance = am.LoadAssetsFile(path, false);
        assetsFile = assetsFileInstance.file;
        assetsTable = assetsFileInstance.table;
        am.LoadClassPackage(Path.Combine(Application.dataPath, "cldb.dat"));
        //AssetFileInfoEx scenery = FindGameObject("_Scenery");
        //if (scenery == null)
        //{
        //    EditorUtility.DisplayDialog("HKEdit", "Failed to find _Scenery gameobject", "OK");
        //    EditorUtility.ClearProgressBar();
        //    return;
        //}
        EditorUtility.DisplayProgressBar("HKEdit", "Loading dependencies", 0);
        am.LoadAssetsFile(path, true);
        am.UpdateDependencies();

        spriteLoader = new SpriteLoader();

        //AssetTypeValueField sceneryGameObject = am.GetATI(am.file, scenery).GetBaseField();
        //AssetTypeValueField sceneryComponent = sceneryGameObject.Get("m_Component").Get("Array");
        //AssetsManager.AssetExternal sceneryTransformExt = am.GetExtAsset(sceneryComponent[0].Get("component"));
        //AssetTypeValueField sceneryTransform = sceneryTransformExt.instance.GetBaseField();
        //
        //for (uint i = 1; i < sceneryComponent.GetValue().AsArray().size; i++)
        //{
        //    AssetsManager.AssetExternal sceneryComponent2 = am.GetExtAsset(sceneryComponent[i].Get("component"), true);
        //    if (sceneryComponent2.info.curFileType == SPRITERENDERER)
        //    {
        //        Object.Instantiate();
        //        break;
        //    }
        //}

        byte[] bundleData = Loader.CreateBundleFromLevel(am, assetsFileInstance);
        EditorUtility.DisplayProgressBar("HKEdit", "Loading bundle", 1);
        bundle = AssetBundle.LoadFromMemory(bundleData);
        EditorUtility.DisplayProgressBar("HKEdit", "Loading scene", 1);

        //assetMap = GetAssetMap(am, bundle);

        string[] names = bundle.GetAllAssetNames();
        bundleAssets = bundle.LoadAllAssets();

        int index = 0;
        foreach (string fileName in names)
        {
            string withoutExt = fileName.Substring(0, fileName.Length - 4);
            string assetName = withoutExt.Split('/')[0];
            int fileId = int.Parse(withoutExt.Split('/')[1]);
            long pathId = long.Parse(withoutExt.Split('/')[2]);
            if (fileId == 0 && pathId == 0)
            {
                index++;
                continue;
            }
            assetMap.Add(new AssetID(assetName, fileId, pathId), index);
            index++;
        }

        int i = 0;
        foreach (AssetFileInfoEx info in assetsTable.pAssetFileInfo)
        {
            EditorUtility.DisplayProgressBar("HKEdit", "Creating GameObjects", i / (float)assetsTable.pAssetFileInfo.Length);
            if (info.curFileType == GAMEOBJECT)
            {
                RecurseGameObjects(info, true);
            }
            i++;
        }
        EditorUtility.ClearProgressBar();

        //SpriteLoader sprites = new SpriteLoader();
        //AssetTypeInstance ati = am.GetATI(am.file, );
        //sharedassets6.assets
        //AssetTypeInstance ati = am.GetATI(am.file, am.GetInfo(0, 220));
        //sharedassets105.assets
        //image = sprites.LoadTK2DSprite(am, am.GetInfo(0, 72));
        //sharedassets37.assets
        //image = sprites.LoadTK2DSprite(am, am.GetInfo(0, 245));
        //BitmapImage image = SpriteLoader.LoadUnitySprite(am, ati.GetBaseField());
        //TestImage.Source = image[0];
        //slider.Maximum = image.Count - 1;
        //List<System.Drawing.Point> points = SpriteLoader.GetPoints(ati.GetBaseField(), 1024);
        //RecursiveGameobjectSearch();
        //<Image x:Name="TestImage" Height="378" Canvas.Left="10" Canvas.Top="10" Width="711"/>
        //}
    }

    public GameObject RecurseGameObjects(AssetFileInfoEx info, bool topRoot)
    {
        AssetTypeValueField gameObject = am.GetATI(assetsFile, info).GetBaseField();
        string name = gameObject.Get("m_Name").GetValue().AsString();
        AssetTypeValueField m_Component = gameObject.Get("m_Component").Get("Array");
        AssetsManager.AssetExternal transformComponent = am.GetExtAsset(assetsFileInstance, m_Component[0].Get("component"));
        AssetTypeValueField transform = transformComponent.instance.GetBaseField();
        if (name == "TileMap Render Data" || name == "Template-TileMap (1) Render Data") //should be .EndsWith(" Render Data")
            return TilemapRenderData(transform, name);
        if (topRoot && transform.Get("m_Father").Get("m_PathID").GetValue().AsInt64() != 0)
            return null;
        if (!topRoot && transform.Get("m_Father").Get("m_PathID").GetValue().AsInt64() == 0)
            return null;
        GameObject gameObjectInstance = new GameObject(name);

        int m_Tag = (ushort)gameObject.Get("m_Tag").GetValue().AsUInt();
        if (m_Tag >= 20000)
        {
            int tagIndex = m_Tag - 20000; //u dumdum u overthink its just tag - 20000
            gameObjectInstance.tag = UnityEditorInternal.InternalEditorUtility.tags[tagIndex];
        }
        else if (m_Tag != 0)
        {
            string[] tags = new[] { "Respawn", "Finished", "EditorOnly", "MainCamera", "Player", "GameController" };
            gameObjectInstance.tag = tags[m_Tag - 1];
        }
        gameObjectInstance.layer = (int)gameObject.Get("m_Layer").GetValue().AsUInt();
        EditDiffer differ = gameObjectInstance.AddComponent<EditDiffer>();
        differ.fileId = 0;
        differ.pathId = info.index;

        Transform transformInstance = gameObjectInstance.transform;

        AssetTypeValueField m_LocalPosition = transform.Get("m_LocalPosition");
        AssetTypeValueField m_LocalRotation = transform.Get("m_LocalRotation");
        AssetTypeValueField m_LocalScale = transform.Get("m_LocalScale");

        Vector3 localPosition = GetVector3(m_LocalPosition);
        Quaternion localRotation = GetQuaternion(m_LocalRotation);
        Vector3 localScale = GetVector3(m_LocalScale);
        //localScale *= 1.5625f; //100/64

        //Debug.Log("in path " + info.index);
        for (uint i = 1; i < m_Component.GetValue().AsArray().size; i++)
        {
            //Debug.Log("check component " + m_Component[i].Get("component").Get("m_FileID").GetValue().AsInt() + " " + m_Component[i].Get("component").Get("m_PathID").GetValue().AsInt64());
            //faster to check for only info but also keeps us from reading
            //particle systems which tend to update literally every minor update
            //if we end up needing more types we can use typetree2cldb on an editor file
            AssetsManager.AssetExternal component = am.GetExtAsset(assetsFileInstance, m_Component[i].Get("component"), true);
            if (component.info.curFileType == SPRITERENDERER)
            {
                component = am.GetExtAsset(assetsFileInstance, m_Component[i].Get("component"));
                AssetTypeValueField baseField = component.instance.GetBaseField();
                AssetTypeValueField m_Sprite = baseField.Get("m_Sprite");
                int fileId = m_Sprite.Get("m_FileID").GetValue().AsInt();
                long pathId = m_Sprite.Get("m_PathID").GetValue().AsInt64();

                AssetsManager.AssetExternal sprite = am.GetExtAsset(assetsFileInstance, m_Sprite);
                if (sprite.info == null) //spriterenderer with no sprite lol
                    break;
                
                AssetsFileInstance spriteInst;
                if (m_Sprite.Get("m_FileID").GetValue().AsInt() == 0)
                    spriteInst = assetsFileInstance;
                else
                    spriteInst = assetsFileInstance.dependencies[m_Sprite.Get("m_FileID").GetValue().AsInt() - 1];

                //Texture2D image = spriteLoader.LoadUnitySprite(am, sprite.instance.GetBaseField(), assetsFileInstance.dependencies[fileId - 1]);

                //Sprite spriteInstance = Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f), 100f);
                Sprite spriteInstance = bundleAssets[assetMap[new AssetID(Path.GetFileName(spriteInst.path), fileId, pathId)]] as Sprite;
                SpriteRenderer sr = gameObjectInstance.AddComponent<SpriteRenderer>();
                string[] sortingLayers = new[] { "Default", "Far BG 2", "Far BG 1", "Mid BG", "Immediate BG", "Actors", "Player", "Tiles", "MID Dressing", "Immediate FG", "Far FG", "Vignette", "Over", "HUD" };
                sr.sortingLayerName = sortingLayers[baseField.Get("m_SortingLayer").GetValue().AsInt()];
                sr.sortingOrder = baseField.Get("m_SortingOrder").GetValue().AsInt();
                sr.sprite = spriteInstance;
                //spriteInstance.bounds.SetMinMax(new Vector3(0, 0), new Vector3(50, 50));
                //Debug.Log(spriteInstance.bounds.extents.x + "," + spriteInstance.bounds.extents.y + "," + spriteInstance.bounds.extents.z);
                break;
            }
            /*if (component.info.curFileType == MONOBEHAVIOUR)
            {
                component = am.GetExtAsset(assetsFileInstance, m_Component[i].Get("component"));
                AssetTypeValueField baseField = component.instance.GetBaseField();

                int monoTypeId = assetsFileInstance.file.typeTree.pTypes_Unity5[component.info.curFileTypeOrIndex].scriptIndex;
                if (monoBehaviourIds.ContainsKey(monoTypeId))
                {
                    if (monoBehaviourIds[monoTypeId] == "tk2dSprite")
                    {
                        baseField = Util.GetMonoBaseField(am, assetsFile, component.info, Path.GetDirectoryName(assetsFileInstance.path));

                        AssetTypeValueField collection = baseField.Get("collection");
                        int _spriteId = baseField.Get("_spriteId").GetValue().AsInt();

                        int fileId = collection.Get("m_FileID").GetValue().AsInt();
                        //long pathId = collection.Get("m_PathID").GetValue().AsInt64();

                        AssetsManager.AssetExternal sprite = am.GetExtAssetExt(assetsFileInstance, collection);
                        if (sprite.info == null)
                            break;

                        AssetsFileInstance spriteFileInstance = assetsFileInstance.dependencies[fileId - 1];
                        AssetTypeValueField spriteBaseField = Util.GetMonoBaseField(am, spriteFileInstance.file, sprite.info, Path.GetDirectoryName(assetsFileInstance.path));
                        Texture2D image = spriteLoader.LoadTK2dSprite(am, spriteBaseField, spriteFileInstance, _spriteId);

                        Sprite spriteInstance = Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f), 100f);
                        SpriteRenderer sr = gameObjectInstance.AddComponent<SpriteRenderer>();
                        sr.sortingLayerName = "Default";
                        sr.sortingOrder = 0;
                        sr.sprite = spriteInstance;
                        break;
                    }
                }
                else
                {
                    //map out the monobehaviour script indexes to their name for fast lookup
                    AssetTypeValueField m_Script = baseField.Get("m_Script");
                    AssetsManager.AssetExternal script = am.GetExtAssetExt(assetsFileInstance, m_Script);
                    string scriptName = script.instance.GetBaseField().Get("m_Name").GetValue().AsString();
                    if (!monoBehaviourIds.ContainsValue(scriptName))
                    {
                        monoBehaviourIds[monoTypeId] = scriptName;
                        //Debug.Log("monobids | " + monoTypeId + " = " + scriptName);
                    }
                }
            }*/
        }

        transformInstance.localScale = localScale;
        transformInstance.localPosition = localPosition;
        //transformInstance.localPosition *= 0.64f;
        transformInstance.localRotation = localRotation;

        AssetTypeValueField childrenArray = transform.Get("m_Children").Get("Array");
        uint childrenCount = childrenArray.GetValue().AsArray().size;
        for (uint i = 0; i < childrenCount; i++)
        {
            AssetTypeValueField childTf = am.GetExtAsset(assetsFileInstance, childrenArray[i]).instance.GetBaseField();
            AssetFileInfoEx childGo = am.GetExtAsset(assetsFileInstance, childTf.Get("m_GameObject")).info;
            RecurseGameObjects(childGo, false).transform.SetParent(transformInstance, false);
        }

        return gameObjectInstance;
    }

    public GameObject TilemapRenderData(AssetTypeValueField transform, string name)
    {
        GameObject tileMapRenderData = new GameObject(name);
        GameObject scenemap = new GameObject("Scenemap");
        scenemap.transform.parent = tileMapRenderData.transform;

        AssetTypeValueField trdChildArray = transform.Get("m_Children").Get("Array");
        AssetTypeValueField scenemapBaseField = am.GetExtAsset(assetsFileInstance, trdChildArray[0]).instance.GetBaseField();
        AssetTypeValueField scenemapChildArray = scenemapBaseField.Get("m_Children").Get("Array");
        uint childrenCount = scenemapChildArray.GetValue().AsArray().size;
        for (uint i = 0; i < childrenCount; i++)
        {
            AssetTypeValueField childTf = am.GetExtAsset(assetsFileInstance, scenemapChildArray[i]).instance.GetBaseField();
            AssetTypeValueField childGo = am.GetExtAsset(assetsFileInstance, childTf.Get("m_GameObject")).instance.GetBaseField();

            GameObject chunk = new GameObject(childGo.Get("m_Name").GetValue().AsString());
            chunk.transform.parent = scenemap.transform;

            AssetTypeValueField m_LocalPosition = childTf.Get("m_LocalPosition");
            chunk.transform.localPosition = GetVector3(m_LocalPosition);

            AssetTypeValueField childComp = childGo.Get("m_Component").Get("Array");

            uint componentCount = childComp.GetValue().AsArray().size;
            for (uint j = 1; j < componentCount; j++)
            {
                AssetsManager.AssetExternal component = am.GetExtAsset(assetsFileInstance, childComp[j].Get("component"));
                if (component.info.curFileType == MESHFILTER)
                {
                    component = am.GetExtAsset(assetsFileInstance, childComp[j].Get("component"));
                    AssetTypeValueField mesh = component.instance.GetBaseField().Get("m_Mesh");
                    AssetTypeValueField meshBaseField = am.GetExtAsset(assetsFileInstance, mesh).instance.GetBaseField();

                    AssetTypeValueField m_VertexData = meshBaseField.Get("m_VertexData");
                    AssetTypeValueField m_Channels = m_VertexData.Get("m_Channels").Get("Array");
                    uint channelsSize = m_Channels.GetValue().AsArray().size;

                    int skipSize = 0;
                    //start at 1 to skip verts
                    for (uint k = 1; k < channelsSize; k++)
                    {
                        skipSize += (int)m_Channels[k].Get("dimension").GetValue().AsUInt();
                    }
                    byte[] m_DataSize = GetByteData(m_VertexData.Get("m_DataSize"));
                    uint m_VertexCount = m_VertexData.Get("m_VertexCount").GetValue().AsUInt();

                    Vector3[] verts = new Vector3[m_VertexCount];

                    int pos = 0;
                    for (uint k = 0; k < m_VertexCount; k++)
                    {
                        float x = ReadFloat(m_DataSize, pos);
                        float y = ReadFloat(m_DataSize, pos + 4);
                        float z = ReadFloat(m_DataSize, pos + 8);
                        verts[k] = new Vector3(x, y, z);
                        pos += 12 + (skipSize * 4); //go past verts, then any remaining channel's data
                    }

                    AssetTypeValueField m_IndexBuffer = meshBaseField.Get("m_IndexBuffer").Get("Array");
                    uint triCount = m_IndexBuffer.GetValue().AsArray().size;
                    int[] tris = new int[triCount / 2];
                    for (uint k = 0; k < triCount; k += 2)
                    {
                        int tri = (int)(m_IndexBuffer[k + 0].GetValue().AsUInt() | (m_IndexBuffer[k + 1].GetValue().AsUInt() << 8));
                        tris[k / 2] = tri;
                    }

                    UnityEngine.Mesh meshComponent = new UnityEngine.Mesh();
                    chunk.AddComponent<MeshFilter>();
                    chunk.AddComponent<MeshRenderer>();
                    chunk.GetComponent<MeshFilter>().mesh = meshComponent;
                    meshComponent.vertices = verts;
                    meshComponent.triangles = tris;
                    chunk.GetComponent<MeshRenderer>().material = Resources.Load<Material>("BackMat");
                    break;
                }
            }
        }

        return tileMapRenderData;
    }
    
    public Vector3 GetVector3(AssetTypeValueField field)
    {
        float x = field.Get("x").GetValue().AsFloat();
        float y = field.Get("y").GetValue().AsFloat();
        float z = field.Get("z").GetValue().AsFloat();
        return new Vector3(x, y, z);
    }

    public Quaternion GetQuaternion(AssetTypeValueField field)
    {
        float x = field.Get("x").GetValue().AsFloat();
        float y = field.Get("y").GetValue().AsFloat();
        float z = field.Get("z").GetValue().AsFloat();
        float w = field.Get("w").GetValue().AsFloat();
        return new Quaternion(x, y, z, w);
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

    private AssetFileInfoEx FindGameObject(string name)
    {
        foreach (AssetFileInfoEx info in assetsTable.pAssetFileInfo)
        {
            //faster check for GameObject
            if (info.curFileType == 0x01)
            {
                ClassDatabaseType type = AssetHelper.FindAssetClassByID(am.classFile, info.curFileType);
                string infoName = UABEExtras.GetAssetNameFast(info, am.classFile, type, assetsFile.reader);
                if (infoName == name)
                {
                    return info;
                }
            }
        }
        return null;
    }
}
