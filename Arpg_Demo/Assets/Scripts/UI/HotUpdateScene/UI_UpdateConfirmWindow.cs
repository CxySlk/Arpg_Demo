using System;
using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_UpdateConfirmWindow), nameof(UI_UpdateConfirmWindow), false, 4)]
public class UI_UpdateConfirmWindow : UI_WindowBase
{
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _content;
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private Button _btnCancel;
    [SerializeField] private Button _btnClose;

    private Action _confirmAction;
    private Action _cancelAction;
    private bool _isConfirmClicked;
    private bool _isCancelClicked;

    protected override void OnShow()
    {
        _confirmAction = null;
        _cancelAction = null;
        _isConfirmClicked = false;
        _isCancelClicked = false;

        _btnConfirm.onClick.RemoveListener(OnConfirmClicked);
        _btnCancel.onClick.RemoveListener(OnCancelClicked);
        if (_btnClose != null)
            _btnClose.onClick.RemoveListener(OnCancelClicked);

        _btnConfirm.onClick.AddListener(OnConfirmClicked);
        _btnCancel.onClick.AddListener(OnCancelClicked);
        if (_btnClose != null)
            _btnClose.onClick.AddListener(OnCancelClicked);
    }

    protected override void OnClose()
    {
        _btnConfirm.onClick.RemoveListener(OnConfirmClicked);
        _btnCancel.onClick.RemoveListener(OnCancelClicked);
        if (_btnClose != null)
            _btnClose.onClick.RemoveListener(OnCancelClicked);

        if (_isConfirmClicked)
            _confirmAction?.Invoke();
        else if (_isCancelClicked)
            _cancelAction?.Invoke();

        _confirmAction = null;
        _cancelAction = null;
        _isConfirmClicked = false;
        _isCancelClicked = false;
    }

    public void ShowConfirm(string title, string content, Action onConfirm, Action onCancel)
    {
        _title.text = title;
        _content.text = content;
        _confirmAction = onConfirm;
        _cancelAction = onCancel;
    }

    private void OnConfirmClicked()
    {
        _isConfirmClicked = true;
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.Close<UI_UpdateConfirmWindow>();
    }

    private void OnCancelClicked()
    {
        _isCancelClicked = true;
        AudioSystem.PlayEffectMusic("ButtonClick");
        UISystem.Close<UI_UpdateConfirmWindow>();
    }
}
