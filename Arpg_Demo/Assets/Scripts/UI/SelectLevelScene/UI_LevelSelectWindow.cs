using System.Collections.Generic;
using cfg;
using Data;
using Level;
using LLKFrame;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UI;

[UIWindow(nameof(UI_LevelSelectWindow), nameof(UI_LevelSelectWindow), false, 0)]
public class UI_LevelSelectWindow : UI_WindowBase
{
    [SerializeField] private Button _btnLeft;
    [SerializeField] private Button _btnRight;
    [SerializeField] private Button _btnSure;
    [SerializeField] private Button _btnBack;
    [SerializeField] private UI_LevelSelectView _levelSelectView;

    private IReadOnlyList<LevelConfig> _levelConfigList;

    protected override void OnShow()
    {
        // 按钮事件
        _btnLeft.onClick.AddListener(OnLeftBtnClicked);
        _btnRight.onClick.AddListener(OnRightBtnClicked);
        _btnSure.onClick.AddListener(OnSureBtnClicked);
        _btnBack.onClick.AddListener(OnBackBtnClicked);
        // 获取关卡配置
        _levelConfigList = ConfigManager.Tables.TbLevelConfig.DataList;
        _levelSelectView.Init(_levelConfigList);
    }

    private void OnBackBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.CloseAll();
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("BeginScene",UISystem.Close<UI_LoadingSceneWindow>);
    }

    [Button("上一个")]
    private void OnLeftBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        if (_levelSelectView.CurLevelIndex > 0)
            _levelSelectView.ToPreviousLevel();
    }
    
    [Button("下一个")]
    private void OnRightBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        if (_levelSelectView.CurLevelIndex < _levelConfigList.Count - 1)
            _levelSelectView.ToNextLevel();
    }
    
    private void OnSureBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        LevelConfig  levelConfig = _levelConfigList[_levelSelectView.CurLevelIndex];
        // 关卡还未解锁
        if (!LevelManager.IsUnlock(levelConfig.Id))
        {
            UISystem.Show<UI_TipWindow>().ShowTip("提示","关卡还未解锁");
            return;
        }
        // 关卡已解锁
       UI_TipWindow window = UISystem.Show<UI_TipWindow>();
       window.ShowTip("提示","确定进入该关卡？");
       window.SetSureCallBack(() =>
       {
           UISystem.Close<UI_LevelSelectWindow>();
           LevelManager.EnterLevel(levelConfig.Id);
       });
    }
}