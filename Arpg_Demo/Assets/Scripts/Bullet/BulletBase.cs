using GAS;
using LLKFrame;
using ParadoxNotion;
using UnityEngine;

namespace Bullet
{
    public class BulletBase : MonoBehaviour
    {
        private BulletData _bulletData;
        private AbilitySystemComponent _owner;
        private float _moveSpeed;
        private float _curDuration;

        public void Init(BulletData bulletData, AbilitySystemComponent owner)
        {
            _bulletData = bulletData;
            _owner = owner;
            _moveSpeed = 0;
            _curDuration = _bulletData.maxDuration;
        }

        protected virtual void Update()
        {
            _curDuration -=  Time.deltaTime;
            if (_curDuration <= 0)
            {
                Recycle();
                return;
            }
            
            // 子弹加速
            if (_moveSpeed < _bulletData.maxSpeed)
                _moveSpeed = Mathf.Min(_moveSpeed + Time.deltaTime * _bulletData.accelerate, _bulletData.maxSpeed);
            transform.Translate(Vector3.forward * (_moveSpeed * Time.deltaTime));
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 碰撞到目标
            if (collision.gameObject.IsInLayerMask(_bulletData.targetLayerMask))
            {
                AbilitySystemComponent asc = collision.transform.root.GetComponentInChildren<AbilitySystemComponent>();
                if (!asc) return;
                foreach (GameplayEffect effect in _bulletData.effects)
                    asc.ApplyEffect(effect, _owner);
                Recycle();
                return;
            }
            
            // 障碍物
            if (collision.gameObject.IsInLayerMask(_bulletData.colliderLayerMask))
                Recycle();
        }

        private void Recycle()
        {
            _bulletData = null;
            _moveSpeed = 0;
            PoolSystem.Global.PushGameObject(gameObject);
        }
    }
}