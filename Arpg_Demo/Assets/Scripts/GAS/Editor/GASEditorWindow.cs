using UnityEditor;
using UnityEngine;

public class GASEditorWindow : EditorWindow
{
    private GASEditorData data;
    private SerializedObject serializedData;
    private Vector2 scrollPos;

    private void OnEnable()
    {
        data = GASEditorData.Instance;
        serializedData = new SerializedObject(data);
    }

    private void OnGUI()
    {
        if (data == null || serializedData == null) return;

        serializedData.Update();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("GAS 数据", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        SerializedProperty prop = serializedData.GetIterator();
        prop.NextVisible(true);
        while (prop.NextVisible(false))
            EditorGUILayout.PropertyField(prop, true);

        serializedData.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
