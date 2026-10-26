using System;
using System.Collections.Generic;
using Character;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 玩家锁敌（魂系硬锁）。
/// <para>
/// 状态流转：未锁定 → 按锁定键 → 选屏幕中心附近最合适的目标 → 锁定中维持目标合法性 /
/// 滚轮或右摇杆切目标 / 目标死亡或超距时自动切下一个 → 再按锁定键解锁。
/// </para>
/// <para>
/// 只负责"锁谁"：选目标与维持状态。相机表现由 <see cref="LockOnCameraDriver"/> 读取本组件，
/// 角色朝向由基类的 <c>LookTargetUpdate</c> 读取 <c>Target</c> 完成。
/// </para>
/// </summary>
public class PlayerTargetLock : MonoBehaviour
{
    #region 配置

    [Title("范围")]
    [SerializeField] private float _lockRadius = 12f;          // 锁定半径（米）：初次获取目标的范围
    [SerializeField] private float _loseRadius = 16.8f;        // 脱离半径（米）：要大于锁定半径，避免边界抖动
    [SerializeField] private Vector3 _eyeOffset = new(0f, 1.5f, 0f); // 视线检测起点（相对角色根）
    [SerializeField] private float _anchorHeight = 1.2f;       // 锁定锚点高度（米，胸口）
    [SerializeField] private float _loseGrace = 0.5f;          // 超距丢失宽限（秒）：超时后才自动切
    [SerializeField] private float _refreshInterval = 0.1f;    // 候选刷新间隔（秒）

    [Title("评分权重")]
    [SerializeField] private float _wScreenCenter = 1f;        // 屏幕中心评分权重（主项）
    [SerializeField] private float _wDistance = 0.5f;          // 距离评分权重（次项）
    [SerializeField] private float _stickyBonus = 0.5f;        // 当前目标粘性加分（防频繁换目标）

    #endregion

    #region 运行时状态

    private CharacterControllerBase _controller; // 玩家控制器（提供 Target/IsDead/LockTarget）
    private TargetSelector _selector;            // 纯逻辑选择器
    private Camera _camera;                      // 用于屏幕空间评分
    private LayerMask _enemyMask;
    private LayerMask _obstacleMask;

    // 候选列表复用，避免每帧分配。
    private readonly List<TargetCandidate> _candidates = new();

    private Transform _current;      // 当前锁定目标
    private float _lostTimer;        // 当前目标持续非法的累计时间
    private float _refreshTimer;     // 距离下次刷新候选的剩余时间

    #endregion

    #region 对外状态

    /// <summary>当前锁定目标（未锁定/无目标时为 null）。</summary>
    public Transform CurrentTarget => _current;

    /// <summary>是否处于锁定状态（镜像控制器的 isLockTarget 标记）。</summary>
    public bool IsLocked => _controller != null && _controller.IsLocked;

    /// <summary>锁定目标变化时触发（切目标/解锁都会触发；解锁时参数为 null）。</summary>
    public event Action<Transform> OnTargetChanged;

    #endregion

    /// <summary>由 PlayerController 初始化时调用：绑定控制器并注入配置。</summary>
    public void Init(CharacterControllerBase controller)
    {
        _controller = controller;
        _camera = Camera.main;

        // 按层名取位掩码：敌人层可锁定，环境层挡视线。
        _enemyMask = 1 << LayerMask.NameToLayer("Enemy");
        _obstacleMask = 1 << LayerMask.NameToLayer("Env");

        _selector = new TargetSelector();
        _selector.Configure(_lockRadius, _loseRadius, _anchorHeight, _eyeOffset, _enemyMask, _obstacleMask,
            _wScreenCenter, _wDistance, _stickyBonus);
    }

    private void Update()
    {
        if (!_controller) return;
        if (!_camera) _camera = Camera.main; // 相机可能晚于玩家出现

        // 锁定键：toggle。基类 LockTarget 只负责置 isLockTarget 标记，具体选目标由下面处理。
        if (InputManager.LockOnInput)
            _controller.LockTarget(!_controller.IsLocked);

        if (!IsLocked)
        {
            // 未锁定：清掉当前目标并重置丢失计时。
            SetCurrent(null);
            _lostTimer = 0f;
            return;
        }

        // 候选按固定间隔刷新，而不是每帧全量搜（Overlap + LOS 都有成本）。
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = _refreshInterval;
            _selector.Gather(_controller.transform.position, _candidates);
        }

        Vector3 origin = _controller.transform.position;

        if (!_current)
        {
            // 刚进入锁定 / 目标为空：选屏幕中心附近评分最高的一个。
            SetCurrent(_selector.SelectBest(_candidates, null, null, _camera, origin));
            _lostTimer = 0f;
        }
        else if (_selector.IsTargetDead(_current))
        {
            // 目标死亡：立即切下一个（不等宽限），没有可选的就解锁。
            SwitchToNextOnLost(origin);
            if (!IsLocked) return;
        }
        else if (!_selector.IsInLoseRange(_current, origin))
        {
            // 只是超距：先宽限，避免在边界附近来回切。
            _lostTimer += Time.deltaTime;
            if (_lostTimer >= _loseGrace)
            {
                SwitchToNextOnLost(origin);
                if (!IsLocked) return;
            }
        }
        else
        {
            _lostTimer = 0f;
        }

        // 滚轮 / 右摇杆：按屏幕左右顺序切换目标。
        int switchDir = InputManager.SwitchTargetInput;
        if (switchDir != 0 && _current)
        {
            Transform next = _selector.SelectNextByScreenX(_candidates, _current, switchDir, _camera);
            if (next && next != _current) SetCurrent(next);
        }

        // 把目标同步给控制器：基类 LookTargetUpdate 会据此让角色面向敌人。
        _controller.SetTarget(_current);
    }

    /// <summary>
    /// 当前目标丢失（死亡/超距）时切到下一个：排除旧目标，没有可选的就直接解锁。
    /// </summary>
    private void SwitchToNextOnLost(Vector3 origin)
    {
        Transform next = _selector.SelectBest(_candidates, null, _current, _camera, origin);
        if (next)
        {
            SetCurrent(next);
            _lostTimer = 0f;
            return;
        }

        _controller.LockTarget(false);
        SetCurrent(null);
    }

    /// <summary>更新当前目标；只有变化时才广播，避免每帧触发事件。</summary>
    private void SetCurrent(Transform t)
    {
        if (_current == t) return;
        _current = t;
        OnTargetChanged?.Invoke(t);
    }
}
