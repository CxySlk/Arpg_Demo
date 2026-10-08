using LLKFrame;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续特效 Cue 的 Spec。生成粒子实例，达到 DurationSeconds 秒后回收。
    /// </summary>
    public class GameplayCueDurationalParticleSpec : GameplayCueDurationalSpec<GameplayCueDurationalParticle>
    {
        private ParticleSystem _instance;

        public override void Apply()
        {
            base.Apply();
            Transform targetTrans = (target as Component)!.transform;
            Transform parent = DurationalCue.FollowTarget? targetTrans : null;
            _instance = PoolSystem.Global.GetGameObject(DurationalCue.ParticlePrefab, parent)
                .GetComponent<ParticleSystem>();
            _instance.transform.position = targetTrans.TransformPoint(DurationalCue.Position);
            // 旋转顺序：角色旋转在前、偏移在后 → 偏移在角色局部坐标生效，角色朝不同方向时粒子角度保持一致
            _instance.transform.rotation = targetTrans.rotation * Quaternion.Euler(DurationalCue.Rotation);
            _instance.Play();
        }

        protected override void Destroy()
        {
            base.Destroy();
            _instance.Stop();
            _instance.Clear();
            _instance.PushGameObject();
            _instance = null;
        }
    }
}