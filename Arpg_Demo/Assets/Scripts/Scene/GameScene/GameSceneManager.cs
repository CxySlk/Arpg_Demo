using System.Collections;
using System.Collections.Generic;
using cfg;
using Data;
using Level;
using LLKFrame;
using Sirenix.OdinInspector;
using UI.GameScene;
using UnityEngine;
using YooAsset;

public class GameSceneManager : SingletonMonoBase<GameSceneManager>
{
#if UNITY_EDITOR
    [LabelText("是否测试")] public bool isTest;

    [LabelText("是否创建存档"), ShowIf("@isTest==true")]
    public bool isCreateArchive;

    [LabelText("玩家配置ID"), ShowIf("@isTest==true")]
    public int playerConfigID;
#endif

    /// <summary>本场景加载的角色配置表，OnDestroy 时释放</summary>
    private IReadOnlyList<PlayerConfig> _playerConfigList;

    [LabelText("玩家根物体"), SerializeField, Required]
    private Transform _playerRoot;
    
    protected void Start()
    {
        // 播放背景音乐
        int levelId = LevelManager.LevelID;
        string bgMusicName = ConfigManager.Tables.TbLevelConfig.Get(levelId).BgMusic;
        // 加载音乐
        StartCoroutine(PlayBgMusicAsync(bgMusicName));

        CreatePlayer();
        UISystem.Show<UI_GameWindow>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// 播放音乐
    /// </summary>
    /// <param name="location">在YooAsset里的地址</param>
    /// <returns></returns>
    private IEnumerator PlayBgMusicAsync(string location)
    {
        AssetHandle handle = YooAssetMgr.Instance.LoadAssetAsync<AudioClip>(YooAssetMgr.AudioPackage, location);
        yield return handle;
        if (handle.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError("加载音频失败：" + handle.Status);
            yield break;
        }
        AudioSystem.PlayBgMusic(handle.AssetObject as AudioClip);
    }

    private void OnDestroy()
    {
        // TODO：释放YooAsset加载的音乐
        
        DataManager.SetPlayerConfig(null);
        // 只释放自己加载过的那份引用（Start 之前被销毁时不能去减别人的计数）
        if (_playerConfigList == null) return;
    }

    // 角色创建及初始化
    private void CreatePlayer()
    {
        // 获取角色配置
        _playerConfigList = ConfigManager.Tables.TbPlayerConfig.DataList;
#if UNITY_EDITOR
        if (isTest)
        {
            PlayerConfig playerConfig = _playerConfigList[playerConfigID];
            PlayerController player = ResSystem.LoadAsset<GameObject>(playerConfig.PrefabPath)
                .GetComponentInChildren<PlayerController>();
            player.transform.parent.SetParent(_playerRoot);
            player.transform.parent.localPosition = Vector3.zero;
            player.transform.parent.localRotation = Quaternion.identity;
            DataManager.SetPlayerConfig(playerConfig);
            // 用 DataManager 创建存档：内部会删除旧存档、创建新存档并赋值 DataManager.Archive，
            // 否则 Archive 为 null，随后 LoadPlayerData 会空引用
            if (isCreateArchive)
            {
                DataManager.CreateArchive();
            }

            DataManager.LoadPlayerData();
            player.Init(playerConfig, DataManager.PlayerData.skillLearnData);
            return;
        }
#endif

        DataManager.LoadPlayerData();
        PlayerConfig config = _playerConfigList[DataManager.PlayerData.playerConfigID];
        DataManager.SetPlayerConfig(config);
        GameObject playerObj = ResSystem.LoadAsset<GameObject>(config.PrefabPath);
        playerObj.transform.SetParent(_playerRoot);
        playerObj.transform.localPosition = Vector3.zero;
        playerObj.transform.localRotation = Quaternion.identity;
        PlayerController playerController = playerObj.GetComponentInChildren<PlayerController>();
        playerController.Init(config, DataManager.PlayerData.skillLearnData);
    }

    private void OpenOrCloseBag()
    {
        if (UISystem.GetWindow<UI_SkillBagWindow>() is { IsShow: true })
            UISystem.Close<UI_SkillBagWindow>();
        else UISystem.Show<UI_SkillBagWindow>();
    }

    private void OpenOrCloseSetting()
    {
        if (UISystem.GetWindow<UI_GameSettingWindow>() is { IsShow: true })
            UISystem.Close<UI_GameSettingWindow>();
        else UISystem.Show<UI_GameSettingWindow>();
    }
}