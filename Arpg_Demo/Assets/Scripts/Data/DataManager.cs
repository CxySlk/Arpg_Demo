using LLKFrame;


#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

public static class DataManager
{
    // 存档相关
    public static ArchiveItem Archive { get; private set; }
    public static bool HasArchive => Archive != null;
    // 音量相关
    public static VolumeData VolumeData { get; private set; }
    // 角色配置
    public static PlayerConfig PlayerConfig { get; private set; }
    public static PlayerData PlayerData { get; private set; }

    public static void Init()
    {
        // 加载存档
        LoadArchive();

        if (HasArchive)
        {
            // 加载玩家数据
            LoadPlayerData();
        }

        // 加载音量数据
        VolumeData = SaveSystem.GetGlobalSetting<VolumeData>();
    }

    #region 存档通用相关
    private static void LoadArchive() => Archive = SaveSystem.GetArchive(0);
    public static void CreateArchive()
    {
        // 删除之前的存档
        if (HasArchive)
            SaveSystem.DeleteAllArchive();

        // 创建新的存档
        Archive = SaveSystem.CreateArchive();
    }

    public static TObj LoadArchiveFile<TObj>(string fileName) where TObj : new()
    {
#if UNITY_EDITOR
        if (Archive == null) LogWarning($"DataManager：当前存档为空，读取失败");
#endif
        return SaveSystem.GetFile<TObj>(fileName, Archive);
    }
    public static TObj LoadArchiveFile<TObj>() where TObj : new() => LoadArchiveFile<TObj>(typeof(TObj).Name);
    public static void SaveArchiveFile(object obj, string fileName)
    {
#if UNITY_EDITOR
        if (Archive == null)
            LogWarning($"DataManager：存档为空");
#endif
        SaveSystem.SaveFile(obj, fileName, Archive);
    }
    public static void SaveArchiveFile(object obj) => SaveArchiveFile(obj, obj.GetType().Name);
    #endregion
    #region 音量相关
    public static void SaveVolumeData() => SaveSystem.SaveGlobalSetting(VolumeData);
    public static void SetBgVolume(float volume) => VolumeData.bgVolume = volume;
    public static void SetEffVolume(float volume) => VolumeData.effVolume = volume;
    public static void SetGlobalVolume(float volume) => VolumeData.globalVolume = volume;
    #endregion
    #region 角色数据相关
    public static void SetPlayerConfig(PlayerConfig playerConfig) => DataManager.PlayerConfig = playerConfig;
    public static void InitPlayerData(int playerConfigID)
    {
        PlayerData = new PlayerData();
        PlayerData.playerConfigID = playerConfigID;
    }
    public static void SavePlayerData() => SaveSystem.SaveFile(PlayerData, Archive);
    public static void LoadPlayerData() => PlayerData = SaveSystem.GetFile<PlayerData>(Archive);
    #endregion
}
