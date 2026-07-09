using MessagePack;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// 存档系统，提供存档路径管理及编辑器快捷操作
    /// </summary>
    public static class SaveSystem
    {
        #region 内部类
        [MessagePackObject]
        public class SaveItem
        {
            [Key(0)] public int saveID;              // 存档ID
            [Key(1)] public float lastModifyTime;    // 上次修改时间
            public SaveItem() { }
            public SaveItem(int saveID, float lastModifyTime)
            {
                this.saveID = saveID;
                this.lastModifyTime = lastModifyTime;
            }

            public void UpdateModifyTime(float modifyTime) => lastModifyTime = modifyTime;
        }

        [MessagePackObject]
        public class SaveInfo
        {
            [Key(0)] public Dictionary<int, SaveItem> saveItemDic = new Dictionary<int, SaveItem>();    // 存档列表
            [Key(1)] public int createSaveID;    // 当前用于创建存档的ID

            public void CreateSaveItem()
            {
                SaveItem saveItem = new SaveItem(createSaveID, Time.time);
                saveItemDic.Add(createSaveID, saveItem);
                createSaveID++;
            }
            public void DeleteSaveItem(int saveID) => saveItemDic.Remove(saveID);
            public void CleanAllSave()
            {
                saveItemDic.Clear();
                createSaveID = 0;
            }
        }
        #endregion

        /// <summary>存档文件存放路径（StreamingAssets 下）</summary>
        private static readonly string savePath = Application.persistentDataPath + "/SavePath/";
        /// <summary>存档信息存放路径（StreamingAssets 下）</summary>
        private const string saveInfoFileName = "SaveInfo";
        private static readonly string saveInfoPath = savePath + saveInfoFileName;
        /// <summary>当前有哪些存档</summary> 
        private static SaveInfo saveInfo;

        public static void Init()
        {
            // 创建存档根目录
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            // 读取存档信息
            saveInfo = LoadObject<SaveInfo>(saveInfoPath);
        }
        #region MenuItem
#if UNITY_EDITOR
        [MenuItem("LLKFrame/打开存档路径")]
        private static void OpenSavePath()
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            Process.Start(savePath);
        }

        [MenuItem("LLKFrame/清空所有存档")]
        private static void CleanAllSave()
        {
            if (!Directory.Exists(savePath))
                return;

            if (EditorUtility.DisplayDialog("清理存档", "确定要删除所有存档文件吗？此操作不可恢复。", "确定", "取消"))
            {
                Directory.Delete(savePath, true);
                Directory.CreateDirectory(savePath);
                UnityEngine.Debug.Log($"存档已清理：{savePath}");
            }
        }
#endif
        #endregion
        #region 文件的基础操作
        private static void SaveObject(object obj, string directory, string fileName)
        {
#if UNITY_EDITOR
            if (obj.GetType().GetCustomAttribute<MessagePackObjectAttribute>() == null)
                UnityEngine.Debug.LogError($"LLKFrame：{obj.GetType()}序列化必须有MessagePackObject特性");
#endif
            // 创建目录
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            // 二进制序列化
            using (FileStream stream = new FileStream(directory + fileName, FileMode.OpenOrCreate))
            {
                byte[] bytes = MessagePackSerializer.Serialize(obj);
                stream.Write(bytes);
            }
        }
        private static T LoadObject<T>(string path) where T : new()
        {
#if UNITY_EDITOR
            if (typeof(T).GetCustomAttribute<MessagePackObjectAttribute>() == null)
                UnityEngine.Debug.LogError($"LLKFrame：{typeof(T)}反序列化必须有MessagePackObject特性");
#endif
            if (!File.Exists(path))
                return new T();

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                return MessagePackSerializer.Deserialize<T>(stream);
            }
        }
        #endregion
        #region 存档相关
        /// <summary>
        /// 保存存档信息
        /// </summary>
        public static void SaveSaveInfo()
        {
            SaveObject(saveInfo, savePath, saveInfoFileName);
        }

        /// <summary>
        /// 新建一个存档
        /// </summary>
        public static void AddSave()
        {
            // 冗余操作，防止玩家在游戏外已经删除了存档文件夹
            string saveItemPath = $"{savePath}/{saveInfo.createSaveID}";
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);
            // 创建存档文件
            Directory.CreateDirectory(saveItemPath);

            // 把存档信息存入到列表中
            saveInfo.CreateSaveItem();
            SaveSaveInfo();
        }
        /// <summary>
        /// 删除指定存档
        /// </summary>
        /// <param name="saveID"></param>
        public static void DeleteSave(int saveID = 0)
        {
            // 删除存档文件
            string saveItemPath = $"{savePath}/{saveID}";
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);

            // 存档列表信息删除存档
            saveInfo.DeleteSaveItem(saveID);
            SaveSaveInfo();
        }
        /// <summary>
        /// 删除所有存档
        /// </summary>
        public static void DeleteAllSave()
        {
            // 删除所有存档
            if (Directory.Exists(savePath))
                Directory.Delete(savePath, true);
            Directory.CreateDirectory(savePath);

            // 更新存档列表信息
            saveInfo.CleanAllSave();
            SaveSaveInfo();
        }

        private static string GetSavePathBySaveID(int saveID) => savePath + saveID + "/";
        private static string GetSaveFilePath(int saveID, string fileName) => savePath + saveID + "/" + fileName;

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="saveID"></param>
        public static void SaveFile(object obj, string fileName, int saveID = 0)
        {
            if (saveInfo.saveItemDic.TryGetValue(saveID, out SaveItem saveItem))
            {
                SaveObject(obj, GetSavePathBySaveID(saveID), fileName);
                saveItem.UpdateModifyTime(Time.time);
                SaveSaveInfo();
            }
#if UNITY_EDITOR
            else UnityEngine.Debug.LogError($"LLKFrame：存档ID为{saveID}的存档不存在");
#endif
        }
        public static void SaveFile(object obj, int saveID = 0) => SaveFile(obj, obj.GetType().Name, saveID);

        public static void DeleteFile(string fileName, int saveID = 0)
        {
            if (saveInfo.saveItemDic.TryGetValue(saveID, out SaveItem saveItem))
            {
                string filePath = GetSaveFilePath(saveID, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    saveItem.UpdateModifyTime(Time.time);
                    SaveSaveInfo();
                }
            }
#if UNITY_EDITOR
            else UnityEngine.Debug.LogError($"LLKFrame：存档ID为{saveID}的存档不存在");
#endif
        }
        #endregion
    }
}
