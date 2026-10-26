using Level;
using LLKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI.GameScene
{
    /// <summary>
    /// 游戏内设置（暂停菜单）：继续游戏 / 返回 / 设置 / 重新开始 / 右上角关闭。
    /// 打开即暂停（引用计数，支持与背包等叠加），关闭恢复。
    /// </summary>
    [UIWindow(nameof(UI_GameSettingWindow), nameof(UI_GameSettingWindow), true, 1)]
    public class UI_GameSettingWindow : UI_WindowBase
    {
        [SerializeField] private Button _btnContinue;
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnSetting;
        [SerializeField] private Button _btnRestart;
        [SerializeField] private Button _btnClose;

        protected override void OnShow()
        {
            // 暂停时间（引用计数：与其它暂停源叠加时不会被误恢复）
            TimeSystem.Global.Pause();

            // 显示光标，便于点击设置界面
            Cursor.lockState = CursorLockMode.None;

            _btnContinue.onClick.AddListener(OnResumeClicked);
            _btnClose.onClick.AddListener(OnResumeClicked);
            _btnBack.onClick.AddListener(OnBackClicked);
            _btnSetting.onClick.AddListener(OnSettingClicked);
            _btnRestart.onClick.AddListener(OnRestartClicked);
        }

        protected override void OnClose()
        {
            _btnContinue.onClick.RemoveListener(OnResumeClicked);
            _btnClose.onClick.RemoveListener(OnResumeClicked);
            _btnBack.onClick.RemoveListener(OnBackClicked);
            _btnSetting.onClick.RemoveListener(OnSettingClicked);
            _btnRestart.onClick.RemoveListener(OnRestartClicked);

            // 恢复时间（引用计数自减，归零才真正解冻）
            TimeSystem.Global.UnPause();
        }

        /// <summary>继续游戏 / 关闭：关闭本窗口即恢复。</summary>
        private void OnResumeClicked()
        {
            Click();
            UISystem.Close<UI_GameSettingWindow>();
            // 还原为游戏中锁定光标
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>返回：异步加载开始游戏场景（带进度加载面板）。</summary>
        private void OnBackClicked()
        {
            Click();
            UISystem.Show<UI_LoadingSceneWindow>();
            SceneSystem.LoadSceneAsync("SelectLevelScene", UISystem.Close<UI_LoadingSceneWindow>);
            UISystem.Close<UI_GameSettingWindow>();
            UISystem.Close<UI_GameWindow>();
        }

        /// <summary>设置：打开音量设置面板（本窗口保持暂停）。</summary>
        private void OnSettingClicked()
        {
            Click();
            UISystem.Show<UI_SettingWindow>();
        }

        /// <summary>重新开始：异步重新加载游戏场景（带进度加载面板）。</summary>
        private void OnRestartClicked()
        {
            Click();
            UISystem.Close<UI_GameSettingWindow>();
            LevelManager.EnterLevel(LevelManager.LevelID);
        }

        private static void Click() => AudioSystem.PlayEffectMusic("ButtonClick");
    }
}