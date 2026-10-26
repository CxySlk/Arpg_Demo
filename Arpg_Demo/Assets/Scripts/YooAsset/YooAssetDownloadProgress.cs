/// <summary>
/// YooAsset 资源包下载进度（通过 <see cref="YooAssetEventKey.DownloadProgress"/> 发给 UI）。
/// <para>
/// 百分比以“字节数”为准：文件数量少但单文件很大时，按文件数算会跳变，按字节数更平滑。
/// </para>
/// <para>
/// 这是一个只读结构体，事件里按值传递，不会产生 GC；
/// 但下载进度可能每帧变化，UI 侧建议按 1% 或 0.1 秒节流刷新。
/// </para>
/// </summary>
public readonly struct YooAssetDownloadProgress
{
    /// <summary>资源包名（多包时用于区分是哪个包在下载）。</summary>
    public readonly string PackageName;

    /// <summary>本次需要下载的文件总数（用于显示“3/10 个文件”）。</summary>
    public readonly int TotalCount;

    /// <summary>当前已下载完成的文件数（用于显示“3/10 个文件”）。</summary>
    public readonly int CurrentCount;

    /// <summary>本次需要下载的总字节数（用于显示“8.5 MB”）。</summary>
    public readonly long TotalBytes;

    /// <summary>当前已下载的字节数（用于显示“1.2 / 8.5 MB”）。</summary>
    public readonly long CurrentBytes;

    /// <summary>
    /// 下载进度（0~1），由 <see cref="CurrentBytes"/> / <see cref="TotalBytes"/> 计算。
    /// <para>总字节为 0（没有可下载文件）时固定为 0。</para>
    /// </summary>
    public readonly float Percent;

    /// <summary>由管理器在下载进度回调里构造，业务侧只读。</summary>
    public YooAssetDownloadProgress(string packageName, int totalCount, int currentCount,
        long totalBytes, long currentBytes, float percent)
    {
        PackageName = packageName;
        TotalCount = totalCount;
        CurrentCount = currentCount;
        TotalBytes = totalBytes;
        CurrentBytes = currentBytes;
        Percent = percent;
    }
}
