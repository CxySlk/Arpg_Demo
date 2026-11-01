using LLKFrame;
using UnityEngine;

namespace VFX
{
    public sealed class FireballExplosionEffect : MonoBehaviour
    {
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private float _lightFadeDuration = 0.35f;

        private ParticleSystem[] _particleSystems;
        private Light _light;
        private float _initialLightIntensity;
        private float _elapsed;

        private void Awake()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            _light = GetComponentInChildren<Light>(true);
            if (_light != null) _initialLightIntensity = _light.intensity;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            Restart();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopAll();
        }

        public void PlayAt(Vector3 position)
        {
            transform.position = position;
            Restart();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            _elapsed += Time.deltaTime;

            if (_light && _light.enabled)
            {
                float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _lightFadeDuration));
                _light.intensity = Mathf.Lerp(_initialLightIntensity, 0f, t);
                if (t >= 1f) _light.enabled = false;
            }

            if (_elapsed >= _duration)
                PoolSystem.Global.PushGameObject(gameObject);
        }

        private void Restart()
        {
            _elapsed = 0f;

            if (_light != null)
            {
                _light.enabled = true;
                _light.intensity = _initialLightIntensity;
            }

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null) continue;
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void StopAll()
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null) continue;
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
