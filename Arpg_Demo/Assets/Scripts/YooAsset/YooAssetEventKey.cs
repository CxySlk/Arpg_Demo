using LLKFrame;

/// <summary>
/// YooAsset 初始化 / 更新流程事件定义。
/// <para>
/// 事件都挂在全局 <see cref="EventCenter.Global"/> 上，订阅方在销毁/关闭时记得
/// <c>UnSubscribe</c>，避免旧对象被事件引用。
/// </para>
/// <para>
/// 所有事件都带包名，多包场景下 UI 可以按包名过滤，只显示当前关心的包。
/// </para>
/// <para>
/// 单包典型事件顺序：
/// <c>StepChanged("初始化资源包") → StepChanged("请求资源版本") → StepChanged("加载资源清单")
/// → InitCompleted → StepChanged("创建下载器") → StepChanged("下载资源")
/// → DownloadProgress(...) → DownloadCompleted</c>；
/// 任一步失败会发 <see cref="Failed"/>。
/// </para>
/// <para>
/// 订阅示例：
/// <code>
/// EventCenter.Global.Subscribe(YooAssetEventKey.StepChanged, OnStep);
/// EventCenter.Global.Subscribe(YooAssetEventKey.DownloadProgress, OnProgress);
/// EventCenter.Global.Subscribe(YooAssetEventKey.Failed, OnFailed);
/// </code>
/// </para>
/// </summary>
public static class YooAssetEventKey
{
    /// <summary>
    /// 当前步骤变化。
    /// <para>参数：(包名, 步骤描述)，步骤描述可直接显示在加载界面上。</para>
    /// <para>触发时机：初始化文件系统、请求版本、加载清单、创建下载器、开始下载。</para>
    /// <para>订阅方：加载界面 / 启动流程，用来显示“正在做什么”。</para>
    /// </summary>
    public static readonly EventKey<string, string> StepChanged = "YOOASSET_STEP_CHANGED";

    /// <summary>
    /// 下载进度变化。
    /// <para>参数：<see cref="YooAssetDownloadProgress"/>（包名、文件数、字节数、0~1 百分比）。</para>
    /// <para>触发时机：下载器每次收到进度更新时；建议 UI 按 1% 或 0.1 秒节流刷新。</para>
    /// <para>订阅方：进度条 / 下载大小文字；用 PackageName 过滤当前包。</para>
    /// </summary>
    public static readonly EventKey<YooAssetDownloadProgress> DownloadProgress = "YOOASSET_DOWNLOAD_PROGRESS";

    /// <summary>
    /// 单个包初始化完成（文件系统 + 版本 + 清单已就绪）。
    /// <para>参数：包名。</para>
    /// <para>触发时机：清单加载成功之后、开始下载之前；此时已经可以加载该包的资源。</para>
    /// <para>订阅方：需要“包已可用”才能继续的流程（例如进入关卡前等 InitPackage 完成）。</para>
    /// </summary>
    public static readonly EventKey<string> InitCompleted = "YOOASSET_INIT_COMPLETED";

    /// <summary>
    /// 单个包下载完成（没有可下载文件时也会触发）。
    /// <para>参数：包名。</para>
    /// <para>触发时机：下载器成功结束，或检查到差异文件数为 0。</para>
    /// <para>订阅方：需要“资源已全部就位”才能继续的流程（例如进入战斗/关卡）。</para>
    /// </summary>
    public static readonly EventKey<string> DownloadCompleted = "YOOASSET_DOWNLOAD_COMPLETED";

    /// <summary>
    /// 初始化或下载失败。
    /// <para>参数：(包名, 错误信息)。</para>
    /// <para>触发时机：初始化文件系统、请求版本、加载清单、下载失败，或单个文件下载报错。</para>
    /// <para>订阅方：错误提示 / 重试按钮；注意单文件报错后 YooAsset 仍可能重试，最终以 DownloadCompleted/Failed 为准。</para>
    /// </summary>
    public static readonly EventKey<string, string> Failed = "YOOASSET_FAILED";

    /// <summary>
    /// InitAll 中所有包都处理完成（不保证全部成功，成功与否看各包的 InitCompleted/Failed）。
    /// <para>触发时机：按 <c>PackageNames</c> 顺序跑完所有包之后。</para>
    /// <para>注意：如果 <c>_autoUpdate = false</c>，这里只代表“初始化完成”，下载仍需手动触发。</para>
    /// </summary>
    public static readonly EventKey AllInitCompleted = "YOOASSET_ALL_INIT_COMPLETED";
}
