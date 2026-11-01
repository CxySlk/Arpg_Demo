using cfg;
using Data;
using LLKFrame;


#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

public static class DataManager
{
    // 存档相关
    public static LoginData LoginData { get; private set; }

    private static ArchiveItem _archive;
    public static ArchiveItem Archive => _archive;

    public static ArchiveInfos ArchiveInfo => SaveSystem.ArchiveInfo;

    // 音量相关
    public static VolumeData VolumeData { get; private set; }

    // 角色配置
    public static PlayerConfig PlayerConfig { get; private set; }
    public static PlayerData PlayerData { get; private set; }
    
    // 关卡数据
    public static LevelData LevelData { get;private set; }

    public static void Init()
    {
        // 加载登录数据
        LoginData = SaveSystem.LoadGlobalSetting<LoginData>();
        // 加载音量数据
        VolumeData = SaveSystem.LoadGlobalSetting<VolumeData>();
    }

    #region 存档通用相关

    public static bool HasArchive() => ArchiveInfo.HasArchive(LoginDataItem.saveKey);

    public static void LoadArchive() => SaveSystem.TryGetArchive(LoginDataItem.saveKey, out _archive);

    public static void CreateArchive()
    {
        // 删除之前的存档
        if (LoginDataItem.saveKey >= 0)
            SaveSystem.DeleteArchive(LoginDataItem.saveKey);

        // 创建新的存档，保存数据
        _archive = SaveSystem.CreateArchive();
        LoginDataItem.saveKey = _archive.saveID;
        SaveLoginData();
    }

    public static TObj LoadArchiveFile<TObj>(string fileName) where TObj : new()
    {
#if UNITY_EDITOR
        if (Archive == null) LogWarning($"DataManager：当前存档为空，读取失败");
#endif
        return SaveSystem.LoadFile<TObj>(fileName, Archive);
    }

    public static TObj LoadArchiveFile<TObj>() where TObj : new() => LoadArchiveFile<TObj>(typeof(TObj).Name);

    public static void SaveArchiveFile<T>(T obj, string fileName)
    {
#if UNITY_EDITOR
        if (Archive == null)
            LogWarning($"DataManager：存档为空");
#endif
        SaveSystem.SaveFile(obj, fileName, Archive);
    }

    public static void SaveArchiveFile<T>(T obj) => SaveArchiveFile(obj, obj.GetType().Name);

    #endregion

    #region 音量相关

    public static void SaveVolumeData() => SaveSystem.SaveGlobalSetting(VolumeData);
    public static void SetBgVolume(float volume) => VolumeData.bgVolume = volume;
    public static void SetEffVolume(float volume) => VolumeData.effVolume = volume;
    public static void SetGlobalVolume(float volume) => VolumeData.globalVolume = volume;

    #endregion

    #region 角色数据相关

    public static void SetPlayerConfig(PlayerConfig playerConfig) => PlayerConfig = playerConfig;

    public static void InitPlayerData(int playerConfigID)
    {
        PlayerData = new()
        {
            playerConfigID = playerConfigID
        };
    }

    public static void SavePlayerData()
    {
#if UNITY_EDITOR
        // 没有存档（直接 Play GameScene 调试）时不写盘，避免 SaveSystem 空引用
        if (Archive == null) return;
#endif
        SaveSystem.SaveFile(PlayerData, Archive);
    }

    public static PlayerData LoadPlayerData()
    {
#if UNITY_EDITOR
        // 没有存档（例如直接 Play GameScene 调试）时给一份空数据，避免 SaveSystem 空引用
        if (Archive == null)
        {
            PlayerData = new();
            return null;
        }
#endif
        PlayerData = SaveSystem.LoadFile<PlayerData>(Archive);
        return PlayerData;
    }

    #endregion

    #region 账号数据

    public static LoginDataItem LoginDataItem { get; private set; }

    public static void SaveLoginData()
    {
        SaveSystem.SaveGlobalSetting(LoginData);
    }

    public static void SetLoginData(LoginDataItem loginDataItem)
    {
        LoginDataItem = loginDataItem;
    }

    #endregion

    #region 关卡数据

    public static LevelData LoadLevelData()
    {
        LevelData =  SaveSystem.LoadFile<LevelData>(_archive);
        return LevelData;
    }

    public static void SaveLevelData()
    {
        SaveSystem.SaveFile(LevelData, Archive);
    }
    
    #endregion
}