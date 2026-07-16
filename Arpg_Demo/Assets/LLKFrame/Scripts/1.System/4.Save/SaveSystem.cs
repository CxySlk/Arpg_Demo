using MessagePack;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

namespace LLKFrame
{
    /// <summary>
    /// 存档系统，提供存档路径管理及编辑器快捷操作
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>存档文件存放路径（StreamingAssets 下）</summary>
        public static readonly string savePath = Application.persistentDataPath + "/SavePath/";
        /// <summary>存档信息存放路径（StreamingAssets 下）</summary>
        private const string saveInfoFileName = "ArchiveInfos";
        private static readonly string saveInfoPath = savePath + saveInfoFileName;
        /// <summary>当前有哪些存档</summary> 
        public static ArchiveInfos ArchiveInfo { get; private set; }

#if UNITY_EDITOR
        static SaveSystem()
        {
            Init();
        }
#endif

        public static void Init()
        {
            // 创建存档根目录
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            // 读取存档信息
            ArchiveInfo = LoadObject<ArchiveInfos>(saveInfoPath);
        }

        #region 文件的基础操作
        private static void SaveObject(object obj, string directory, string fileName)
        {
#if UNITY_EDITOR
            if (obj.GetType().GetCustomAttribute<MessagePackObjectAttribute>() == null)
                LogError($"LLKFrame：{ErrorColor(obj.GetType())}序列化必须有MessagePackObject特性");
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
                LogError($"LLKFrame：{ErrorColor(typeof(T))}反序列化必须有MessagePackObject特性");
#endif
            if (!File.Exists(path))
                return new T();

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                return MessagePackSerializer.Deserialize<T>(stream);
            }
        }
        #endregion
        #region 通用
        private static string GetSavePathBySaveID(int saveID) => savePath + saveID + "/";
        private static string GetSaveFilePath(int saveID, string fileName) => savePath + saveID + "/" + fileName;

        #endregion
        #region 存档相关
        /// <summary>保存存档信息</summary>
        private static void SaveArchiveInfo() => SaveObject(ArchiveInfo, savePath, saveInfoFileName);
        /// <summary>新建一个存档</summary>
        public static ArchiveItem CreateArchive()
        {
            // 冗余操作，防止玩家在游戏外已经删除了存档文件夹
            string saveItemPath = $"{savePath}/{ArchiveInfo.createSaveID}";
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);
            // 创建存档文件
            Directory.CreateDirectory(saveItemPath);

            // 把存档信息存入到列表中
            ArchiveItem archiveItem = ArchiveInfo.CreateArchiveItem();
            SaveArchiveInfo();
            return archiveItem;
        }
        /// <summary>获取存档</summary>
        public static ArchiveItem GetArchive(int saveID)
        {
            ArchiveInfo.archiveItemDic.TryGetValue(saveID, out ArchiveItem archiveItem);
            return archiveItem;
        }

        /// <summary>删除指定存档</summary>
        public static void DeleteArchive(int saveID = 0)
        {
            // 删除存档文件
            string saveItemPath = $"{savePath}/{saveID}";
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);

            // 存档列表信息删除存档
            ArchiveInfo.DeleteArchiveItem(saveID);
            SaveArchiveInfo();
        }
        public static void DeleteArchive(ArchiveItem archiveItem) => DeleteArchive(archiveItem.saveID);
        /// <summary>删除所有存档</summary>
        public static void DeleteAllArchive()
        {
            string saveItemPath;
            foreach (KeyValuePair<int, ArchiveItem> item in ArchiveInfo.archiveItemDic)
            {
                saveItemPath = $"{savePath}/{item.Key}";
                if (Directory.Exists(saveItemPath))
                    Directory.Delete(saveItemPath, true);
            }
            // 更新存档列表信息
            ArchiveInfo.archiveItemDic.Clear();
            ArchiveInfo.CleanAllArchive();
            SaveArchiveInfo();
        }

        #endregion
        #region 保存文件
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="saveID"></param>
        public static void SaveFile(object obj, string fileName, int saveID = 0)
        {
            if (ArchiveInfo.archiveItemDic.TryGetValue(saveID, out ArchiveItem saveItem))
            {
                SaveObject(obj, GetSavePathBySaveID(saveID), fileName);
                saveItem.UpdateModifyTime(Time.time);
                SaveArchiveInfo();
            }
#if UNITY_EDITOR
            else LogError($"LLKFrame：存档ID为{ErrorColor(saveID)}的存档不存在");
#endif
        }
        public static void SaveFile(object obj, int saveID = 0) => SaveFile(obj, obj.GetType().Name, saveID);
        public static void SaveFile(object obj, string fileName, ArchiveItem archiveItem) => SaveFile(obj, fileName, archiveItem.saveID);
        public static void SaveFile(object obj, ArchiveItem archiveItem) => SaveFile(obj, obj.GetType().Name, archiveItem.saveID);
        #endregion
        #region 获取文件
        /// <summary>
        /// 获取存储文件
        /// </summary>
        public static TObj GetFile<TObj>(string fileName, int saveID = 0) where TObj : new()
        {
            string filePath = GetSaveFilePath(saveID, fileName);
            TObj obj = LoadObject<TObj>(filePath);
            return obj;
        }
        public static TObj GetFile<TObj>(int saveID = 0) where TObj : new() => GetFile<TObj>(typeof(TObj).Name, saveID);
        /// <summary>获取存储文件</summary>
        public static TObj GetFile<TObj>(string fileName, ArchiveItem archiveItem) where TObj : new() => GetFile<TObj>(fileName, archiveItem.saveID);
        public static TObj GetFile<TObj>(ArchiveItem archiveItem) where TObj : new() => GetFile<TObj>(typeof(TObj).Name, archiveItem.saveID);
        #endregion
        #region 删除文件
        public static void DeleteFile(string fileName, int saveID = 0)
        {
            if (ArchiveInfo.archiveItemDic.TryGetValue(saveID, out ArchiveItem saveItem))
            {
                string filePath = GetSaveFilePath(saveID, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    saveItem.UpdateModifyTime(Time.time);
                    SaveArchiveInfo();
                }
            }
#if UNITY_EDITOR
            else LogError($"LLKFrame：存档ID为{ErrorColor(saveID)}的存档不存在");
#endif
        }
        public static void DeleteFile(string fileName, ArchiveItem archiveItem) => DeleteFile(fileName, archiveItem.saveID);
        #endregion
        #region 全局设置
        public static void SaveGlobalSetting(object obj, string fileName) => SaveObject(obj, savePath, fileName);
        public static void SaveGlobalSetting(object obj) => SaveObject(obj, savePath, obj.GetType().Name);
        public static TObj GetGlobalSetting<TObj>(string fileName) where TObj : new() => LoadObject<TObj>(savePath + fileName);
        public static TObj GetGlobalSetting<TObj>() where TObj : new() => LoadObject<TObj>(savePath + typeof(TObj).Name);
        #endregion
    }
}
