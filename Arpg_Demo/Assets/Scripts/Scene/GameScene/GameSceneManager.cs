using System.Collections;
using System.Collections.Generic;
using cfg;
using Character;
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
        // 游戏胜利
        EventCenter.Global.Subscribe(CharacterEventKey.EnemyAllDeathEventKey, OnGameWin);
        // 游戏失败
        EventCenter.Global.Subscribe(CharacterEventKey.PlayerDeathEventKey, OnGameFail);
    }

    private void OnGameFail(PlayerController obj)
    {
        Cursor.lockState = CursorLockMode.None;
        var window = UISystem.Show<UI_TipWindow>();
        window.ShowTip("游戏失败", "您已死亡，关卡结束！");
        window.SetCallBack(OnGameOver);
    }

    private void OnDisable()
    {
        EventCenter.Global.UnSubscribe(CharacterEventKey.EnemyAllDeathEventKey, OnGameWin);
        EventCenter.Global.UnSubscribe(CharacterEventKey.PlayerDeathEventKey, OnGameFail);
    }

    private void OnGameWin()
    {
        Cursor.lockState = CursorLockMode.None;
        // 关卡胜利：只有首次通关才加技能点/解锁下一关
        bool firstClear = LevelManager.CompleteLevel();
        DataManager.SaveLevelData();
        DataManager.SavePlayerData();

        var window = UISystem.Show<UI_TipWindow>();
        if (firstClear)
        {
            LevelConfig lf = ConfigManager.Tables.TbLevelConfig.Get(LevelManager.LevelID);
            window.ShowTip(
                "游戏胜利！",
                $"获得技能点：{lf.CompeleteSkillPoints}\n是否返回关卡选择界面？");
        }
        else
        {
            window.ShowTip("游戏胜利！", "当前关卡已通关，是否返回关卡选择界面？");
        }

        // 确定返回关卡选择；叉只关闭弹窗，留在当前场景
        window.SetSureCallBack(OnGameOver);
        window.SetCancelCallBack(OnStayInLevel);
    }

    private void OnStayInLevel()
    {
        // 取消返回选关后，重新锁定鼠标，继续在当前场景操作
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnGameOver()
    {
        UISystem.CloseAll();
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("SelectLevelScene", UISystem.Close<UI_LoadingSceneWindow>);

        // 播放背景音乐
        var handle = YooAssetMgr.Instance.LoadAssetSync<AudioClip>(YooAssetMgr.AudioPackage, "BgMusic");
        if (handle.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError("加载音频失败！");
            handle.Release();
            return;
        }

        var clip = handle.AssetObject as AudioClip;
        AudioSystem.PlayBgMusic(clip);
        handle.Release();
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
            handle.Release();
            yield break;
        }

        AudioSystem.PlayBgMusic(handle.AssetObject as AudioClip);
        handle.Release();
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
        {
            UISystem.Close<UI_GameSettingWindow>();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else UISystem.Show<UI_GameSettingWindow>();
    }
}