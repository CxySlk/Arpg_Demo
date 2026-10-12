using System.Collections.Generic;
using GAS;
using Hovl;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class WeaponController : MonoBehaviour
    {
        [LabelText("武器"), SerializeField] private List<Weapon> _weapons = new();
        private CharacterControllerBase controller;
        public EventCenter GasEventCenter => controller.Asc.GasEventCenter;

        public void Init(CharacterControllerBase controller)
        {
            this.controller = controller;
            foreach (Weapon weapon in _weapons)
            {
                weapon.Init(this);
            }

            // 订阅开启武器事件
            GasEventCenter.Subscribe(GASEventCenterKeys.StartWeaponColliderKey, StartWeapons);
            GasEventCenter.Subscribe(GASEventCenterKeys.StartAllWeaponColliderKey, StartAllWeapons);
            // 订阅关闭武器事件
            GasEventCenter.Subscribe(GASEventCenterKeys.StopWeaponColliderKey, StopWeapons);
            GasEventCenter.Subscribe(GASEventCenterKeys.StopAllWeaponColliderKey, StopAllWeapons);
        }

        private void OnDisable()
        {
            // 取消订阅开启武器事件
            GasEventCenter.UnSubscribe(GASEventCenterKeys.StartWeaponColliderKey, StartWeapons);
            GasEventCenter.UnSubscribe(GASEventCenterKeys.StartAllWeaponColliderKey, StartAllWeapons);
            // 取消订阅关闭武器事件
            GasEventCenter.UnSubscribe(GASEventCenterKeys.StopWeaponColliderKey, StopWeapons);
            GasEventCenter.UnSubscribe(GASEventCenterKeys.StopAllWeaponColliderKey, StopAllWeapons);
        }

        /// <summary>
        /// 开启所有武器
        /// </summary>
        public void StartAllWeapons(LayerMask layerMask)
        {
            foreach (Weapon weapon in _weapons)
                weapon.StartWeapon(layerMask);
        }

        public void StartWeapons(int weaponKeys, LayerMask layerMask)
        {
            // 按位开启：0 表示全部；例如 3(0b11) → 开启第 0、1 把武器
            if (weaponKeys == 0)
            {
                StartAllWeapons(layerMask);
                return;
            }

            int i = 0;
            while (weaponKeys > 0)
            {
                if ((weaponKeys & 1) == 1)
                    _weapons[i].StartWeapon(layerMask);
                weaponKeys >>= 1;
                i++;
            }
        }

        /// <summary>
        /// 关闭所有武器
        /// </summary>
        public void StopAllWeapons()
        {
            foreach (Weapon weapon in _weapons)
                weapon.StopWeapon();
        }


        public void StopWeapons(int weaponKeys)
        {
            // 按位关闭：0 表示全部；例如 3(0b11) → 关闭第 0、1 把武器
            if (weaponKeys == 0)
            {
                StopAllWeapons();
                return;
            }

            int i = 0;
            while (weaponKeys > 0)
            {
                if ((weaponKeys & 1) == 1)
                    _weapons[i].StopWeapon();
                weaponKeys >>= 1;
                i++;
            }
        }

        /// <summary>关闭所有武器(死亡等场景):只关碰撞体/拖尾,不隐藏武器物体。</summary>
        public void Close()
        {
            foreach (Weapon weapon in _weapons)
                weapon.Close();
        }

        public void ApplyTrailPreset(HS_SwordTrailPreset trailPreset)
        {
            if (!trailPreset) return;
            foreach (Weapon weapon in _weapons)
            {
                weapon.ApplyTrailPreset(trailPreset);
            }
        }

#if UNITY_EDITOR
        [Button("自动加载所有武器")]
        private void LoadAllWeapons()
        {
            Weapon[] weapons = GetComponentsInChildren<Weapon>();
            _weapons.Clear();
            foreach (Weapon weapon in weapons)
            {
                _weapons.Add(weapon);
            }
        }
#endif
    }
}