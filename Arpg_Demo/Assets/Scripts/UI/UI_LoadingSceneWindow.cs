using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_LoadingSceneWindow), nameof(UI_LoadingSceneWindow), false, 4)]
public class UI_LoadingSceneWindow : UI_WindowBase
{
    [SerializeField] private Image imgProgress;
    [SerializeField] private TMP_Text txtProgress;

    public override void Show(int layer)
    {
        base.Show(layer);
        EventCenter.AddEventListener<int>(EventCenterType.LOAD_SCENE_PROGRESS, UpdateProgress);
        UpdateProgress(0);
    }

    protected override void OnClose()
    {
        EventCenter.RemoveEventListener<int>(EventCenterType.LOAD_SCENE_PROGRESS, UpdateProgress);
    }

    private void UpdateProgress(int progress)
    {
        if (imgProgress == null || txtProgress == null) return;
        imgProgress.fillAmount = progress / 100f;
        txtProgress.text = progress + "%";
    }
}
