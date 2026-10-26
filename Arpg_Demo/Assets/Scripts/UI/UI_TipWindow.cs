using System;
using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用提示窗口：半透明遮罩 + 居中面板（标题、内容、确定按钮、右上角关闭 ×）。
/// 点击"确定"或"×"关闭自身；窗口缓存，UI 在预制体中搭建。
/// 用法：UISystem.Show&lt;UI_TipWindow&gt;().ShowTip("提示", "内容");
/// 显示/关闭的淡入淡出由挂在窗口根物体上的表现效果组件（UI_WindowFadeEffect / UI_WindowScaleEffect）自动播放。
/// </summary>
[UIWindow(nameof(UI_TipWindow), nameof(UI_TipWindow), true, 4)]
public class UI_TipWindow : UI_WindowBase
{
    [LabelText("标题"), SerializeField] private TextMeshProUGUI _title;
    [LabelText("内容"), SerializeField] private TextMeshProUGUI _content;
    [LabelText("确定按钮"), SerializeField] private Button _btnSure;
    [LabelText("关闭按钮"), SerializeField] private Button _btnCancel;

    private Action _sureCallBack;
    private Action _cancelCallBack;
    private Action _anyCallBack;
    private bool _isSureBtnClicked;
    private bool _isCancelBtnClicked;

    protected override void OnShow()
    {
        _sureCallBack = null;
        _cancelCallBack = null;
        _anyCallBack = null;

        // 先移除再添加：缓存窗口重复 Show 时避免监听器累积。
        _btnSure.onClick.RemoveListener(OnSureClicked);
        _btnCancel.onClick.RemoveListener(OnCancelClicked);
        _btnSure.onClick.AddListener(OnSureClicked);
        _btnCancel.onClick.AddListener(OnCancelClicked);

        _isSureBtnClicked = _isCancelBtnClicked = false;
    }

    protected override void OnClose()
    {
        _btnSure.onClick.RemoveListener(OnSureClicked);
        _btnCancel.onClick.RemoveListener(OnCancelClicked);

        if (_isSureBtnClicked) _sureCallBack?.Invoke();
        if (_isCancelBtnClicked) _cancelCallBack?.Invoke();
        _anyCallBack?.Invoke();

        _sureCallBack = null;
        _cancelCallBack = null;
        _anyCallBack = null;
        _isSureBtnClicked = _isCancelBtnClicked = false;
    }

    /// <summary>设置标题与提示内容（在 UISystem.Show&lt;UI_TipWindow&gt;() 之后调用）。</summary>
    public void ShowTip(string title, string content)
    {
        _title.text = title;
        _content.text = content;
    }

    #region 设置回调

    /// <summary>设置"任意按钮关闭都会触发"的回调；只触发一次。</summary>
    public void SetCallBack(Action callback) => _anyCallBack = callback;

    public void SetSureCallBack(Action callback) => _sureCallBack += callback;

    public void SetCancelCallBack(Action callback) => _cancelCallBack += callback;

    #endregion

    #region 按钮点击

    private void OnSureClicked()
    {
        _isSureBtnClicked = true;
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.Close<UI_TipWindow>();
    }

    private void OnCancelClicked()
    {
        _isCancelBtnClicked = true;
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.Close<UI_TipWindow>();
    }

    #endregion
}