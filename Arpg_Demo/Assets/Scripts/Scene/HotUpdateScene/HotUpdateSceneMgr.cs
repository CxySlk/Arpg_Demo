using LLKFrame;
using UnityEngine;

public class HotUpdateSceneMgr : SingletonMonoBase<HotUpdateSceneMgr>
{
    private const string LoginSceneName = "LoginScene";

    private bool _running;
    private bool _enteringLogin;
    private bool _downloadStarted;
    private bool _updatePrompted;
    private UI_HotUpdateWindow _progressWindow;

    private void Start()
    {
        Begin();
    }

    private void Begin()
    {
        if (_running)
            return;

        _running = true;
        _enteringLogin = false;
        _downloadStarted = false;
        _updatePrompted = false;

        YooAssetMgr.Instance.InitAll(OnInitCompleted, false);
    }

    private void OnInitCompleted(bool success)
    {
        if (!success)
        {
            _running = false;
            ShowInitError("资源初始化失败，请检查网络或 CDN 配置。");
            return;
        }

        YooAssetMgr.DownloadSummary summary = YooAssetMgr.Instance.GetDownloadSummary();
        if (!summary.HasUpdate)
        {
            EnterLoginScene();
            return;
        }

        if (_updatePrompted)
        {
            StartDownload();
            return;
        }

        _updatePrompted = true;
        ShowConfirm(summary);
    }

    private void ShowConfirm(YooAssetMgr.DownloadSummary summary)
    {
        string content =
            $"检测到新的资源包，共 {summary.Count} 个文件，约 {FormatBytes(summary.Bytes)}。\n" +
            "更新后可以获得最新内容，是否立即下载？";

        UI_UpdateConfirmWindow window = UISystem.Show<UI_UpdateConfirmWindow>();
        window.ShowConfirm("发现新版本", content, StartDownload, EnterLoginScene);
    }

    private void StartDownload()
    {
        if (_downloadStarted)
            return;

        _downloadStarted = true;
        _running = true;

        _progressWindow = UISystem.Show<UI_HotUpdateWindow>();
        _progressWindow.SetRetryAction(RetryDownload);
        _progressWindow.ClearError();
        _progressWindow.SetStep("准备下载资源");

        YooAssetMgr.Instance.DownloadAllPackage(OnDownloadCompleted);
    }

    private void OnDownloadCompleted(bool success)
    {
        if (!success)
        {
            _downloadStarted = false;
            _running = false;
            ShowDownloadError("资源下载失败，请检查网络后重试。");
            return;
        }

        if (_progressWindow != null)
            _progressWindow.SetProgress(1f, 0, 0);

        EnterLoginScene();
    }

    private void RetryDownload()
    {
        if (_running && _downloadStarted)
            return;

        _downloadStarted = false;
        StartDownload();
    }

    private void RetryInit()
    {
        if (_running)
            return;

        UISystem.Close<UI_HotUpdateWindow>();
        Begin();
    }

    private void ShowInitError(string message)
    {
        _progressWindow = UISystem.Show<UI_HotUpdateWindow>();
        _progressWindow.SetRetryAction(RetryInit);
        _progressWindow.ShowError(message);
    }

    private void ShowDownloadError(string message)
    {
        if (_progressWindow == null)
            _progressWindow = UISystem.Show<UI_HotUpdateWindow>();

        _progressWindow.SetRetryAction(RetryDownload);
        _progressWindow.ShowError(message);
    }

    private void EnterLoginScene()
    {
        if (_enteringLogin)
            return;

        _enteringLogin = true;
        _running = false;

        UISystem.Close<UI_HotUpdateWindow>();
        UISystem.Close<UI_UpdateConfirmWindow>();
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync(LoginSceneName, OnLoginSceneLoaded);
    }

    private void OnLoginSceneLoaded()
    {
        UISystem.Close<UI_LoadingSceneWindow>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            instance = null;
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
