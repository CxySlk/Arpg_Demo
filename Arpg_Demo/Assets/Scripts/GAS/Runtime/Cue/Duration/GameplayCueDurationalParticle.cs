using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续特效 Cue 模板。配置粒子预制体，触发时生成粒子，持续 DurationSeconds 秒后回收。
    /// </summary>
    [System.Serializable]
    public class GameplayCueDurationalParticle : GameplayCueDurational
    {
        [LabelText("粒子预制体"), OnValueChanged(nameof(CalculateDuration))]
        public GameObject ParticlePrefab;

        [LabelText("生成位置偏移")]
        public Vector3 Position;

        [LabelText("生成旋转偏移"), OnValueChanged("OnRotationChanged")]
        public Vector3 Rotation;

        [LabelText("跟随目标"), Tooltip("需要运动的粒子效果（如 Buff 光环）勾选此项")]
        public bool FollowTarget;

        protected override GameplayCueSpec CreateSpec() => PoolSystem.Global.GetOrNewObject<GameplayCueDurationalParticleSpec>();

        public override GameplayCueDurational DeepCopy()
        {
            GameplayCueDurationalParticle particleCue = PoolSystem.Global.GetOrNewObject<GameplayCueDurationalParticle>();
            particleCue.DurationSeconds = DurationSeconds;
            particleCue.ParticlePrefab = ParticlePrefab;
            particleCue.Position = Position;
            particleCue.Rotation = Rotation;
            particleCue.FollowTarget = FollowTarget;
            return particleCue;
        }

        #region Editor相关
#if UNITY_EDITOR
        private void OnRotationChanged()
        {
            // 归一到 (-180, 180]：避免 180° 被取模成 0°（与 SceneGizmoView.NormalizeRotation 一致）
            Rotation = new Vector3(
                Mathf.DeltaAngle(0f, Rotation.x),
                Mathf.DeltaAngle(0f, Rotation.y),
                Mathf.DeltaAngle(0f, Rotation.z));
        }

        /// <summary>持续时长 = 粒子系统里最长的 (startLifetime + duration)（秒）</summary>
        public override void CalculateDuration()
        {
            if (ParticlePrefab == null)
            {
                DurationSeconds = 0f;
                return;
            }

            ParticleSystem[] particleSystems = ParticlePrefab.GetComponentsInChildren<ParticleSystem>();
            float maxTime = 0;
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                float time = particleSystem.main.startLifetime.constantMax + particleSystem.main.duration;
                if (maxTime < time)
                    maxTime = time;
            }

            DurationSeconds = maxTime;
        }
#endif
        #endregion
    }
}
