using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class HKSelect : EditorWindow
{
    List<AssetInfo> assetInfos = null;
    string[] strings = null;
    string folderPath = "";
    AssetsManager am;
    [MenuItem("HKEdit/Open FSM", priority = 0)]
    public static void ShowOpenFileDialog()
    {
        string path = EditorUtility.OpenFilePanel("Open level file", "", "");
        if (path.Length != 0)
        {
            HKSelect window = GetWindow<HKSelect>();
            window.am = new AssetsManager();
            AssetsFileInstance assetsFileInstance = window.am.LoadAssetsFile(path, true); //might work with false
            AssetsFile assetsFile = assetsFileInstance.file;
            AssetsFileTable assetsTable = assetsFileInstance.table;
            window.am.LoadClassPackage(Path.Combine(Application.dataPath, "cldb.dat"));

            window.folderPath = Path.GetDirectoryName(path);

            Stream assetStream = assetsFileInstance.stream;

            List<AssetInfo> assetInfos = new List<AssetInfo>();
            uint fsmTypeId = 0;
            foreach (AssetFileInfoEx info in assetsTable.pAssetFileInfo)
            {
                bool isMono = false;
                if (fsmTypeId == 0)
                {
                    ushort monoType = assetsFile.typeTree.pTypes_Unity5[info.curFileTypeOrIndex].scriptIndex;
                    if (monoType != 0xFFFF)
                    {
                        isMono = true;
                    }
                }
                else if (info.curFileType == fsmTypeId)
                {
                    isMono = true;
                }
                if (isMono)
                {
                    AssetTypeInstance monoAti = window.am.GetATI(assetsFile, info);
                    AssetTypeInstance scriptAti = window.am.GetExtAsset(assetsFileInstance, monoAti.GetBaseField().Get("m_Script")).instance;
                    AssetTypeInstance goAti = window.am.GetExtAsset(assetsFileInstance, monoAti.GetBaseField().Get("m_GameObject")).instance;
                    if (goAti == null) //found a scriptable object, oops
                    {
                        fsmTypeId = 0;
                        continue;
                    }
                    string m_Name = goAti.GetBaseField().Get("m_Name").GetValue().AsString();
                    string m_ClassName = scriptAti.GetBaseField().Get("m_ClassName").GetValue().AsString();

                    if (m_ClassName == "PlayMakerFSM")
                    {
                        if (fsmTypeId == 0)
                            fsmTypeId = info.curFileType;

                        BinaryReader reader = new BinaryReader(assetStream);

                        long oldPos = assetStream.Position;
                        reader.BaseStream.Position = (long)info.absoluteFilePos;
                        reader.BaseStream.Position += 28;
                        uint length = reader.ReadUInt32();
                        reader.ReadBytes((int)length);

                        long pad = 4 - (reader.BaseStream.Position % 4);
                        if (pad != 4) reader.BaseStream.Position += pad;

                        reader.BaseStream.Position += 16;

                        uint length2 = reader.ReadUInt32();
                        string fsmName = Encoding.ASCII.GetString(reader.ReadBytes((int)length2));
                        reader.BaseStream.Position = oldPos;

                        assetInfos.Add(new AssetInfo()
                        {
                            id = info.index,
                            size = info.curFileSize,
                            name = m_Name + "-" + fsmName
                        });
                    }
                }
            }
            assetInfos.Sort((x, y) => x.name.CompareTo(y.name));

            window.assetInfos = assetInfos;
            window.strings = assetInfos.Select(i => i.name).ToArray();
        }
    }

    Vector2 scrollPos;
    int selected = -1;
    void OnGUI()
    {
        if (assetInfos == null || strings == null)
            return;
        Rect scrollViewRect = new Rect(0, 0, position.width, position.height);
        Rect selectionGridRect = new Rect(0, 0, position.width - 20, strings.Length * 20);
        scrollPos = GUI.BeginScrollView(scrollViewRect, scrollPos, selectionGridRect);
        selected = GUI.SelectionGrid(selectionGridRect, selected, strings, 1);
        GUI.EndScrollView();

        if (selected != -1)
        {
            HKMenu window = GetWindow<HKMenu>();
            window.LoadFSM(am, assetInfos[selected], folderPath);
            selected = -1;
        }
    }

    void OnEnable()
    {
        titleContent.text = "FSM Browser";
    }
}
