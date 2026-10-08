using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[UIWindow(nameof(UI_LoadingSceneWindow), nameof(UI_LoadingSceneWindow), false, 4)]
public class UI_LoadingSceneWindow : UI_WindowBase
{
    [FormerlySerializedAs("imgProgress")] [SerializeField] private Image _imgProgress;
    [FormerlySerializedAs("txtProgress")] [SerializeField] private TMP_Text _txtProgress;

    public override void Show(int layer)
    {
        base.Show(layer);
        
        EventCenter.Global.Subscribe(SceneSystem.LoadProgressEventKey,UpdateProgress);
        UpdateProgress(0);
    }

    protected override void OnClose()
    {
        EventCenter.Global.UnSubscribe(SceneSystem.LoadProgressEventKey,UpdateProgress);
    }

    private void UpdateProgress(int progress)
    {
        if (_imgProgress == null || _txtProgress == null) return;
        _imgProgress.fillAmount = progress / 100f;
        _txtProgress.text = progress + "%";
    }
}
