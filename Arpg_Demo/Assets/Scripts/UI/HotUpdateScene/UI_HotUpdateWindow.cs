using System;
using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_HotUpdateWindow), nameof(UI_HotUpdateWindow), false, 1)]
public class UI_HotUpdateWindow : UI_WindowBase
{
    [SerializeField] private Image _progressFill;
    [SerializeField] private TextMeshProUGUI _percentText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _packageText;
    [SerializeField] private TextMeshProUGUI _sizeText;
    [SerializeField] private GameObject _errorRoot;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private Button _btnRetry;

    private Action _retryAction;
    private float _targetFill;
    private int _lastPercent = -1;

    protected override void OnShow()
    {
        Unsubscribe();
        Subscribe();

        _targetFill = 0f;
        _lastPercent = -1;
        if (_progressFill != null)
            _progressFill.fillAmount = 0f;
        if (_percentText != null)
            _percentText.text = "0%";
        if (_statusText != null)
            _statusText.text = "准备更新资源";
        if (_packageText != null)
            _packageText.text = string.Empty;
        if (_sizeText != null)
            _sizeText.text = string.Empty;
        ClearError();
        RefreshRetryButton();
    }

    protected override void OnClose()
    {
        Unsubscribe();
        if (_btnRetry != null)
            _btnRetry.onClick.RemoveListener(OnRetryClicked);
    }

    private void Update()
    {
        if (_progressFill == null)
            return;

        _progressFill.fillAmount = Mathf.MoveTowards(
            _progressFill.fillAmount,
            _targetFill,
            Time.unscaledDeltaTime * 1.8f);
    }

    public void SetRetryAction(Action action)
    {
        _retryAction = action;
        RefreshRetryButton();
    }

    public void SetStep(string step)
    {
        if (_statusText != null)
            _statusText.text = step;
    }

    public void SetPackage(string packageName)
    {
        if (_packageText != null)
            _packageText.text = string.IsNullOrEmpty(packageName) ? string.Empty : "资源包：" + packageName;
    }

    public void SetProgress(YooAssetDownloadProgress progress)
    {
        SetProgress(progress.Percent, progress.CurrentBytes, progress.TotalBytes);
        SetPackage(progress.PackageName);
    }

    public void SetProgress(float percent, long currentBytes, long totalBytes)
    {
        percent = Mathf.Clamp01(percent);
        _targetFill = percent;

        int rounded = Mathf.RoundToInt(percent * 100f);
        if (rounded != _lastPercent)
        {
            _lastPercent = rounded;
            if (_percentText != null)
                _percentText.text = rounded + "%";
        }

        if (_sizeText != null)
            _sizeText.text = FormatBytes(currentBytes) + " / " + FormatBytes(totalBytes);
    }

    public void ShowError(string message)
    {
        if (_errorRoot != null)
            _errorRoot.SetActive(true);
        if (_errorText != null)
            _errorText.text = message;
        if (_statusText != null)
            _statusText.text = "更新失败";
        RefreshRetryButton();
    }

    public void ClearError()
    {
        if (_errorRoot != null)
            _errorRoot.SetActive(false);
        if (_errorText != null)
            _errorText.text = string.Empty;
    }

    private void RefreshRetryButton()
    {
        if (_btnRetry == null)
            return;

        _btnRetry.onClick.RemoveListener(OnRetryClicked);
        _btnRetry.onClick.AddListener(OnRetryClicked);
        _btnRetry.gameObject.SetActive(_retryAction != null && _errorRoot != null && _errorRoot.activeSelf);
    }

    private void OnRetryClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        ClearError();
        SetStep("准备重新下载");
        _retryAction?.Invoke();
    }

    private void Subscribe()
    {
        EventCenter.Global.Subscribe(YooAssetEventKey.StepChanged, OnStepChanged);
        EventCenter.Global.Subscribe(YooAssetEventKey.DownloadProgress, OnDownloadProgress);
        EventCenter.Global.Subscribe(YooAssetEventKey.Failed, OnDownloadFailed);
    }

    private void Unsubscribe()
    {
        EventCenter.Global.UnSubscribe(YooAssetEventKey.StepChanged, OnStepChanged);
        EventCenter.Global.UnSubscribe(YooAssetEventKey.DownloadProgress, OnDownloadProgress);
        EventCenter.Global.UnSubscribe(YooAssetEventKey.Failed, OnDownloadFailed);
    }

    private void OnStepChanged(string packageName, string step)
    {
        SetPackage(packageName);
        SetStep(step);
    }

    private void OnDownloadProgress(YooAssetDownloadProgress progress)
    {
        ClearError();
        SetProgress(progress);
    }

    private void OnDownloadFailed(string packageName, string error)
    {
        SetPackage(packageName);
        ShowError(error);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024f).ToString("0.0") + " KB";
        if (bytes < 1024L * 1024 * 1024)
            return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
        return (bytes / (1024f * 1024f * 1024f)).ToString("0.00") + " GB";
    }
}
