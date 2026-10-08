using System;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 战斗飘字管理器(3D 世界空间版,单例组件,运行时挂在 GameRoot 下常驻)。
///
/// 【职责】
/// 1. 订阅战斗层发布的飘字事件(<see cref="FloatTextEventKey.RequestKey"/>,发布方是
///    NormalDamageEffect 等伤害/治疗结算处),战斗层只管发数据、UI 层只管显示,单向解耦;
/// 2. 把请求里的【世界坐标】加上头顶偏移与随机散落后作为文字出生点,
///    在世界中直接生成 3D TMP 飘字(不依赖任何 Canvas / 屏幕坐标换算);
/// 3. 从对象池(<see cref="PoolSystem"/>)取一条飘字预制体实例交给
///    <see cref="FloatingTextItem"/> 播放,播完由该 Item 自己回池。
///
/// 【为什么不直接在场景里放一个组件?】
/// GameRoot 是跨场景常驻的根,飘字需要"任何场景都能用",所以这里用静态 EnsureInit 在
/// GameRoot 下临时 new 一个对象并挂上本组件;Awake 里做单例守卫,保证全项目只有一份。
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    /// <summary>
    /// 飘字预制体在 Addressables 里的固定地址。
    /// 与编辑器脚本 FloatingTextAssetCreator 注册的 address 完全一致,二者必须同名。
    /// </summary>
    public const string PrefabAddress = "FloatingText";

    // ============================ 单例引用 ============================

    private static FloatingTextManager _instance;

    /// <summary>
    /// 全局访问入口。只有经过 EnsureInit 成功创建后才有值;
    /// 外部(如 Test 脚本)一般先 EnsureInit 再访问 Instance。
    /// </summary>
    public static FloatingTextManager Instance => _instance;

    // ============================ 可配置字段(Inspector 可调) ============================

    /// <summary>
    /// 飘字预制体模板(3D TMP)。首次 Show 前为 null,会按 <see cref="PrefabAddress"/>
    /// 从 Addressables 加载并缓存到这里;也可以在 Inspector 手动拖一个同结构预制体。
    /// </summary>
    [LabelText("飘字预制体(Addressable 加载缓存)"), SerializeField]
    private GameObject _itemPrefab;

    /// <summary>
    /// 世界坐标头顶偏移:飘字显示在世界点上方 1.6m(约角色头顶)。
    /// </summary>
    [LabelText("世界坐标头顶偏移"), SerializeField] private Vector3 _headOffset = new (0f, .8f, 0f);

    /// <summary>
    /// 出生点随机散落范围(世界单位):x=左右最多偏移, y=向上额外随机抬升 0~y。
    /// 用于同一时刻多个伤害在相近位置时不至于完全重叠。
    /// </summary>
    [LabelText("出生点随机散落范围(世界单位)"), SerializeField]
    private Vector2 _spreadRange = new (0.35f, 0.25f);

    // ============================ 初始化 ============================

    /// <summary>
    /// 确保管理器存在(幂等)。
    /// 调用时机:UI_GameWindow 打开(GameScene 进场)时调用;任何外部代码也可直接调用。
    /// </summary>
    public static void EnsureInit()
    {
        if (_instance != null) return;

        // GameRoot 还没 Awake(域重载后首帧)时拿不到根,直接放弃,等下次再调用即可
        Transform root = GameRoot.RootTransform;
        if (root == null) return;

        // 先建空物体再 AddComponent:AddComponent 会立即触发 Awake,所以订阅在 Awake 里做
        GameObject go = new("FloatingTextManager");
        go.transform.SetParent(root, false);   // false = 不保持世界坐标,直接作为子物体放原点
        go.AddComponent<FloatingTextManager>();
    }

    /// <summary>组件就绪:单例守卫 → 订阅伤害飘字事件。</summary>
    private void Awake()
    {
        // 单例守卫:如果已经存在一份,销毁自己
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // 订阅战斗层的飘字事件;退订在 OnDestroy,避免事件系统持有已销毁对象的引用(内存泄漏)
        EventCenter.Global.Subscribe(FloatTextEventKey.RequestKey, OnFloatTextRequest);
    }

    /// <summary>组件销毁:清空单例并退订事件。</summary>
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        EventCenter.Global.UnSubscribe(FloatTextEventKey.RequestKey, OnFloatTextRequest);
    }

    // ============================ 事件入口 ============================

    /// <summary>
    /// 事件回调:收到 <see cref="FloatTextEventKey.RequestKey"/> 后把请求拆给内部 Show 处理。
    /// </summary>
    private void OnFloatTextRequest(FloatTextRequest request)
    {
        Show(request.worldPos, request.value, request.isCritical, request.kind);
    }

    /// <summary>
    /// 外部直接调用的展示入口(测试/非 Effect 路径)。
    /// </summary>
    /// <param name="worldPos">要飘字的世界坐标(如角色头顶)</param>
    /// <param name="value">数值(显示时由 Item 取整)</param>
    /// <param name="isCritical">是否暴击(影响字号/颜色)</param>
    /// <param name="kind">飘字类型(伤害/治疗,影响颜色)</param>
    public void ShowAt(Vector3 worldPos, float value, bool isCritical = false, FloatTextKind kind = FloatTextKind.Damage)
    {
        Show(worldPos, value, isCritical, kind);
    }

    // ============================ 核心展示流程 ============================

    /// <summary>
    /// 在世界中展示一条飘字:
    /// 1. 前置检查(预制体可加载);
    /// 2. 出生点 = 世界坐标 + 头顶偏移 + 随机散落;
    /// 3. 从对象池取 FloatingTextItem(取不到就按预制体 Instantiate 一个),
    ///    把物体放到出生点,剩下动画/回池由 Item 自己负责。
    /// </summary>
    private void Show(Vector3 worldPos, float value, bool isCritical, FloatTextKind kind)
    {
        // 预制体还没加载过 → 尝试按 Addressable key 加载;失败则放弃
        if (!_itemPrefab && !TryLoadPrefab()) return;

        // 世界出生点:头顶偏移 + 随机散落(避免同屏叠字)
        Vector3 spawnPos = worldPos + _headOffset + new Vector3(
            UnityEngine.Random.Range(-_spreadRange.x, _spreadRange.x),
            UnityEngine.Random.Range(0f, _spreadRange.y),
            UnityEngine.Random.Range(-_spreadRange.x, _spreadRange.x));

        // 取池实例(不传 parent:对象池会在场景根与池根之间转移;3D 文字无需 Canvas)
        FloatingTextItem item = PoolSystem.Global.GetGameObject<FloatingTextItem>(_itemPrefab, null);
        if (!item) return;

        item.transform.position = spawnPos;
        item.transform.localScale = Vector3.one;
        item.Play(new FloatTextRequest
        {
            worldPos = spawnPos,   // Item 可直接从自身 position 上飘,这里保留以便扩展
            value = value,
            isCritical = isCritical,
            kind = kind,
        });
    }

    // ============================ 预制体加载 ============================

    /// <summary>
    /// 按 <see cref="PrefabAddress"/> 从 Addressables 同步加载飘字预制体并缓存。
    /// 只加载一次(成功后再调用直接返回 true);失败会打错误日志,不影响游戏主流程。
    /// </summary>
    private bool TryLoadPrefab()
    {
        try
        {
            // WaitForCompletion:编辑器/未预加载环境下同步取到结果,保证 Show 流程可串行继续
            _itemPrefab = Addressables.LoadAssetAsync<GameObject>(PrefabAddress).WaitForCompletion();
        }
        catch (Exception e)
        {
            Debug.LogError($"FloatingTextManager: 加载预制体失败({PrefabAddress}) → {e.Message}");
        }

        return _itemPrefab;
    }
}
