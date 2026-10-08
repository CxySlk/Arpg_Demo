using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 瞬时伤害效果：结算 (攻击 - 防御 + 额外伤害) 并弹伤害飘字。
    /// </summary>
    public class NormalDamageEffect : GameplayInstantEffect
    {
        [LabelText("伤害值（大于0）")] public float damage;
        [LabelText("是否可以被格挡")] public bool isParry = true;

        [LabelText("是否开启伤害跳字")]
        public bool isFloatingText;

        protected override void ApplyEffect()
        {
            source.TryGetAttrValue(AttributeType.Atk, out Attribute atkAttr);
            target.TryGetAttrValue(AttributeType.Def, out Attribute defAttr);

            float _damage = Mathf.Max(0, (atkAttr?.CurValue ?? 0) - (defAttr?.CurValue ?? 0) + damage);

            // 伤害跳字
            if (isFloatingText)
            {
                EventCenter.Global.Publish(FloatTextEventKey.RequestKey, new FloatTextRequest()
                {
                    isCritical = false,
                    kind = FloatTextKind.Damage,
                    value = -_damage,
                    worldPos = target!.transform.position
                });
            }
            
            // 告知受击方"这一击允许被格挡"（payload = 攻击方）。
            // 只是告知，不代表一定被格挡：受击方具备格挡能力的技能行为会去监听这个事件，
            // 自己决定要不要格挡（没有格挡能力的技能不监听即可）。
            if (isParry)
                target.GasEventCenter.Publish(GASEventCenterKeys.ParryableAttackEventKey, source ? source.gameObject : null);

            target.AddAttrValue(AttributeType.Hp, AttributeValueType.CurValue, -_damage);
        }
    }
}