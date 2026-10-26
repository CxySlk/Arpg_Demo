using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_LoadingSceneWindow), nameof(UI_LoadingSceneWindow), false, 4)]
public class UI_LoadingSceneWindow : UI_WindowBase
{
    [SerializeField] private Image _imgProgress;
    [SerializeField] private TMP_Text _txtProgress;

    protected override void OnShow()
    {
        EventCenter.Global.UnSubscribe(SceneSystem.LoadProgressEventKey, UpdateProgress);
        EventCenter.Global.Subscribe(SceneSystem.LoadProgressEventKey, UpdateProgress);
        UpdateProgress(0);
    }

    protected override void OnClose()
    {
        EventCenter.Global.UnSubscribe(SceneSystem.LoadProgressEventKey, UpdateProgress);
    }

    private void UpdateProgress(int progress)
    {
        _imgProgress.fillAmount = progress / 100f;
        _txtProgress.text = progress + "%";
    }
}