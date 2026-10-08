using LLKFrame;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_BeginWindow), nameof(UI_BeginWindow), false, 4)]
public class UI_BeginWindow : UI_WindowBase
{
    [SerializeField]private Button btnContinue;
    [SerializeField]private Button btnStart;
    [SerializeField]private Button btnSetting;
    [SerializeField] private Button btnQuit;

    public override void Show(int layer)
    {
        base.Show(layer);
        btnContinue.onClick.AddListener(OnContinueButtonClicked);
        btnStart.onClick.AddListener(OnStartButtonClicked);
        btnSetting.onClick.AddListener(OnSettingButtonClicked);
        btnQuit.onClick.AddListener(OnQuitButtonClicked);

        if (!DataManager.HasArchive)
            btnContinue.gameObject.SetActive(false);
    }
    #region 按钮事件监听
    private void OnContinueButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
    }
    private void OnStartButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        // 进入角色选择场景
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("ChooseCharacterScene", () =>
        {
            UISystem.Close<UI_LoadingSceneWindow>();
        });
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
    private void OnQuitButtonClicked()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");
        Application.Quit();
    }
    #endregion
}
