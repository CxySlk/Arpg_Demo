using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GASEditorData : SerializedScriptableObject
{
#if UNITY_EDITOR
    private const string AssetPath = "Assets/Scripts/GAS/EditorData/GASEditorData.asset";
    private static GASEditorData instance;
    public static GASEditorData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = AssetDatabase.LoadAssetAtPath<GASEditorData>(AssetPath);
                if (instance == null)
                {
                    instance = CreateInstance<GASEditorData>();
                    Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                    AssetDatabase.CreateAsset(instance, AssetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            return instance;
        }
    }

    public List<string> Tags = new List<string>();
    public List<string> Attributes = new List<string>();
#endif
}
