using Unity.Cinemachine;
using UnityEngine;

public partial class PlayerController
{
    #region 锁敌

    private PlayerTargetLock _targetLock;
    public PlayerTargetLock TargetLock => _targetLock;

    /// <summary>
    /// 创建/获取锁敌组件，并装配锁定相机驱动。
    /// 运行时装配而不是依赖 prefab 手工挂载：少一处需要记得拖的引用。
    /// </summary>
    private void InitTargetLock()
    {
        _targetLock = GetComponent<PlayerTargetLock>();
        if (_targetLock == null) _targetLock = gameObject.AddComponent<PlayerTargetLock>();
        _targetLock.Init(this); // 把本控制器（Target/IsDead/LockTarget）交给锁敌组件

        InitLockCamera();
    }

    /// <summary>
    /// 找到玩家这棵树里的 Cinemachine 相机并挂上锁定驱动。
    /// 用 <c>transform.root</c> 是因为相机（Cameras）是玩家节点（Player1）的兄弟，
    /// 只在 Player1 子树里找会找不到；从根往下找才能覆盖整棵玩家层级。
    /// </summary>
    private void InitLockCamera()
    {
        // GetComponentInChildren 的 true = 包含未激活物体（相机节点可能未激活）。
        CinemachineCamera vcam = transform.root.GetComponentInChildren<CinemachineCamera>(true);
        if (vcam == null) return;

        LockOnCameraDriver driver = vcam.GetComponent<LockOnCameraDriver>();
        if (driver == null) driver = vcam.gameObject.AddComponent<LockOnCameraDriver>();
        driver.Init(vcam, _targetLock);
    }

    #endregion
}
