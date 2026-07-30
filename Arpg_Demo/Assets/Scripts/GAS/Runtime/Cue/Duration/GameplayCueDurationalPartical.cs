using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public class GameplayCueDurationalPartical : GameplayCueDurational
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

        public override GameplayCueDurationalSpec CreateSpec() => PoolSystem.GetOrNewObject<GameplayCueDurationalParticalSpec>();

#if UNITY_EDITOR
        private void OnParticlePrefabChanged()
        {
            if (_particlePrefab == null) return;
            ParticleSystem[] particleSystems = _particlePrefab.GetComponentsInChildren<ParticleSystem>();
            float maxTime = 0;
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                float time = particleSystem.main.startLifetime.constantMax + particleSystem.main.duration;
                if (maxTime < time)
                    maxTime = time;
            }
            DurationFrame = Mathf.FloorToInt(maxTime * GASTimer.FrameRate);
        }
#endif
    }
}
