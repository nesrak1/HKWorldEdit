using Assets.Editor;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HKSave
{
    [MenuItem("HKEdit/Save Scene", priority = 11)]
    public static void SaveScene()
    {
        string diff = HKScene.diffFile;
        if (HKScene.diffFile.Length == 0)
        {
            EditorUtility.DisplayDialog("HKEdit", "Initial file not loaded. If you edited code, you can set the active map by \"Set Active Map\".", "OK");
            return;
        }
        string path = EditorUtility.SaveFilePanel("Choose level save location", "", "", "");
        if (path.Length != 0 && File.Exists(diff))
        {
            HKSave save = new HKSave(path, diff);
        }
    }

    AssetsFileInstance assetsFileInstance;
    AssetsFile assetsFile;
    AssetsFileTable assetsTable;
    AssetsManager am;
    AssetBundleBuild bundle;
    DiffFile diff;
    public HKSave(string path, string diff)
    {
        LoadProgress("Load Scene", "Generating diff...", 0);
        am = new AssetsManager();
        assetsFileInstance = am.LoadAssetsFile(diff, false);
        assetsFile = assetsFileInstance.file;
        assetsTable = assetsFileInstance.table;
        am.LoadClassPackage(Path.Combine(Application.dataPath, "cldb.dat"));

        //FileStream stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        //BinaryWriter writer = new BinaryWriter(stream);

        GameObject[] sceneObjs = GameObject.FindObjectsOfType<GameObject>();
        List<GameObjectAdd> addList = new List<GameObjectAdd>();
        List<GameObjectRemove> removeList = new List<GameObjectRemove>();
        List<GameObjectChange> changeList = new List<GameObjectChange>();
        List<GameObject> newGameObjects = new List<GameObject>();
        List<GameObjectInfo> newGoInfos = new List<GameObjectInfo>();

        int i = 0;
        foreach (GameObject obj in sceneObjs)
        {
            LoadProgress("Load Scene", "Generating diff...", ((float)i / sceneObjs.Length) * 100f);
            EditDiffer differ = obj.GetComponent<EditDiffer>();
            if (differ != null)
            {
                if (!differ.newAsset)
                {
                    List<ComponentChangeOrAdd> changes = new List<ComponentChangeOrAdd>();
                    List<ComponentRemove> removes = new List<ComponentRemove>();

                    ulong origPathId = differ.pathId;

                    CompareTransform(obj, origPathId, changes);

                    if (changes.Count > 0 || removes.Count > 0)
                    {
                        changeList.Add(new GameObjectChange()
                        {
                            pathId = (long)origPathId,
                            changes = changes,
                            removes = removes
                        });
                    }
                }
                else
                {
                    //bool parentIsNew = false;
                    ulong parentPathId = ulong.MaxValue;
                    if (obj.transform.parent == null)
                    {
                        parentPathId = 0;
                    }
                    else
                    {
                        EditDiffer ed = obj.transform.parent.gameObject.GetComponent<EditDiffer>();
                        if (ed != null)
                        {
                            parentPathId = ed.pathId;
                            //if (ed.newAsset)
                            //{
                            //    parentIsNew = true;
                            //}
                        }
                        else
                        {
                            Debug.LogWarning("editdiff missing from " + obj.name + "'s parent");
                        }
                    }
                    bool newAsset = differ.origPathId == 0; //when a new asset was created *not* by cloning
                    addList.Add(new GameObjectAdd()
                    {
                        pathId = differ.pathId,
                        parentId = parentPathId,
                        goNew = newAsset
                    });
                    newGameObjects.Add(obj);
                    if (newAsset)
                    {
                        newGoInfos.Add(new GameObjectInfo(obj.name, differ.fileId, differ.origPathId, differ.pathId));
                    }
                }
            }
            i++;
        }
        LoadProgress("Create Diff Bundle", 0);
        CreateBundle(path, addList, removeList, changeList, newGameObjects, newGoInfos);
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("HKEdit", "Diff saved. Because of a Unity bug, please clear and reopen the map to continue working. All errors shown after save are normal.", "OK");
        //stream.Close();
    }

    private void CreateBundle(string path, List<GameObjectAdd> addList, List<GameObjectRemove> removeList, List<GameObjectChange> changeList, List<GameObject> newGameObjects, List<GameObjectInfo> newGoInfos)
    {
        byte[] data = null;
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter w = new BinaryWriter(ms))
        {
            DiffFile file = new DiffFile()
            {
                magic = 0x45574B48,
                version = 1,
                unityCompiledVersion = Application.unityVersion,
                adds = addList,
                removes = removeList,
                changes = changeList,
                infos = newGoInfos
            };
            file.Write(w);
            data = ms.ToArray();
        }

        //unity doesn't trust us to import files from
        //memory so we have to create a file to import it
        string dataPath = "Assets/ABTemp/HKWEDiffData.bytes";

        File.WriteAllBytes(dataPath, data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<string> buildAssets = new List<string>();
        buildAssets.Add("Assets/ABTemp/HKWEDiffData.bytes");
        foreach (GameObject go in newGameObjects)
        {
            EditDiffer ed = go.GetComponent<EditDiffer>();
            string pName = "Assets/ABTemp/HKWEA_" + go.name.Substring(0, Math.Min(go.name.Length, 8)) + "_" + ed.fileId + "_" + ed.origPathId + "_" + ed.pathId + ".prefab";
            Debug.Log("creating asset " + pName);
            GameObject pgo = PrefabUtility.CreatePrefab(pName, go, ReplacePrefabOptions.Default);
            PrefabUtility.DisconnectPrefabInstance(go);
            UnityEngine.Object.DestroyImmediate(pgo.GetComponent<EditDiffer>(), true);
            EditorUtility.SetDirty(pgo);
            buildAssets.Add(pName);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("printing assets");
        foreach (string str in buildAssets)
        {
            Debug.Log(str);
        }

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = Path.GetFileName(path);
        buildMap[0].assetNames = buildAssets.ToArray();
        BuildPipeline.BuildAssetBundles(Path.GetDirectoryName(path), buildMap, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows64);

        //File.Delete(dataPath);
    }

    //fast transform compare since most
    //changes will probably be transforms
    private void CompareTransform(GameObject obj, ulong origPathId, List<ComponentChangeOrAdd> changes)
    {
        Transform objTransform = obj.transform;

        AssetFileInfoEx goInfo = assetsTable.getAssetInfo(origPathId);
        AssetTypeInstance goInstance = am.GetATI(assetsFile, goInfo);
        AssetTypeValueField transformPPtr = goInstance.GetBaseField()
            .Get("m_Component")
            .Get("Array")[0]
            .Get("component");
        AssetTypeInstance tfmInfo = am.GetExtAsset(assetsFileInstance, transformPPtr).instance;
        AssetTypeValueField tfmBaseField = tfmInfo.GetBaseField();
        AssetTypeValueField m_LocalPosition = tfmBaseField.Get("m_LocalPosition");
        AssetTypeValueField m_LocalRotation = tfmBaseField.Get("m_LocalRotation");
        AssetTypeValueField m_LocalScale = tfmBaseField.Get("m_LocalScale");
        Vector3 localPosition = new Vector3(
            m_LocalPosition.Get("x").GetValue().AsFloat(),
            m_LocalPosition.Get("y").GetValue().AsFloat(),
            m_LocalPosition.Get("z").GetValue().AsFloat()
        );
        Quaternion localRotation = new Quaternion(
            m_LocalRotation.Get("x").GetValue().AsFloat(),
            m_LocalRotation.Get("y").GetValue().AsFloat(),
            m_LocalRotation.Get("z").GetValue().AsFloat(),
            m_LocalRotation.Get("w").GetValue().AsFloat()
        );
        Vector3 localScale = new Vector3(
            m_LocalScale.Get("x").GetValue().AsFloat(),
            m_LocalScale.Get("y").GetValue().AsFloat(),
            m_LocalScale.Get("z").GetValue().AsFloat()
        );
        if (objTransform.localPosition != localPosition ||
            objTransform.localRotation != localRotation ||
            objTransform.localScale != localScale)
        {
            List<FieldChange> fieldChanges = new List<FieldChange>();

            if (localPosition.x != objTransform.localPosition.x)
                fieldChanges.Add(new FieldChange("m_LocalPosition/x", objTransform.localPosition.x));
            if (localPosition.y != objTransform.localPosition.y)
                fieldChanges.Add(new FieldChange("m_LocalPosition/y", objTransform.localPosition.y));
            if (localPosition.z != objTransform.localPosition.z)
                fieldChanges.Add(new FieldChange("m_LocalPosition/z", objTransform.localPosition.z));

            if (localRotation.w != objTransform.localRotation.w)
                fieldChanges.Add(new FieldChange("m_LocalRotation/w", objTransform.localRotation.w));
            if (localRotation.x != objTransform.localRotation.x)
                fieldChanges.Add(new FieldChange("m_LocalRotation/x", objTransform.localRotation.x));
            if (localRotation.y != objTransform.localRotation.y)
                fieldChanges.Add(new FieldChange("m_LocalRotation/y", objTransform.localRotation.y));
            if (localRotation.z != objTransform.localRotation.z)
                fieldChanges.Add(new FieldChange("m_LocalRotation/z", objTransform.localRotation.z));

            if (localScale.x != objTransform.localScale.x)
                fieldChanges.Add(new FieldChange("m_LocalScale/x", objTransform.localScale.x));
            if (localScale.y != objTransform.localScale.y)
                fieldChanges.Add(new FieldChange("m_LocalScale/y", objTransform.localScale.y));
            if (localScale.z != objTransform.localScale.z)
                fieldChanges.Add(new FieldChange("m_LocalScale/z", objTransform.localScale.z));

            changes.Add(new ComponentChangeOrAdd()
            {
                isNewComponent = false,
                componentIndex = 0,
                componentType = "Transform",
                changes = fieldChanges
            });
            //Debug.Log("diffing " + obj.name + "'s transform");
        }
    }

    string process = "";
    private void LoadProgress(string operation, string process, float percent)
    {
        this.process = process;
        EditorUtility.DisplayProgressBar("HKEdit", process + " (" + operation + ")", percent / 100f);
    }
    private void LoadProgress(string operation, float percent)
    {
        EditorUtility.DisplayProgressBar("HKEdit", process + " (" + operation + ")", percent / 100f);
    }
}
