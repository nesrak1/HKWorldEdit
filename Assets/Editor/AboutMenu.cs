using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AboutMenu : EditorWindow
{
    [MenuItem("HKEdit/About HKEdit", priority = 44)]
    public static void About()
    {
        //EditorWindow window = GetWindow<AboutMenu>();
        AboutMenu window = CreateInstance(typeof(AboutMenu)) as AboutMenu;
        window.titleContent = new GUIContent("About HKEdit");
        window.minSize = new Vector2(460, 250);
        window.maxSize = new Vector2(460, 250);
        window.ShowUtility();
        //window.Show();
    }

    void OnGUI()
    {
        Texture2D tex = new Texture2D(256, 256);
        ImageConversion.LoadImage(tex, File.ReadAllBytes("Assets/Editor/icon.png"));
        GUILayout.BeginHorizontal();
        GUILayout.Label(tex);
        GUILayout.BeginVertical(GUILayout.Height(240));
        GUILayout.FlexibleSpace();
        GUILayout.Label("HKEdit by nes\nAssetsTools by DerPopo\nHollow Knight by Team Cherry");
        if (GUILayout.Button("OK"))
        {
            Close();
        }
        GUILayout.EndHorizontal();
    }
}
