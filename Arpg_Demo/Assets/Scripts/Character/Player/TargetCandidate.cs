using UnityEngine;

/// <summary>
/// 锁敌候选：一个可锁定目标（敌人根）以及它提前算好的锁定锚点。
/// <para>
/// 为什么缓存 <see cref="LockPos"/>：锁定锚点 = 敌人位置 + 胸口高度，
/// 评分（屏幕投影）、相机 LookAt、UI 锁定框都会用到；
/// 在收集候选时算一次，避免每帧反复计算。
/// </para>
/// </summary>
public struct TargetCandidate
{
    /// <summary>敌人根 Transform：用于 SetTarget、角色转向、相机 LookAt。</summary>
    public Transform Target;

    /// <summary>世界坐标下的锁定点（敌人位置 + 胸口高度）。</summary>
    public Vector3 LockPos;

    /// <param name="target">敌人根 Transform。</param>
    /// <param name="lockPos">世界坐标锁定点（通常由 TargetSelector.AnchorOf 计算）。</param>
    public TargetCandidate(Transform target, Vector3 lockPos)
    {
        Target = target;
        LockPos = lockPos;
    }
}
