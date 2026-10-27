using System.Collections.Generic;
using Character;
using UnityEngine;

/// <summary>
/// 锁敌选择器（纯逻辑）：候选收集、合法性校验、评分选优与方向切换。
/// <para>
/// 刻意不继承 MonoBehaviour、不碰输入：
/// 这样它就是一段可单独测试、不依赖场景状态的逻辑，输入/生命周期留给 <see cref="PlayerTargetLock"/>。
/// </para>
/// <para>
/// 评分公式（<see cref="Score"/>）：
/// <c>屏幕中心分 * wScreen + 距离分 * wDistance + 当前目标粘性</c>。
/// 其中"屏幕中心分"是主项——魂系锁定选的是"离屏幕中心最近"的敌人，而不是单纯最近的。
/// </para>
/// </summary>
public class TargetSelector
{
    #region 配置与缓存

    // OverlapSphereNonAlloc 的复用缓冲：避免每次收集候选都 new 数组产生 GC。
    private readonly Collider[] _buffer;

    // 切目标排序时的复用列表（同样是避免每帧 new）。
    private List<TargetCandidate> _sortBuffer;

    // 同一敌人可能挂多个碰撞体（身体/武器/Trigger），候选按角色去重。
    private readonly HashSet<CharacterControllerBase> _seenTargets = new();

    private float _lockRadius;      // 锁定半径（米）：初次获取目标的范围
    private float _loseRadius;      // 脱离半径（米）：已锁定的目标超出这个范围才算丢失（迟滞）
    private float _anchorHeight;    // 锁定锚点相对敌人根的高度（米，胸口位置）
    private Vector3 _eyeOffset;     // 视线检测起点相对角色根的位置
    private LayerMask _enemyMask;   // 可锁定的敌人层
    private LayerMask _obstacleMask;// 遮挡层（墙/环境）

    private float _wScreen;         // 屏幕中心评分的权重
    private float _wDistance;       // 世界距离评分的权重
    private float _stickyBonus;     // 当前目标额外加分（避免目标频繁跳动）

    public TargetSelector(int bufferSize = 32)
    {
        _buffer = new Collider[bufferSize];
    }

    /// <summary>由外部（PlayerTargetLock 的 Inspector 参数）集中注入配置。</summary>
    public void Configure(float lockRadius, float loseRadius, float anchorHeight, Vector3 eyeOffset,
        LayerMask enemyMask, LayerMask obstacleMask,
        float wScreen, float wDistance, float stickyBonus)
    {
        _lockRadius = lockRadius;
        _loseRadius = Mathf.Max(lockRadius, loseRadius);
        _anchorHeight = anchorHeight;
        _eyeOffset = eyeOffset;
        _enemyMask = enemyMask;
        _obstacleMask = obstacleMask;
        _wScreen = wScreen;
        _wDistance = wDistance;
        _stickyBonus = stickyBonus;
    }

    #endregion

    #region 候选收集与校验

    /// <summary>
    /// 目标的锁定锚点（世界坐标）。
    /// 敌人 pivot 通常在脚底，直接用它做锁定/相机 LookAt 会偏低，所以统一加一个胸口高度。
    /// </summary>
    public Vector3 AnchorOf(Transform target) => target.position + Vector3.up * _anchorHeight;

    /// <summary>
    /// 收集锁定半径内的合法候选（存活 + 视线不被遮挡），写入 results（先清空，便于复用列表）。
    /// </summary>
    public void Gather(Vector3 origin, List<TargetCandidate> results)
    {
        results.Clear();
        _seenTargets.Clear();

        // NonAlloc：结果写进复用缓冲，避免每帧分配数组。
        int count = Physics.OverlapSphereNonAlloc(origin, _lockRadius, _buffer, _enemyMask, QueryTriggerInteraction.Collide);
        Vector3 eye = origin + _eyeOffset;

        for (int i = 0; i < count; i++)
        {
            Collider col = _buffer[i];
            if (!col) continue; // 缓冲里可能残留旧引用

            // 碰撞体通常挂在模型的子物体上，所以要从父级找角色控制器。
            CharacterControllerBase target = col.GetComponentInParent<CharacterControllerBase>();
            if (!target || target.IsDead) continue; // 死亡目标不可锁定
            if (!_seenTargets.Add(target)) continue;

            Transform t = target.transform;
            Vector3 anchor = AnchorOf(t);
            if (HasObstacle(eye, anchor)) continue; // 被墙挡住也看不见

            results.Add(new TargetCandidate(t, anchor));
        }
    }

    /// <summary>
    /// 当前目标是否仍然合法：存活 + 在脱离半径内。
    /// <para>
    /// 刻意不检查视线：锁定的敌人躲到柱子后面也应保持锁定，
    /// 否则目标一被遮挡就自动切、露出来又切回来，会来回跳。
    /// 视线只用于 <see cref="Gather"/> 初次筛选候选。
    /// </para>
    /// </summary>
    public bool IsValid(Transform target, Vector3 origin)
    {
        return !IsTargetDead(target) && IsInLoseRange(target, origin);
    }

