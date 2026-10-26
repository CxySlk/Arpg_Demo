using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 锁定相机驱动：读取 <see cref="PlayerTargetLock"/>，把 Cinemachine 相机切到锁定构图。
/// <para>
/// 锁定中：相机位置仍跟随玩家（Follow 不动），只把水平轨道角平滑对准"玩家→目标"，
/// 并把 LookAt 指向一个位于玩家与目标之间的代理点——不是目标本身。
/// 这样目标是画面上方、玩家在下方，两者都在画面里；直接把 LookAt 设成目标会把目标怼到正中、
/// 把近处的玩家挤出画面。
/// </para>
/// <para>
/// 锁定中禁用水平转视角（"Look Orbit X"），保留俯仰（"Look Orbit Y"）。
/// 不修改 Cinemachine 源码，只驱动它的公开 API；放在 LateUpdate 是为了在角色更新之后再覆盖参数。
/// </para>
/// </summary>
public class LockOnCameraDriver : MonoBehaviour
{
    [LabelText("水平角平滑时间(秒)"), SerializeField]
    private float _yawSmoothTime = 0.15f; // 越大转得越慢

    [LabelText("瞄准点偏向敌人(0=玩家,1=敌人)"), Range(0f, 1f), SerializeField]
    private float _lookWeight = 0.35f;    // 越小玩家越靠下、敌人越靠上

    [LabelText("瞄准点高度(相对脚底)"), SerializeField]
    private float _lookHeight = 1.2f;     // 玩家/敌人锚点高度（胸口）

    private CinemachineCamera _vcam;
    private CinemachineOrbitalFollow _orbital;
    private CinemachineRotationComposer _composer;
    private CinemachineInputAxisController _inputAxis;
    private PlayerTargetLock _lock;

    private Transform _lookProxy;        // 运行时创建的瞄准代理点（无渲染器）
    private Vector3 _savedTargetOffset;  // 进入锁定前 composer 的 TargetOffset
    private bool _hasSavedOffset;
    private bool _wasLocked;             // 上一帧是否锁定：用于"只在解锁那一帧"做恢复

    /// <summary>由 PlayerController 初始化时调用；几个 Cinemachine 组件都在同一台 vcam 上。</summary>
    public void Init(CinemachineCamera vcam, PlayerTargetLock targetLock)
    {
        _vcam = vcam;
        _lock = targetLock;
        _orbital = vcam.GetComponent<CinemachineOrbitalFollow>();
        _composer = vcam.GetComponent<CinemachineRotationComposer>();
        _inputAxis = vcam.GetComponent<CinemachineInputAxisController>();

        if (!_lookProxy)
        {
            // 代理点只是给 LookAt 用的空节点，隐藏起来不干扰层级。
            GameObject proxyGo = new GameObject("LockOnLookProxy");
            proxyGo.hideFlags = HideFlags.HideInHierarchy;
            _lookProxy = proxyGo.transform;
            _lookProxy.SetParent(transform, false);
            proxyGo.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (!_vcam || !_lock) return;

        Transform target = _lock.CurrentTarget;
        bool locked = _lock.IsLocked && target;

        if (locked)
        {
            Transform follow = _vcam.Follow;
            Vector3 origin = follow ? follow.position : transform.position;
            Vector3 flat = target.position - origin;
            flat.y = 0f;

            // 相机位置仍在玩家背后：水平轨道角平滑对准"玩家→目标"。
            if (_orbital && flat.sqrMagnitude > 0.0001f)
            {
                float want = Quaternion.LookRotation(flat).eulerAngles.y;

                // 指数平滑：1 - exp(-dt/tau)，帧率无关，不会像固定 lerp 系数那样随帧率变化。
                float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, _yawSmoothTime));
                _orbital.HorizontalAxis.Value = Mathf.LerpAngle(_orbital.HorizontalAxis.Value, want, k);
            }

            // 瞄准玩家与目标之间的加权点：目标偏上、玩家偏下，避免把玩家挤出画面。
            if (_lookProxy)
            {
                Vector3 playerAnchor = origin + Vector3.up * _lookHeight;
                Vector3 enemyAnchor = target.position + Vector3.up * _lookHeight;
                _lookProxy.position = Vector3.Lerp(playerAnchor, enemyAnchor, _lookWeight);

                if (!_lookProxy.gameObject.activeSelf)
                    _lookProxy.gameObject.SetActive(true);
            }

            _vcam.LookAt = _lookProxy ? _lookProxy : target;

            // 进入锁定的第一帧：暂存 composer 的偏移并置零，代理点已经带了高度。
            if (!_wasLocked && _composer && !_hasSavedOffset)
            {
                _savedTargetOffset = _composer.TargetOffset;
                _hasSavedOffset = true;
                _composer.TargetOffset = Vector3.zero;
            }

            // 锁定中禁用水平转视角，俯仰保留。
            SetControllerEnabled("Look Orbit X", false);
        }
        else if (_wasLocked)
        {
            // 只在刚解锁的那一帧恢复，避免每帧重复操作。
            // 注意：不要把 LookAt 设为 null——Cinemachine 的 LookAt setter 会把 CustomLookAtTarget
            // 又置回 true；清掉自定义标记后它会自动回落到 TrackingTarget（玩家），恢复默认视角。
            _vcam.Target.CustomLookAtTarget = false;
            SetControllerEnabled("Look Orbit X", true);

            if (_composer && _hasSavedOffset)
            {
                _composer.TargetOffset = _savedTargetOffset;
                _hasSavedOffset = false;
            }

            if (_lookProxy) _lookProxy.gameObject.SetActive(false);
        }

        _wasLocked = locked;
    }

    /// <summary>按编辑器里配置的轴名（如 "Look Orbit X"）启用/禁用某个输入轴控制器。</summary>
    private void SetControllerEnabled(string controllerName, bool enabled)
    {
        if (!_inputAxis) return;

        List<CinemachineInputAxisController.Controller> controllers = _inputAxis.Controllers;
        for (int i = 0; i < controllers.Count; i++)
        {
            if (controllers[i].Name == controllerName)
                controllers[i].Enabled = enabled;
        }
    }
}
