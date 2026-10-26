using System;
using DG.Tweening;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>单个 Tab 按钮：负责点击、选中线表现，不负责页面内容。</summary>
public class UI_TabButton : MonoBehaviour
{
    [LabelText("点击按钮"), SerializeField, Required] private Button _btnTab;
    [LabelText("选择线"), SerializeField, Required] private Image _imgSelectLine;

    private UI_TabGroup _tabGroup;
    private Tween _tween;
    private Action _callBack;

    public bool Init(UI_TabGroup tabGroup, Action callBack)
    {
        if (tabGroup == null || _btnTab == null || _imgSelectLine == null)
        {
            Debug.LogError($"{name} 的 Tab 引用未配置", this);
            return false;
        }

        _tabGroup = tabGroup;
        _callBack = callBack;
        _btnTab.onClick.RemoveListener(OnClick);
        _btnTab.onClick.AddListener(OnClick);
        return true;
    }

    public void Select()
    {
        _tween?.Kill();
        if (_imgSelectLine != null)
            _tween = _imgSelectLine.DOFade(1f, 0.25f);
    }

    public void UnSelect()
    {
        _tween?.Kill();
        if (_imgSelectLine != null)
            _tween = _imgSelectLine.DOFade(0f, 0.25f);
    }

    /// <summary>由 TabGroup 在真正选中后触发，保证默认选中也能刷新内容。</summary>
    public void NotifySelected() => _callBack?.Invoke();

    private void OnClick()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        if (_tabGroup == null || _tabGroup.CurTabButton == this) return;
        _tabGroup.SelectTabPage(this);
    }
}
