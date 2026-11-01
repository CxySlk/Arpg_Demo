using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using VFX;

namespace Bullet
{
    public class OrbBullet : BulletBase
    {
        [LabelText("爆炸效果"), SerializeField] private GameObject _explodeEffect;
        [LabelText("音效"), SerializeField] private AudioClip _audioClip;
        [LabelText("音量"), SerializeField] private float _volume = 1;

        protected override void OnShootTarget(Collider other)
        {
            Vector3 point = other.ClosestPoint(transform.position);
            SpawnExplosion(point);
            base.OnShootTarget(other);
        }

        protected override void OnShootObstacle(Collider other)
        {
            Vector3 point = other.ClosestPoint(transform.position);
            SpawnExplosion(point);
            base.OnShootObstacle(other);
        }

        private void SpawnExplosion(Vector3 point)
        {
            if (_explodeEffect == null) return;

            GameObject explode = PoolSystem.Global.GetGameObject(_explodeEffect, RootTrans);
            FireballExplosionEffect explosion = explode.GetComponent<FireballExplosionEffect>();
            if (explosion != null)
                explosion.PlayAt(point);
            else
                explode.transform.position = point;

            // 播放爆炸音效
            if (_audioClip)  AudioSystem.PlayEffectMusic(_audioClip, point, _volume);
        }
    }
}