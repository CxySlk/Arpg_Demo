using UnityEditor;
using UnityEngine;

public static class GASEditor
{
    [MenuItem("GAS/打开GAS数据")]
    public static void OpenGASData()
    {
        GASEditorWindow wnd = EditorWindow.GetWindow<GASEditorWindow>();
        wnd.titleContent = new GUIContent("GAS 数据");
        wnd.minSize = new Vector2(400, 300);
    }
}
