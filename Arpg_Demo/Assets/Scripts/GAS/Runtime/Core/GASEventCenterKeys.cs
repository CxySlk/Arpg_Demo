using System.Collections.Generic;
using cfg;
using LLKFrame;
using UnityEngine;

namespace GAS
{
    public static class GASEventCenterKeys
    {
        /// <summary>效果移除的事件</summary>
        public static readonly EventKey<GameplayTag> RemoveEffectEvent = "GAS.Effect.Remove";

        /// <summary>效果新增的事件</summary>
        public static readonly EventKey<GameplayDurationalEffect> AddEffectEvent = "GAS.Effect.Add";

        /// <summary>效果剩余归一化时间改变的事件</summary>
        public static readonly EventKey<GameplayTag, float> EffectRemainNormalizeTimeEvent =
            "GAS.Effect.Remain.NormalizeTime";

        /// <summary>效果层数改变的事件</summary>
        public static readonly EventKey<GameplayDurationalEffect, int> EffectStackChangedEvent =
            "GAS.Effect.Stack.Changed";

        /// <summary>新增属性事件</summary>
        public static readonly EventKey<Attribute> AddAttributeKey = "GAS.Attribute.Add";

        /// <summary>当前值变化事件，payload = (属性类型, 当前值)</summary>
        public static readonly EventKey<EAttributeType, float> CurValueChangedKey = "GAS.Attribute.CurValue.Changed";

        /// <summary>最小值变化事件，payload = (属性类型, 最小值)</summary>
        public static readonly EventKey<EAttributeType, float> MinValueChangedKey = "GAS.Attribute.MinValue.Changed";

        /// <summary>最大值变化事件，payload = (属性类型, 最大值)</summary>
        public static readonly EventKey<EAttributeType, float> MaxValueChangedKey = "GAS.Attribute.MaxValue.Changed";

        /// <summary>归一化值变化事件，payload = (属性类型, 归一值)</summary>
        public static readonly EventKey<EAttributeType, float> NormalizeValueChangedKey = "GAS.Attribute.NormalizeValue.Changed";

        /// <summary>
        /// 可以被格挡的攻击：攻击方在结算伤害时发布到**受击方**的事件中心，payload = 攻击方 GameObject。
        /// 只是"告知这一击允许格挡"，并不代表一定会被格挡 —— 是否格挡由受击方
        /// 具备格挡能力的技能行为自行监听后决定（没有该能力的技能不监听即可）。
        /// </summary>
        public static readonly EventKey<GameObject> ParryableAttackEventKey = "GAS.Combat.ParryableAttack";
        /// <summary>
        /// 开启武器碰撞事件
        /// List<int> 是要开启武器的ID
        /// </summary>
        public static readonly EventKey<int,LayerMask> StartWeaponColliderKey = "GAS.Combat.StartWeaponCollider";
        /// <summary>
        /// 开启武器碰撞事件
        /// List<int> 是要开启武器的ID
        /// </summary>
        public static readonly EventKey<LayerMask> StartAllWeaponColliderKey = "GAS.Combat.StartAllWeaponCollider";
        /// <summary>
        /// 关闭武器碰撞事件
        /// </summary>
        public static readonly EventKey<int> StopWeaponColliderKey = "GAS.Combat.StopWeaponCollider";
        /// <summary>
        /// 关闭所有武器碰撞事件
        /// </summary>
        public static readonly EventKey StopAllWeaponColliderKey = "GAS.Combat.StopAllWeaponCollider";
        /// <summary>
        /// 武器击中目标
        /// </summary>
        public static readonly EventKey<Collider> WeaponHitTarget = "GAS.Combat.WeaponHitTarget";
    }
}