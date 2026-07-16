using LLKFrame;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[UIWindow(nameof(UI_ChooseCharacterWindow), nameof(UI_ChooseCharacterWindow), false, 0)]
public class UI_ChooseCharacterWindow : UI_WindowBase
{
    // 角色切换按钮
    [LabelText("左切换按钮"), SerializeField] private Button btnLeft;
    [LabelText("右切换按钮"), SerializeField] private Button btnRight;
    // 开始按钮
    [LabelText("确定按钮"), SerializeField] private Button btnSure;
    [LabelText("返回按钮"), SerializeField] private Button btnBack;
    // 拖拽区域
    [LabelText("角色模型拖拽区域"), SerializeField] private Image imgModelDrag;
    // 角色文本相关
    [LabelText("角色名"), SerializeField] private TMP_Text txtNmae;
    [LabelText("角色简介"), SerializeField] private TMP_Text txtDescription;
    // 属性相关
    [LabelText("攻击力"), SerializeField] private TMP_Text txtAttackValue;
    [LabelText("防御力"), SerializeField] private TMP_Text txtDefendValue;
    [LabelText("血量"), SerializeField] private TMP_Text txtHpValue;
    [LabelText("魔法值"), SerializeField] private TMP_Text txtMpValue;
    // 角色配置
    [LabelText("角色配置列表数据"), SerializeField] private PlayerListConfig playerDatas;

    private int curSelIndex = 0;

    public override void Show(int layer)
    {
        base.Show(layer);
        // 按钮事件监听
        btnLeft.onClick.AddListener(OnLeftBtnClicked);
        btnRight.onClick.AddListener(OnRightBtnClicked);

        btnSure.onClick.AddListener(() =>
        {
            // 创建新存档
            DataManager.CreateArchive();
            // 保存角色配置
            DataManager.InitPlayerData(curSelIndex);
            DataManager.SavePlayerData();

            AudioSystem.PlayEffectMusic("ButtonClick");
            LoadSceneBySceneName("GameScene");
        });

        btnBack.onClick.AddListener(() =>
        {
            AudioSystem.PlayEffectMusic("ButtonClick");
            LoadSceneBySceneName("BeginScene");
        });

        // 显示第一个角色配置
        ChangeCharacter(0);
        // 监听拖拽模型事件
        imgModelDrag.AddEventListener<PointerEventData>(EventSystemType.OnDrag, (data) => ChooseCharacter.Instance.RotateModel(-data.delta.x));
    }

    private void LoadSceneBySceneName(string sceneName)
    {
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync(sceneName, () =>
        {
            UISystem.Close<UI_LoadingSceneWindow>();
        });
        UISystem.Close<UI_ChooseCharacterWindow>();
    }

    public void OnLeftBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        ChangeCharacter(curSelIndex - 1);
    }

    public void OnRightBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        ChangeCharacter(curSelIndex + 1);
    }

    private void ChangeCharacter(int index)
    {
        if (index < 0 || index >= playerDatas.PlayerDataList.Count) return;
        curSelIndex = index;
        PlayerConfig playerData = playerDatas.PlayerDataList[curSelIndex];
        ChooseCharacter.Instance.CreateCharacter(playerData.Prefab, playerData.StandardAnimationDic["Idle"]);
        txtNmae.text = playerData.PlayerName;
        txtDescription.text = playerData.Description;
        txtAttackValue.text = playerData.AttackBaseValue.ToString();
        txtDefendValue.text = playerData.DefendBaseValue.ToString();
        txtHpValue.text = playerData.HpBaseValue.ToString();
        txtMpValue.text = playerData.MpBaseValue.ToString();
    }
}
