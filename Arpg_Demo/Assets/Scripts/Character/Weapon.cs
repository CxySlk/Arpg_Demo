using GAS;
using Hovl;
using LLKFrame;
using ParadoxNotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class Weapon : MonoBehaviour
    {
        [LabelText("武器碰撞体"), SerializeField, Required]
        private Collider _weaponCollider;
        [LabelText("武器拖尾"), SerializeField, Required]
        private HS_SwordMeshTrail  _swordTrail;

        public bool Status { get; private set; } = false;
        private LayerMask _triggerLayerMask;
        private WeaponController _controller;
        private EventCenter GasEventCenter => _controller.GasEventCenter;

        public void Init(WeaponController weaponController)
        {
            _controller = weaponController;
            StopWeapon();
        }

        /// <summary>
        /// 开启武器（碰撞）
        /// </summary>
        /// <param name="layerMask"></param>
        public void StartWeapon(LayerMask layerMask)
        {
            _triggerLayerMask = layerMask;
            _weaponCollider.enabled = true;
            Status = true;
        }

        /// <summary>
        /// 关闭武器（碰撞）
        /// </summary>
        public void StopWeapon()
        {
            _weaponCollider.enabled = false;
            Status = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.IsInLayerMask(_triggerLayerMask))
            {
                GasEventCenter.Publish(GASEventCenterKeys.WeaponHitTarget, other);
            }
        }

        public void ApplyTrailPreset(HS_SwordTrailPreset trailPreset)
        {
            _swordTrail?.ApplyTrailPreset(trailPreset);
        }
    }
}