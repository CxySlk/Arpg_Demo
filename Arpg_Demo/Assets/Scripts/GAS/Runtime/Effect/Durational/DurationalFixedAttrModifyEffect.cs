using cfg;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续固定属性修改效果：进入时按固定值加减属性，持续时间结束或被移除时按实际改动量返还。
    /// </summary>
    [LabelText("持续固定属性修改效果")]
    public class DurationalFixedAttrModifyEffect : GameplayDurationalEffect
    {
        [LabelText("固定修改值"), Tooltip("正数=增加，负数=减少")]
        public float fixedModify;

        [LabelText("修改属性类型"), Required]
        public EAttributeType attrType;

        private float _appliedValue;

        protected override void OnEnter()
        {
            base.OnEnter();
            // 首次进入时层数还是 0，交给 OnStackChanged；
            // 这里兜底“重新启用且已有层数”的情况。
            if (effectStack > 0 && Mathf.Approximately(_appliedValue, 0f))
                ApplyStackValue(effectStack);
        }

        protected override void OnExit()
        {
            base.OnExit();
            // 到期/被移除：把实际加过的量原样减回去。
            if (!Mathf.Approximately(_appliedValue, 0f))
            {
                target.AddAttrValue(attrType, AttributeValueType.CurValue, -_appliedValue);
                _appliedValue = 0f;
            }
        }

        protected override void OnStackChanged(int previousStack, int currentStack)
        {
            base.OnStackChanged(previousStack, currentStack);

            int delta = currentStack - previousStack;
            if (delta > 0) ApplyStackValue(delta);
            else if (delta < 0) RemoveStackValue(-delta);
        }

        protected override void ApplyEffect()
        {
            
        }

        protected override void OnCopied()
        {
            base.OnCopied();
            _appliedValue = 0f;
        }

        private void ApplyStackValue(int stack)
        {
            if (stack <= 0) return;
            float actual = target.AddAttrValue(attrType, AttributeValueType.CurValue, fixedModify * stack);
            _appliedValue += actual;
        }

        private void RemoveStackValue(int stack)
        {
            if (stack <= 0) return;
            float actual = target.AddAttrValue(attrType, AttributeValueType.CurValue, -fixedModify * stack);
            _appliedValue += actual;
        }
    }
}
