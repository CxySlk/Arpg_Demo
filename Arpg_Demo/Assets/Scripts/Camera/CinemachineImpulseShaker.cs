using GAS;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>IScreenShaker 的 Cinemachine 实现：收到 velocity 就发一次脉冲。</summary>
public class CinemachineImpulseShaker : MonoBehaviour, IScreenShaker
{
    [LabelText("脉冲源"), SerializeField] private CinemachineImpulseSource _impulseSource;

    public void Shake(Vector3 velocity)
    {
        _impulseSource.GenerateImpulseWithVelocity(velocity);
    }
}
