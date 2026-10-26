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
        public static readonly string SavePath = Application.persistentDataPath + "/SavePath/";

        /// <summary>当前有哪些存档</summary> 
        public static ArchiveInfos ArchiveInfo { get; private set; }

        /// <summary>
        /// 已加载文件的缓存：key = 文件完整路径，value = 反序列化后的对象。
        /// 命中时直接返回同一实例，省掉 File.Exists / ReadAllBytes / MessagePack 反序列化。
        /// </summary>
        private static readonly Dictionary<string, object> fileCacheDic = new();

#if UNITY_EDITOR
        static SaveSystem()
        {
            Init();
        }
#endif

        public static void Init()
        {
            // 重新从磁盘装载，丢掉上一次的内存缓存
            fileCacheDic.Clear();
            // 创建存档根目录
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
            // 读取存档信息
            ArchiveInfo = LoadGlobalSetting<ArchiveInfos>(nameof(ArchiveInfos));
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
            // 用 MessagePack 序列化后直接存储二进制
            string filePath = directory + fileName;
            byte[] bytes = MessagePackSerializer.Serialize(obj);
            File.WriteAllBytes(filePath, bytes);
            // 写穿缓存：紧接着的 Load 立刻拿到刚写入的这个对象
            fileCacheDic[filePath] = obj;
        }

        private static T LoadObject<T>(string path) where T : new()
        {
#if UNITY_EDITOR
            if (typeof(T).GetCustomAttribute<MessagePackObjectAttribute>() == null)
                LogError($"LLKFrame：{ErrorColor(typeof(T))}反序列化必须有MessagePackObject特性");
#endif
            // 命中缓存直接返回；同一路径被不同类型的 T 读取时不返回错类型，继续走读盘并覆盖缓存
            if (fileCacheDic.TryGetValue(path, out object cached) && cached is T hit)
                return hit;

            T obj;
            if (File.Exists(path))
            {
                // 直接读取二进制反序列化
                byte[] bytes = File.ReadAllBytes(path);
                obj = MessagePackSerializer.Deserialize<T>(bytes);
            }
            else obj = new T();

            fileCacheDic[path] = obj;
            return obj;
        }

        #endregion

        #region 通用

        private static string GetSavePathBySaveID(int saveID) => SavePath + saveID + "/";
        private static string GetSaveFilePath(int saveID, string fileName) => SavePath + saveID + "/" + fileName;

        #endregion

        #region 缓存

        /// <summary>清空全部缓存（外部直接删除了存档目录时使用）</summary>
        public static void ClearCache() => fileCacheDic.Clear();

        /// <summary>清掉某个存档目录下的缓存</summary>
        private static void ClearCacheBySaveID(int saveID)
        {
            string prefix = GetSavePathBySaveID(saveID);
            List<string> stalePaths = new();
            foreach (string path in fileCacheDic.Keys)
                if (path.StartsWith(prefix)) stalePaths.Add(path);
            foreach (string path in stalePaths) fileCacheDic.Remove(path);
        }

        #endregion

        #region 存档相关

        /// <summary>保存存档信息</summary>
        private static void SaveArchiveInfo() => SaveGlobalSetting(ArchiveInfo);

        public static bool HasArchive(int saveID)
        {
            return ArchiveInfo.archiveItemDic.ContainsKey(saveID);
        }

        /// <summary>新建一个存档</summary>
        public static ArchiveItem CreateArchive()
        {
            ArchiveItem archiveItem;
            while (!TryCreateArchive(ArchiveInfo.createSaveID, out archiveItem))
            {
                ArchiveInfo.createSaveID++;
            }

            ArchiveInfo.createSaveID++;
            return archiveItem;
        }

        public static bool TryCreateArchive(int saveID, out ArchiveItem archiveItem)
        {
            if (ArchiveInfo.archiveItemDic.ContainsKey(saveID))
            {
                archiveItem = null;
                return false;
            }

            // 冗余操作，防止玩家在游戏外已经删除了存档文件夹
            string saveItemPath = GetSavePathBySaveID(saveID);
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);
            // 创建存档文件
            Directory.CreateDirectory(saveItemPath);
            // 目录被整个删掉重建过，旧缓存一律作废
            ClearCacheBySaveID(saveID);

            // 把存档信息存入到列表中
            archiveItem = ArchiveInfo.CreateArchiveItem(saveID);
            return true;
        }

        /// <summary>获取存档</summary>
        public static ArchiveItem TryGetArchive(int saveID)
        {
            ArchiveInfo.archiveItemDic.TryGetValue(saveID, out ArchiveItem archiveItem);
            return archiveItem;
        }

        public static bool TryGetArchive(int saveID, out ArchiveItem archiveItem)
        {
            return ArchiveInfo.archiveItemDic.TryGetValue(saveID, out archiveItem);
        }

        /// <summary>删除指定存档</summary>
        public static void DeleteArchive(int saveID = 0)
        {
            // 删除存档文件
            string saveItemPath = $"{SavePath}/{saveID}";
            if (Directory.Exists(saveItemPath))
                Directory.Delete(saveItemPath, true);
            // 连带清掉该存档的内存缓存
            ClearCacheBySaveID(saveID);

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
                saveItemPath = $"{SavePath}/{item.Key}";
                if (Directory.Exists(saveItemPath))
                    Directory.Delete(saveItemPath, true);
                ClearCacheBySaveID(item.Key);
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

        public static void SaveFile(object obj, string fileName, ArchiveItem archiveItem) =>
            SaveFile(obj, fileName, archiveItem.saveID);

        public static void SaveFile(object obj, ArchiveItem archiveItem) =>
            SaveFile(obj, obj.GetType().Name, archiveItem.saveID);

        #endregion

        #region 获取文件

        /// <summary>
        /// 获取存储文件
        /// </summary>
        public static TObj LoadFile<TObj>(string fileName, int saveID = 0) where TObj : new()
        {
            string filePath = GetSaveFilePath(saveID, fileName);
            TObj obj = LoadObject<TObj>(filePath);
            return obj;
        }

        public static TObj LoadFile<TObj>(int saveID = 0) where TObj : new() => LoadFile<TObj>(typeof(TObj).Name, saveID);

        /// <summary>获取存储文件</summary>
        public static TObj LoadFile<TObj>(string fileName, ArchiveItem archiveItem) where TObj : new() =>
            LoadFile<TObj>(fileName, archiveItem.saveID);

        public static TObj LoadFile<TObj>(ArchiveItem archiveItem) where TObj : new() =>
            LoadFile<TObj>(typeof(TObj).Name, archiveItem.saveID);

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
                    fileCacheDic.Remove(filePath);
                    saveItem.UpdateModifyTime(Time.time);
                    SaveArchiveInfo();
                }
            }
#if UNITY_EDITOR
            else LogError($"LLKFrame：存档ID为{ErrorColor(saveID)}的存档不存在");
#endif
        }

        public static void DeleteFile(string fileName, ArchiveItem archiveItem) =>
            DeleteFile(fileName, archiveItem.saveID);

        #endregion

        #region 全局设置

        public static void SaveGlobalSetting(object obj, string fileName) => SaveObject(obj, SavePath, fileName);
        public static void SaveGlobalSetting(object obj) => SaveObject(obj, SavePath, obj.GetType().Name);

        public static TObj LoadGlobalSetting<TObj>(string fileName) where TObj : new() =>
            LoadObject<TObj>(SavePath + fileName);

        public static TObj LoadGlobalSetting<TObj>() where TObj : new() =>
            LoadObject<TObj>(SavePath + typeof(TObj).Name);

        #endregion
    }
}