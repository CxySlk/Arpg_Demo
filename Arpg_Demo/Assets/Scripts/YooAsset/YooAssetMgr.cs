using System;
using System.Collections;
using System.Collections.Generic;
using LLKFrame;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

/// <summary>
/// YooAsset 资源包管理器。
/// <para>
/// 职责：
/// 1. 按包名初始化 <see cref="ResourcePackage"/>（文件系统、版本、清单）；
/// 2. 从远端检查并更新资源包；
/// 3. 把初始化步骤、下载进度、成功/失败通过 <see cref="EventCenter"/> 发出；
/// 4. 提供资源 / 场景加载入口。
/// </para>
/// <para>
/// 使用步骤：
/// 1. 在 YooAsset 收集器里建好包，包名与 <see cref="PackageNames"/> 保持一致；
/// 2. 把本组件挂到常驻对象（GameRoot / Bootstrap），设置 _hostInEditor、_autoUpdate；
/// 3. 先订阅 <see cref="YooAssetEventKey"/> 的事件，再调用 <see cref="InitAll"/> 或 <see cref="InitPackage"/>；
/// 4. 初始化完成后用 <see cref="LoadAssetAsync{T}"/> / <see cref="LoadSceneAsync"/> 加载资源；
/// 5. AssetHandle 用完调用 Release；Prefab 实例还在使用时不要提前释放。
/// </para>
/// <para>
/// 单包事件时序：
/// StepChanged("初始化资源包") → StepChanged("请求资源版本") → StepChanged("加载资源清单")
/// → InitCompleted → StepChanged("创建下载器") → StepChanged("下载资源")
/// → DownloadProgress × N → DownloadCompleted；任一步失败发 Failed。
/// </para>
/// <para>
/// 运行模式：
/// - 编辑器默认 <c>EditorSimulateMode</c>：不连 CDN，直接读模拟构建目录，迭代快；
/// - 勾选 <c>_hostInEditor</c> 或打包后走 <c>HostPlayMode</c>：内置文件系统 + 沙盒缓存，从远端下载。
/// </para>
/// <para>
/// 多包：
/// - <see cref="InitAll"/> 按 <see cref="PackageNames"/> 顺序初始化；
/// - <see cref="InitPackage"/> 懒加载单个包（第一次用到该包内容前调用）；
/// - 所有事件都带包名，UI 可以按包名过滤。
/// </para>
/// </summary>
public class YooAssetMgr : MonoBehaviour
{
    #region 单例

    /// <summary>全局单例；挂到常驻对象上，跨场景保留。</summary>
    public static YooAssetMgr Instance { get; private set; }

    #endregion

    #region 配置

    /// <summary>远端 CDN 根地址。</summary>
    /// <remarks>最终远端根 = DefaultUrl + DefaultVersion + "/" + 包名，见 <see cref="GetRemoteRoot"/>。</remarks>
    public const string DefaultUrl = "http://127.0.0.1:8080/CDN/PC/";

    /// <summary>远端版本目录（不是 YooAsset 的包版本；包版本由 RequestPackageVersionAsync 获取）。</summary>
    public const string DefaultVersion = "v1.0";

    /// <summary>YooAsset 收集器里的包名；数组顺序就是 <see cref="InitAll"/> 的初始化顺序。</summary>
    /// <remarks>
    /// 这是唯一需要按项目改包名的地方。新增包时在这里追加；
    /// 不在数组里的包也可以直接调 <see cref="InitPackage"/> 懒加载。
    /// </remarks>
    public const string UIPackage = "UI";

    public const string AudioPackage = "Audio";
    private static readonly string[] PackageNames = { UIPackage, AudioPackage };

    [Tooltip("初始化时是否自动检查并下载更新；关闭后初始化只到清单，之后手动调 DownloadPackage")] [SerializeField]
    private bool _autoUpdate = true;

    /// <summary>包名 → 已创建的 ResourcePackage，避免重复 CreatePackage。</summary>
    private readonly Dictionary<string, ResourcePackage> _packages = new();

    /// <summary>已完成初始化（版本 + 清单）的包；可开始加载资源。</summary>
    private readonly HashSet<string> _ready = new();

    /// <summary>正在初始化的包；用于防止重复 InitPackage。</summary>
    private readonly HashSet<string> _initializing = new();

    /// <summary>正在下载的包；用于防止重复 DownloadPackage。</summary>
    private readonly HashSet<string> _downloading = new();

