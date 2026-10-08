using System.Collections;
using LLKFrame;
using UnityEngine;

namespace GAS
{
    public class GameplayCueInstantParticleSpec : GameplayCueInstantSpec<GameplayCueInstantParticle>
    {
        private ParticleSystem _particleSystem;

        public override void Apply()
        {
            _particleSystem =
                PoolSystem.Global.GetGameObject<ParticleSystem>(InstanceCue.particlePrefab,
                    (target as Component)!.transform);
            _particleSystem.Play();
            float totalTime = _particleSystem.main.startDelay.constantMax
                              + _particleSystem.main.duration
                              + _particleSystem.main.startLifetime.constantMax;
            MonoSystem.Start_Coroutine(WaitTimeRecycle(totalTime));
        }

        protected override void Destroy()
        {
            base.Destroy();
            _particleSystem.Stop();
            _particleSystem.PushGameObject();
            _particleSystem = null;
        }

        private IEnumerator WaitTimeRecycle(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            Destroy();
        }
    }
}