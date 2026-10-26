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
        private HS_SwordMeshTrail _swordTrail;

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

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.IsInLayerMask(_triggerLayerMask))
            {
                GasEventCenter.Publish(GASEventCenterKeys.WeaponHitTarget, other);
            }
        }

        /// <summary>
        /// 关闭武器:关碰撞体 + 停拖尾(不隐藏武器本体;死亡等场景用,武器仍可见)。
        /// </summary>
        public void Close()
        {
            _weaponCollider.enabled = false;
            Status = false;
            _swordTrail.StopTrail();
        }

        public void ApplyTrailPreset(HS_SwordTrailPreset trailPreset)
        {
            _swordTrail?.ApplyTrailPreset(trailPreset);
        }
    }
}