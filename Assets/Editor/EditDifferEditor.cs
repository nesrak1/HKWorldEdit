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
    SerializedProperty origPathId;
    SerializedProperty newAsset;
    GUIStyle wrapStyle;
    GUIContent newAssetLabel;

    void OnEnable()
    {
        fileId = serializedObject.FindProperty("fileId");
        pathId = serializedObject.FindProperty("pathId");
        origPathId = serializedObject.FindProperty("origPathId");
        newAsset = serializedObject.FindProperty("newAsset");
        if (wrapStyle == null)
        {
            wrapStyle = new GUIStyle(EditorStyles.label);
            wrapStyle.wordWrap = true;
        }
        if (newAssetLabel == null)
        {
            newAssetLabel = new GUIContent("New Asset?");
        }
        
        if (!Application.isPlaying)
        {
            EditDiffer differ = (EditDiffer)target;
            if (differ.newAsset)
            {
                EditDiffer.lastId = differ.pathId;
            }
            else
            {
                EditDiffer.lastId = 0;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("HKEdit Differ Data");
        EditorGUILayout.LabelField("If you copied this gameobject, make sure the pathId is different than the object you copied.", wrapStyle);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(fileId);
        EditorGUILayout.PropertyField(pathId);
        if (origPathId.longValue != pathId.longValue)
        {
            EditorGUILayout.PropertyField(origPathId);
        }
        EditorGUILayout.PropertyField(newAsset, newAssetLabel);
        GUI.enabled = true;
        serializedObject.ApplyModifiedProperties();
    }

    public void OnDestroy()
    {
        if (Application.isPlaying)
            return;
        if ((EditDiffer)target == null && EditDiffer.lastId != 0)
        {
            Debug.Log("Freed ID " + EditDiffer.lastId);
            EditDiffer.usedIds.Remove(EditDiffer.lastId);
            EditDiffer.lastId = 0;
        }
    }
}