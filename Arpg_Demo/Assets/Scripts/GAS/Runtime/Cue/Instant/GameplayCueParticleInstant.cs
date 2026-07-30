using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 瞬时粒子 Cue 资产。配置粒子预制体、生成偏移、是否跟随目标和自动回收时间。
    /// 触发时在所有者 ASC 位置生成预制体实例。
    /// </summary>
    [CreateAssetMenu(fileName = "Cue_Particle", menuName = "GAS/即时粒子Cue")]
    public class GameplayCueParticleInstant : GameplayCueInstant
    {
        [LabelText("粒子预制体"), SerializeField, OnValueChanged(nameof(OnParticlePrefabChanged))]
        private GameObject _particlePrefab;
        public GameObject ParticlePrefab => _particlePrefab;

        [LabelText("生成位置偏移"), SerializeField]
        private Vector3 _posOffset;
        public Vector3 PosOffset => _posOffset;

        [LabelText("生成旋转偏移"), SerializeField]
        private Vector3 _rotationOffset;
        public Vector3 RotationOffset => _rotationOffset;

        [LabelText("跟随目标"), SerializeField, Tooltip("需要运动的粒子效果（如 Buff 光环）勾选此项")]
        private bool _followTarget;
        public bool FollowTarget => _followTarget;

        [LabelText("自动回收(秒)"), SerializeField, Tooltip("<=0 表示不自动回收，需手动销毁")]
        private float _autoRecycleTime = 2f;
        public float AutoRecycleTime => _autoRecycleTime;

        public override GameplayCueInstantSpec CreateSpec() => PoolSystem.GetOrNewObject<GameplayCueParticleInstantSpec>();

#if UNITY_EDITOR
        private void OnParticlePrefabChanged()
        {
            if (_particlePrefab == null) return;
            var allPs = _particlePrefab.GetComponentsInChildren<ParticleSystem>();
            float maxTime = 0;
            for (int i = 0; i < allPs.Length; i++)
            {
                var main = allPs[i].main;
                float t = main.duration + main.startLifetime.constantMax;
                if (t > maxTime) maxTime = t;
            }
            _autoRecycleTime = maxTime;
        }
#endif
    }
}
