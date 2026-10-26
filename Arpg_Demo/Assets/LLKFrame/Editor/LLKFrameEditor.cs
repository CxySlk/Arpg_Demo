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
            if (!Directory.Exists(SaveSystem.SavePath))
                Directory.CreateDirectory(SaveSystem.SavePath);

            Process.Start(SaveSystem.SavePath);
        }

        [MenuItem("LLKFrame/清空所有存档")]
        private static void CleanAllSave()
        {
            if (!Directory.Exists(SaveSystem.SavePath))
                return;

            if (EditorUtility.DisplayDialog("清理存档", "确定要删除所有存档文件吗？此操作不可恢复。", "确定", "取消"))
            {
                Directory.Delete(SaveSystem.SavePath, true);
                Directory.CreateDirectory(SaveSystem.SavePath);
                // 目录被整个删掉了，内存缓存与存档列表都要重新装载
                SaveSystem.Init();
                UnityEngine.Debug.Log($"存档已清理：{SaveSystem.SavePath}");
            }
        }
#endif
        #endregion
    }
}
