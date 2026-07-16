using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LLKFrame
{
    public static class LLKFrameEditor
    {
        private static readonly string EventTypePath = $"Assets/LLKFrame/Scripts/1.System/1.Event/{nameof(EventCenterType)}.cs";
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
        #region 事件中心相关
        [MenuItem("LLKFrame/打开事件中心的事件枚举类")]
        private static void OpenEventCenterType()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(EventTypePath);
            if (asset == null)
                LogSystem.LogError("打开事件中心的事件类型枚举失败，路径：", LogSystem.ErrorColor(EventTypePath));
            else
                AssetDatabase.OpenAsset(asset);
        }
        #endregion
    }
}
