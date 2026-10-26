using cfg;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>固定属性修改效果：按固定值加减某个属性（正数加、负数减）。</summary>
    [LabelText("固定属性修改效果")]
    public class FixedAttrModifyEffect : GameplayInstantEffect
    {
        [LabelText("固定修改值"), Tooltip("正数=增加，负数=减少")]
        public float fixedModify;

        [LabelText("修改属性类型"), Required]
        public EAttributeType attrType;

        [LabelText("是否开启伤害跳字"), ShowIf("@attrType==EAttributeType.Hp")]
        public bool isFloatingText;

        protected override void ApplyEffect()
        {
            // 开启伤害跳字
            if (attrType == EAttributeType.Hp && isFloatingText)
            {
                EventCenter.Global.Publish(FloatTextEventKey.RequestKey, new FloatTextRequest()
                {
                    isCritical = false,
                    kind = FloatTextKind.Damage,
                    value = fixedModify,
                    worldPos = target.transform.position + Vector3.up * 1.6f,
                });
            }  
            // 返回实际改动量（被上下限截断之后真正变了多少）
            target.AddAttrValue(attrType, AttributeValueType.CurValue, fixedModify);
        }
    }
}
