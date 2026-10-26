using LLKFrame;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_BeginWindow), nameof(UI_BeginWindow), false, 4)]
public class UI_BeginWindow : UI_WindowBase
{
    [SerializeField] private Button _btnContinue;
    [SerializeField] private Button _btnStart;
    [SerializeField] private Button _btnSetting;
    [SerializeField] private Button _btnReturnLogin;
    [SerializeField] private Button _btnQuit;

    protected override void OnShow()
    {
        _btnContinue.onClick.AddListener(OnContinueButtonClicked);
        _btnStart.onClick.AddListener(OnStartButtonClicked);
        _btnSetting.onClick.AddListener(OnSettingButtonClicked);
        _btnReturnLogin.onClick.AddListener(OnReturnButtonClicked);
        _btnQuit.onClick.AddListener(OnQuitButtonClicked);
        
        if (!DataManager.HasArchive())
            _btnContinue.gameObject.SetActive(false);
    }


    #region 按钮事件监听

    private void OnContinueButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        // 进入游戏场景
        UISystem.Close<UI_BeginWindow>();
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("SelectLevelScene", UISystem.Close<UI_LoadingSceneWindow>);
    }

    private void OnStartButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        // 进入角色选择场景
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("ChooseCharacterScene", UISystem.Close<UI_LoadingSceneWindow>);
        // 关闭自身
        UISystem.Close<UI_BeginWindow>();
    }

    private void OnSettingButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        // 打开设置面板
        UISystem.Show<UI_SettingWindow>();
    }

    private void OnReturnButtonClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("LoginScene", UISystem.Close<UI_LoadingSceneWindow>);
        UISystem.Close<UI_BeginWindow>();
    }

    private void OnQuitButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        Application.Quit();
    }

    #endregion
}