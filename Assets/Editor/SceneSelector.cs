using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SceneSelector : EditorWindow
{
    AssetsManager am = null;
    string[] strings = null;
    string gameDataPath;
    public static SceneSelector ShowDialog(AssetsManager am, List<string> scenes, string gameDataPath)
    {
        SceneSelector window = GetWindow<SceneSelector>();
        window.am = am;
        window.strings = scenes.ToArray();
        window.gameDataPath = gameDataPath;
        return window;
    }

    Vector2 scrollPos;
    int selected = -1;
    void OnGUI()
    {
        if (am == null || strings == null || gameDataPath == string.Empty)
            return;
        Rect scrollViewRect = new Rect(0, 0, position.width, position.height);
        Rect selectionGridRect = new Rect(0, 0, position.width - 20, strings.Length * 20);
        scrollPos = GUI.BeginScrollView(scrollViewRect, scrollPos, selectionGridRect);
        selected = GUI.SelectionGrid(selectionGridRect, selected, strings, 1);
        GUI.EndScrollView();

        if (selected != -1)
        {
            string path = Path.Combine(gameDataPath, "level" + selected);
            HKScene scene = new HKScene(path, am);
            selected = -1;
        }
    }

    void OnEnable()
    {
        titleContent.text = "Level Selector";
    }
}
