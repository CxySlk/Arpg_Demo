using Sirenix.OdinInspector;

namespace GAS
{
    /// <summary>
    /// 持续回血效果：每个周期结算一次，把 回血速度 × 周期秒数 的属性值加回去。
    /// </summary>
    public class DurationalHealEffect : GameplayDurationalEffect
    {
        [BoxGroup("回血配置"), LabelText("回血速度")]
        public float healSpeed;

        [BoxGroup("回血配置"), LabelText("恢复属性类型")]
        public AttributeType healAttrTag;

        private Attribute _attr;

        protected override void Init(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            base.Init(source, target);
            target.TryGetAttrValue(healAttrTag, out _attr);
        }

        protected override void ApplyEffect() =>
            _attr?.AddCurrentValue(healSpeed * periodicSeconds);

        protected override void OnCopied()
        {
            _attr = null;
        }
    }
}