    /// <summary>是否正在执行 InitAll，防止重复启动整轮初始化。</summary>
    private bool _isInitializingAll;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 单例 + 常驻：资源包和 YooAssets 全局实例不随场景卸载。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region 对外接口：初始化 / 下载

    /// <summary>按 <see cref="PackageNames"/> 顺序初始化所有包。</summary>
    /// <param name="onComplete">全部包处理完后回调；true 表示全部成功。</param>
    /// <remarks>
    /// 顺序执行，避免多个包同时下载抢带宽；单个包失败不会中断后续包，
    /// 最终通过 onComplete 的 bool 和 <see cref="YooAssetEventKey.AllInitCompleted"/> 汇总结果。
    /// </remarks>
    public void InitAll(Action<bool> onComplete = null, bool autoDownload = true)
    {
        if (_isInitializingAll)
        {
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(InitAllAsync(onComplete, autoDownload));
    }

    /// <summary>汇总所有已初始化包的下载信息；不会开始下载。</summary>
    public DownloadSummary GetDownloadSummary()
    {
        int totalCount = 0;
        long totalBytes = 0;

        for (int i = 0; i < PackageNames.Length; i++)
        {
            string packageName = PackageNames[i];
            if (!_ready.Contains(packageName))
                continue;

            ResourcePackage package = GetPackage(packageName);
            if (package == null)
                continue;

            ResourceDownloaderOperation downloader =
                package.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));
            totalCount += downloader.TotalDownloadCount;
            totalBytes += downloader.TotalDownloadBytes;
        }

