using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LLKFrame
{
    public static class LLKFrameEditor
    {
        #region SaveSystem相关
#if UNITY_EDITOR
        [MenuItem("LLKFrame/打开存档路径")]
        private static void OpenSavePath()
        {
            if (!Directory.Exists(SaveSystem.savePath))
                Directory.CreateDirectory(SaveSystem.savePath);

            Process.Start(SaveSystem.savePath);
        }

        [MenuItem("LLKFrame/清空所有存档")]
        private static void CleanAllSave()
        {
            if (!Directory.Exists(SaveSystem.savePath))
                return;

            if (EditorUtility.DisplayDialog("清理存档", "确定要删除所有存档文件吗？此操作不可恢复。", "确定", "取消"))
            {
                Directory.Delete(SaveSystem.savePath, true);
                Directory.CreateDirectory(SaveSystem.savePath);
                UnityEngine.Debug.Log($"存档已清理：{SaveSystem.savePath}");
            }
        }
#endif
        #endregion
    }
}
