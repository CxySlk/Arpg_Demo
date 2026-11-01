using GAS;
using LLKFrame;
using ParadoxNotion;
using UnityEngine;

namespace Bullet
{
    public abstract class BulletBase : MonoBehaviour
    {
        protected BulletData _bulletData;
        protected AbilitySystemComponent _owner;
        protected float _moveSpeed;
        protected float _curDuration;
        protected static Transform RootTrans
        {
            get
            {
                if (_rootTrans == null)
                    _rootTrans = new GameObject("BulletRoot").transform;
                return _rootTrans;
            }
        }

        private static Transform _rootTrans;

        public void Init(BulletData bulletData, AbilitySystemComponent owner)
        {
            _bulletData = bulletData;
            _owner = owner;
            _moveSpeed = 0;
            _curDuration = _bulletData.maxDuration;
        }

        protected virtual void Update()
        {
            if (_bulletData == null) return;

            _curDuration -= Time.deltaTime;
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

        private void OnTriggerEnter(Collider other)
        {
            if (_bulletData == null) return;

            // 碰撞到目标
            if (other.gameObject.IsInLayerMask(_bulletData.targetLayerMask))
                OnShootTarget(other);
            // 障碍物
            else if (other.gameObject.IsInLayerMask(_bulletData.colliderLayerMask))
                OnShootObstacle(other);
        }

        protected virtual void OnShootTarget(Collider other)
        {
            AbilitySystemComponent asc = other.transform.GetComponentInChildren<AbilitySystemComponent>();
            if (!asc) return;
            foreach (GameplayEffect effect in _bulletData.effects)
                asc.ApplyEffect(effect, _owner);
            Recycle();
        }

        protected virtual void OnShootObstacle(Collider other)
        {
            Recycle();
        }

        private void Recycle()
        {
            if (_bulletData == null) return;

            _bulletData = null;
            _moveSpeed = 0;
            PoolSystem.Global.PushGameObject(gameObject);
        }
    }
}