    /// <summary>目标是否还在脱离半径内（用 sqrMagnitude 省一次开方）。</summary>
    public bool IsInLoseRange(Transform target, Vector3 origin)
    {
        if (!target) return false;
        return (AnchorOf(target) - origin).sqrMagnitude <= _loseRadius * _loseRadius;
    }

    /// <summary>目标是否已死亡/无效（null 也视为无效，方便调用方安全判断）。</summary>
    public bool IsTargetDead(Transform target)
    {
        if (!target) return true;
        CharacterControllerBase c = target.GetComponentInParent<CharacterControllerBase>();
        return !c || c.IsDead;
    }

    #endregion

    #region 选择

    /// <summary>
    /// 选评分最高的候选。
    /// </summary>
    /// <param name="current">当前目标：会获得粘性加分，避免有更优目标时立刻跳走。</param>
    /// <param name="exclude">要跳过的目标：自动切目标时排除刚丢失的旧目标，防止又选回它。</param>
    public Transform SelectBest(List<TargetCandidate> candidates, Transform current, Transform exclude, Camera cam, Vector3 origin)
    {
        Transform best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            TargetCandidate c = candidates[i];
            if (c.Target == exclude) continue;

            float s = Score(c, current, cam, origin);
            if (s > bestScore)
            {
                bestScore = s;
                best = c.Target;
            }
        }
        return best;
    }

    /// <summary>
    /// 按"屏幕从左到右"的顺序取当前目标的相邻候选。
    /// <para>
    /// 滚轮是一维输入，没法像右摇杆那样做方向叉积，所以把候选按屏幕 X 排序，
    /// 滚轮一格就取前/后一个（首尾循环）。
    /// </para>
    /// </summary>
    /// <param name="dir">大于 0 = 下一个/右，小于 0 = 上一个/左。</param>
    public Transform SelectNextByScreenX(List<TargetCandidate> candidates, Transform current, int dir, Camera cam)
    {
        if (candidates.Count == 0 || !current) return null;

        // 复用排序列表，避免每次切换都 new。
        if (_sortBuffer == null) _sortBuffer = new List<TargetCandidate>(candidates.Count);
        _sortBuffer.Clear();
        _sortBuffer.AddRange(candidates);
        _sortBuffer.Sort((a, b) => ScreenX(a.LockPos, cam).CompareTo(ScreenX(b.LockPos, cam)));

        int idx = _sortBuffer.FindIndex(c => c.Target == current);
        if (idx < 0) return _sortBuffer[0].Target; // 当前目标不在候选里（刚丢失），从最左侧开始

        // 取模实现首尾循环：最右再往右回到最左。
        idx = (idx + (dir >= 0 ? 1 : -1) + _sortBuffer.Count) % _sortBuffer.Count;
        return _sortBuffer[idx].Target;
    }

    #endregion

    #region 评分

    /// <summary>单个候选的评分：屏幕中心（主）+ 距离（次）+ 当前目标粘性。</summary>
    private float Score(in TargetCandidate c, Transform current, Camera cam, Vector3 origin)
    {
        // 越靠屏幕中心越高（0~1）。
        float screen = 1f - Mathf.Clamp01(ScreenCenterOffset(c.LockPos, cam));

        // 越近越高（0~1），用锁定半径归一化。
        float dist = 1f - Mathf.Clamp01(Vector3.Distance(c.LockPos, origin) / Mathf.Max(0.0001f, _lockRadius));

        // 当前目标加分：只要它仍合法，就不轻易被别的目标抢走。
        float sticky = c.Target == current ? _stickyBonus : 0f;

        return screen * _wScreen + dist * _wDistance + sticky;
    }

    /// <summary>目标在屏幕上的横向位置（viewport.x）；在相机背后时返回 -1，排到最左。</summary>
    private static float ScreenX(Vector3 world, Camera cam)
    {
        if (!cam) return 0f;
        Vector3 vp = cam.WorldToViewportPoint(world);
        return vp.z <= 0f ? -1f : vp.x;
    }

    /// <summary>
    /// 目标离屏幕中心多远：viewport 中心 (0.5,0.5) 到目标的距离，×2 归一化到 0..1。
    /// 0 = 正中心，1 = 边缘/相机背后。
    /// </summary>
    private static float ScreenCenterOffset(Vector3 world, Camera cam)
    {
        Vector3 vp = cam.WorldToViewportPoint(world);
        if (vp.z <= 0f) return 1f; // 在相机后面：最差

        float dx = vp.x - 0.5f;
        float dy = vp.y - 0.5f;
        return Mathf.Clamp01(new Vector2(dx, dy).magnitude * 2f);
    }

    #endregion

    #region 工具

    /// <summary>从眼睛到锁定点做一次直线检测，命中遮挡层就表示看不见。</summary>
    private bool HasObstacle(Vector3 from, Vector3 to)
        => Physics.Linecast(from, to, _obstacleMask, QueryTriggerInteraction.Ignore);

    #endregion
}
