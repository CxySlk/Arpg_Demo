using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    /// <summary>GAS 编辑器入口：打开标签数据窗口。</summary>
    public static class GASEditor
    {
        [MenuItem("GAS/GAS标签管理")]
        public static void OpenGASData()
        {
            GASEditorWindow wnd = EditorWindow.GetWindow<GASEditorWindow>();
            wnd.titleContent = new GUIContent("GAS 标签数据");
            wnd.minSize = new Vector2(400, 300);
        }
    }
}
