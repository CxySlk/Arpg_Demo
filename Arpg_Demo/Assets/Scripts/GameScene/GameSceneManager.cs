using Character.Player.Config;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameSceneManager : SingletonMonoBase<GameSceneManager>
{
#if UNITY_EDITOR
    [LabelText("是否测试")] public bool isTest;

    [LabelText("是否创建存档"), ShowIf("@isTest==true")]
    public bool isCreateArchive;

    [LabelText("玩家配置ID"), ShowIf("@isTest==true")]
    public int playerConfigID;

    [LabelText("玩家配置列表"), ShowIf("@isTest==true")]
    public PlayerConfigList playerConfigList;
#endif

    [LabelText("玩家根物体"), SerializeField, Required]
    private Transform _playerRoot;

    protected void Start()
    {
        CreatePlayer();
        UISystem.Show<UI_GameWindow>();
    }

    // 角色创建及初始化
    private void CreatePlayer()
    {
#if UNITY_EDITOR
        if (isTest)
        {
            PlayerConfig playerConfig = playerConfigList.ConfigList[playerConfigID];
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
            player.SetConfig(playerConfig, DataManager.PlayerData.skillLearnData);
            return;
        }
#endif

        PlayerConfig config = DataManager.PlayerConfig;
        GameObject playerObj = ResSystem.LoadAsset<GameObject>(config.PrefabPath);
        playerObj.transform.SetParent(_playerRoot);
        playerObj.transform.localPosition = Vector3.zero;
        playerObj.transform.localRotation = Quaternion.identity;
        PlayerController playerController = playerObj.GetComponentInChildren<PlayerController>();
        playerController.SetConfig(config, DataManager.PlayerData.skillLearnData);
    }
    
    private void OpenOrCloseBag()
    {
        if (UISystem.GetWindow<UI_SkillBagWindow>() is { IsShow: true })
            UISystem.Close<UI_SkillBagWindow>();
        else UISystem.Show<UI_SkillBagWindow>();
    }
}