        return new DownloadSummary(totalCount, totalBytes);
    }

    /// <summary>按 PackageNames 顺序下载所有已初始化包。</summary>
    public void DownloadAllPackage(Action<bool> onComplete = null)
    {
        StartCoroutine(DownloadAllPackageAsync(onComplete));
    }

    /// <summary>初始化单个包（懒加载入口）。</summary>
    /// <param name="packageName">YooAsset 收集器里的包名。</param>
    /// <param name="onComplete">完成回调；已就绪直接回调 true，正在初始化回调 false。</param>
    /// <remarks>
    /// 适合 DLC / 关卡包：第一次进入对应内容前调用，避免启动时下载全部内容。
    /// 同一个包重复调用不会重复初始化。
    /// </remarks>
    public void InitPackage(string packageName, Action<bool> onComplete = null)
    {
        StartCoroutine(InitPackageAsync(packageName, onComplete));
    }

    /// <summary>手动下载更新（<c>_autoUpdate = false</c> 时使用）。</summary>
    /// <param name="packageName">已初始化完成的包名。</param>
    /// <param name="onComplete">下载完成回调。</param>
    /// <remarks>
    /// 没有可下载文件时会直接回调 true，并发布 <see cref="YooAssetEventKey.DownloadCompleted"/>；
    /// 下载进度仍通过 <see cref="YooAssetEventKey.DownloadProgress"/> 发出。
    /// </remarks>
    public void DownloadPackage(string packageName, Action<bool> onComplete = null)
    {
        if (!_ready.Contains(packageName))
        {
            EventCenter.Global.Publish(YooAssetEventKey.Failed, packageName, "资源包未初始化");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(DownloadPackageAsync(packageName, onComplete));
    }

    #endregion

    #region 对外接口：查询 / 加载

    /// <summary>包是否已完成初始化（可以加载资源）。</summary>
    public bool IsPackageReady(string packageName)
    {
        return _ready.Contains(packageName) && GetPackage(packageName) != null;
    }

    /// <summary>获取已创建的 <see cref="ResourcePackage"/>；未创建时返回 null。</summary>
    public ResourcePackage GetPackage(string packageName)
    {
        _packages.TryGetValue(packageName, out ResourcePackage package);
        return package;
    }

    /// <summary>异步加载资源，返回 YooAsset 句柄。</summary>
    /// <param name="packageName">已初始化的包名。</param>
    /// <param name="location">YooAsset 收集器里配置的资源定位地址（不是绝对路径）。</param>
    /// <returns><see cref="AssetHandle"/>；调用方使用完后必须调用 Release。</returns>
    /// <remarks>
    /// 可以先 yield return handle，再取 handle.AssetObject / GetAssetObject&lt;T&gt;；
    /// 如果是 Prefab 且实例会长期存在，不要在实例销毁前 Release。
    /// </remarks>
    public AssetHandle LoadAssetAsync<T>(string packageName, string location) where T : UnityEngine.Object
    {
        ResourcePackage package = GetPackage(packageName);
        if (package == null)
        {
            Debug.LogError($"YooAsset 包未初始化: {packageName}");
            return null;
        }

        return package.LoadAssetAsync<T>(location);
    }

    /// <summary>同步加载资源，返回 YooAsset 句柄。</summary>
    /// <param name="packageName">已初始化的包名。</param>
    /// <param name="location">YooAsset 收集器里配置的资源定位地址（不是绝对路径）。</param>
    /// <returns><see cref="AssetHandle"/>；调用方使用完后必须调用 Release。</returns>
    public AssetHandle LoadAssetSync<T>(string packageName, string location) where T : UnityEngine.Object
    {
        ResourcePackage package = GetPackage(packageName);
        if (package == null)
        {
            Debug.LogError($"YooAsset 包未初始化: {packageName}");
            return null;
        }

        return package.LoadAssetSync<T>(location);
    }

    /// <summary>异步加载场景。</summary>
    /// <param name="packageName">已初始化的包名。</param>
    /// <param name="location">YooAsset 收集器里配置的场景定位地址。</param>
    /// <param name="mode">Single / Additive。</param>
    /// <param name="allowSceneActivation">false 时加载完成后需手动调用 <c>ActivateScene()</c>。</param>
    /// <returns><see cref="YooAsset.SceneHandle"/>。</returns>
    public YooAsset.SceneHandle LoadSceneAsync(string packageName, string location,
        LoadSceneMode mode = LoadSceneMode.Single, bool allowSceneActivation = true)
    {
        ResourcePackage package = GetPackage(packageName);
        if (package == null)
        {
            Debug.LogError($"YooAsset 包未初始化: {packageName}");
            return null;
        }

        return package.LoadSceneAsync(location, mode, LocalPhysicsMode.None, allowSceneActivation);
    }

    #endregion

    #region 初始化 / 更新协程

    /// <summary>顺序初始化 <see cref="PackageNames"/> 中的所有包。</summary>
    /// <remarks>
    /// 单个包失败只记录 allOk = false，继续后面的包，避免一个 DLC 包失败拖垮整个启动流程。
    /// </remarks>
    private IEnumerator InitAllAsync(Action<bool> onComplete, bool autoDownload)
    {
        _isInitializingAll = true;
        bool allOk = true;

        for (int i = 0; i < PackageNames.Length; i++)
        {
            bool ok = false;
            yield return InitPackageAsync(PackageNames[i], result => ok = result, autoDownload);
            if (!ok) allOk = false;
        }

        _isInitializingAll = false;
        EventCenter.Global.Publish(YooAssetEventKey.AllInitCompleted);
        onComplete?.Invoke(allOk);
    }

    private IEnumerator DownloadAllPackageAsync(Action<bool> onComplete)
    {
        bool allOk = true;

        for (int i = 0; i < PackageNames.Length; i++)
        {
            string packageName = PackageNames[i];
            if (!_ready.Contains(packageName))
            {
                allOk = false;
                continue;
            }

            bool ok = false;
            yield return DownloadPackageAsync(packageName, result => ok = result);
            if (!ok)
                allOk = false;
        }

        onComplete?.Invoke(allOk);
    }

    /// <summary>初始化单个包：文件系统 → 版本 → 清单 →（可选）下载。</summary>
    /// <remarks>
    /// 状态约定：
    /// - _initializing 防重复初始化；
    /// - 清单加载成功后加入 _ready，此时已经可以加载资源；
    /// - 如果 _autoUpdate 为 true，继续走 DownloadPackageAsync；
    /// - 任一步失败调用 Fail，统一清理状态并发 Failed 事件。
    /// </remarks>
    private IEnumerator InitPackageAsync(string packageName, Action<bool> onComplete, bool autoDownload = true)
    {
        // 已就绪：直接成功，避免重复走网络流程。
        if (_ready.Contains(packageName))
        {
            onComplete?.Invoke(true);
            yield break;
        }

        // 同一包正在初始化：不再重复创建/请求，直接返回失败。
        if (!_initializing.Add(packageName))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // YooAssets.Initialize() 是全局的，只在第一次初始化时执行。
        EnsureYooInitialized();
        PublishStep(packageName, "初始化资源包");

        // 创建或复用包实例；包名必须与 YooAsset 收集器一致。
        if (!YooAssets.TryGetPackage(packageName, out ResourcePackage package))
            package = YooAssets.CreatePackage(packageName);
        _packages[packageName] = package;

        // 1. 初始化文件系统：
        //    编辑器模拟模式直接读模拟构建目录；
        //    联机模式则准备内置文件系统 + 沙盒缓存文件系统。
        InitializePackageOperation initOp = package.InitializePackageAsync(BuildInitOptions(packageName));
        yield return initOp;
        if (initOp.Status != EOperationStatus.Succeeded)
        {
            Fail(packageName, initOp.Error, onComplete);
            Debug.LogError("初始化文件系统失败：" + initOp.Error);
            yield break;
        }

        // 2. 向远端请求最新的包版本（PackageVersion.version）。
        //    包版本和 DefaultVersion（app 版本目录）不是一回事。
        PublishStep(packageName, "请求资源版本");
        RequestPackageVersionOperation versionOp = package.RequestPackageVersionAsync();
        yield return versionOp;
        if (versionOp.Status != EOperationStatus.Succeeded)
        {
            EventCenter.Global.Publish(YooAssetEventKey.Failed, packageName, versionOp.Error);
            Debug.LogError("请求版本号失败：" + versionOp.Error);
            yield break;
        }

        // 3. 按版本加载资源清单（package.hash 等）。
        //    清单加载成功后，才能按 location 正确解析依赖并加载资源。
        PublishStep(packageName, "加载资源清单");
        LoadPackageManifestOperation manifestOp =
            package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOp.PackageVersion, 60));
        yield return manifestOp;
        if (manifestOp.Status != EOperationStatus.Succeeded)
        {
            Fail(packageName, manifestOp.Error, onComplete);
            Debug.LogError("加载资源清单失败：" + manifestOp.Error);
            yield break;
        }

        // 清单加载完成：此时已经可以加载资源，先标记 ready 并通知 UI。
        _ready.Add(packageName);
        _initializing.Remove(packageName);
        EventCenter.Global.Publish(YooAssetEventKey.InitCompleted, packageName);

        // 4. 需要自动更新时继续下载；关闭时由外部手动调 DownloadPackage。
        if (autoDownload && _autoUpdate)
        {
            bool downloadOk = false;
            yield return DownloadPackageAsync(packageName, result => downloadOk = result);
            if (!downloadOk)
            {
                onComplete?.Invoke(false);
                yield break;
            }
        }

        onComplete?.Invoke(true);
    }

    /// <summary>下载指定包的更新内容。</summary>
    /// <remarks>
    /// 流程：创建下载器 → 没有文件直接完成 → 绑定进度/错误事件 → StartDownload → 等结束。
    /// 进度通过 <see cref="YooAssetEventKey.DownloadProgress"/> 发出，下载器本身只负责网络与缓存。
    /// </remarks>
    private IEnumerator DownloadPackageAsync(string packageName, Action<bool> onComplete)
    {
        // 同一包正在下载：不重复启动，避免多个下载器抢同一批文件。
        if (!_downloading.Add(packageName))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        ResourcePackage package = GetPackage(packageName);
        if (package == null)
        {
            _downloading.Remove(packageName);
            EventCenter.Global.Publish(YooAssetEventKey.Failed, packageName, "资源包未初始化");
            onComplete?.Invoke(false);
            yield break;
        }

        PublishStep(packageName, "创建下载器");
        // 10 = 最大并发下载数，3 = 单个文件失败后的重试次数。
        ResourceDownloaderOperation downloader =
            package.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));

        // 没有差异文件：直接视为完成。
        if (downloader.TotalDownloadCount == 0)
        {
            _downloading.Remove(packageName);
            EventCenter.Global.Publish(YooAssetEventKey.DownloadCompleted, packageName);
            onComplete?.Invoke(true);
            yield break;
        }

        PublishStep(packageName, "下载资源");

        // 用局部委托保存，下载结束后能精确解绑，避免闭包/订阅泄漏。
        Action<DownloadProgressChangedEventArgs> progressHandler = args => OnProgress(packageName, args);
        Action<DownloadErrorEventArgs> errorHandler = args => OnDownloadError(packageName, args);
        downloader.DownloadProgressChanged += progressHandler;
        downloader.DownloadError += errorHandler;
        downloader.StartDownload();
        yield return downloader;
        downloader.DownloadProgressChanged -= progressHandler;
        downloader.DownloadError -= errorHandler;

        _downloading.Remove(packageName);
        if (downloader.Status != EOperationStatus.Succeeded)
        {
            Fail(packageName, "资源下载失败", onComplete);
            yield break;
        }

        EventCenter.Global.Publish(YooAssetEventKey.DownloadCompleted, packageName);
        onComplete?.Invoke(true);
    }

    #endregion

    #region 初始化参数 / 远端地址

    /// <summary>按运行环境构造 YooAsset 3.x 的初始化参数。</summary>
    /// <remarks>
    /// - 编辑器且未勾 _hostInEditor：EditorSimulateMode，直接读 <c>EditorSimulateBuildInvoker</c> 的模拟构建目录；
    /// - 其他情况：HostPlayMode，内置文件系统（StreamingAssets）+ 沙盒缓存文件系统（远端下载到本地缓存）。
    /// </remarks>
    private InitializePackageOptions BuildInitOptions(string packageName)
    {
        HostPlayModeOptions hostOptions = new();
        // 内置包：随包体发布，首次启动/断网时可用。
        hostOptions.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
        // 把内置包的清单复制到缓存，便于首次启动直接用内置资源。
        hostOptions.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);
        // 沙盒缓存：远端下载的文件落到可写目录，后续直接复用。
        hostOptions.CacheFileSystemParameters =
            FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                new RemoteService(GetRemoteRoot(packageName)));
        // 同时下载的 bundle 数；按带宽/服务器承受能力调整。
        hostOptions.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, 5);
        return hostOptions;
    }

    /// <summary>拼出某个包的远端根目录。</summary>
    /// <remarks>
    /// 结构：<c>{DefaultUrl}/{DefaultVersion}/{包名}</c>。
    /// 例如 <c>http://127.0.0.1:8000/CDN/PC/v1.0/Window</c>。
    /// 如果你的 CDN 不是按包名分子目录，只改这里即可。
    /// </remarks>
    private static string GetRemoteRoot(string packageName)
    {
        string baseUrl = DefaultUrl.TrimEnd('/');
        string version = DefaultVersion.Trim('/');
        return $"{baseUrl}/{version}/{packageName}";
    }

    #endregion

    #region 事件与工具

    /// <summary>YooAssets.Initialize() 是全局操作，整个进程只执行一次。</summary>
    private static void EnsureYooInitialized()
    {
        if (YooAssets.IsInitialized) return;
        YooAssets.Initialize();
    }

    /// <summary>发布“当前步骤”事件，参数为（包名, 步骤描述）。</summary>
    private static void PublishStep(string packageName, string step)
    {
        EventCenter.Global.Publish(YooAssetEventKey.StepChanged, packageName, step);
    }

    /// <summary>初始化/下载失败的统一出口：清理状态、发失败事件、回调 false。</summary>
    private void Fail(string packageName, string error, Action<bool> onComplete)
    {
        _initializing.Remove(packageName);
        _downloading.Remove(packageName);
        EventCenter.Global.Publish(YooAssetEventKey.Failed, packageName, error);
        onComplete?.Invoke(false);
    }

    /// <summary>下载进度回调：把 YooAsset 的字节数换算成 0~1 后发给事件中心。</summary>
    private static void OnProgress(string packageName, DownloadProgressChangedEventArgs args)
    {
        // 以字节数为准；文件数少但文件大时，百分比仍然准确。
        float percent = args.TotalDownloadBytes > 0
            ? (float)args.CurrentDownloadBytes / args.TotalDownloadBytes
            : 0f;
        YooAssetDownloadProgress progress = new(
            packageName,
            args.TotalDownloadCount,
            args.CurrentDownloadCount,
            args.TotalDownloadBytes,
            args.CurrentDownloadBytes,
            percent);
        EventCenter.Global.Publish(YooAssetEventKey.DownloadProgress, progress);
    }

    /// <summary>单个文件下载失败：发 Failed 事件；YooAsset 内部可能还会重试，最终以 downloader.Status 为准。</summary>
    private static void OnDownloadError(string packageName, DownloadErrorEventArgs args)
    {
        EventCenter.Global.Publish(YooAssetEventKey.Failed, packageName, args.FileName + ": " + args.ErrorInfo);
    }

    #endregion

    #region 远端服务

    /// <summary>YooAsset 3.x 的远端地址服务：返回该包所有候选下载地址。</summary>
    private sealed class RemoteService : IRemoteService
    {
        private readonly string _root;

        public RemoteService(string root) => _root = root;

        /// <summary>返回远端候选 URL；需要主备地址时在这里返回多个。</summary>
        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            List<string> urls = new() { $"{_root}/{fileName}" };
            return urls;
        }
    }

    #endregion

    /// <summary>下载信息汇总。</summary>
    public readonly struct DownloadSummary
    {
        public readonly int Count;
        public readonly long Bytes;

        public DownloadSummary(int count, long bytes)
        {
            Count = count;
            Bytes = bytes;
        }

        public bool HasUpdate => Count > 0 || Bytes > 0;
    }
}