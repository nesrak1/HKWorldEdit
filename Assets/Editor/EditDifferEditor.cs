using Assets.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditDiffer))]
[CanEditMultipleObjects]
public class EditDifferEditor : Editor
{
    SerializedProperty fileId;
    SerializedProperty pathId;

    void OnEnable()
    {
        fileId = serializedObject.FindProperty("fileId");
        pathId = serializedObject.FindProperty("pathId");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("HKEdit Differ Data");
        GUI.enabled = false;
        EditorGUILayout.PropertyField(fileId);
        EditorGUILayout.PropertyField(pathId);
        GUI.enabled = true;
        serializedObject.ApplyModifiedProperties();
    }
}