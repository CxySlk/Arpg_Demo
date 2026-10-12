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

        [LabelText("是否开启伤害跳字")] public bool isFloatingText;

        internal override void Execute(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (isParry) target.GasEventCenter.Publish(GASEventCenterKeys.ParryableAttackEventKey, source.gameObject);
            base.Execute(source, target);
        }

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

            target.AddAttrValue(AttributeType.Hp, AttributeValueType.CurValue, -_damage);
        }
    }
}