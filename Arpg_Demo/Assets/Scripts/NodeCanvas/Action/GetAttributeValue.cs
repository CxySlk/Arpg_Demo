using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Attribute = GAS.Attribute;

public class GetAttributeValue : ActionTask
{
    [Name("属性类型")] public BBParameter<AttributeType> attrType;
    [Name("属性值类型")] public AttributeValueType valueType = AttributeValueType.CurValue;
    [Name("返回结果")] public BBParameter<float> result;

    private AbilitySystemComponent _asc;

    protected override string OnInit()
    {
        _asc = agent.GetComponentInChildren<AbilitySystemComponent>();
        return null;
    }

    protected override void OnExecute()
    {
        _asc.TryGetAttrValue(attrType.value, out Attribute attribute);
        switch (valueType)
        {
            case AttributeValueType.CurValue:
                result.SetValue(attribute.CurValue);
                break;
            case AttributeValueType.MinValue:
                result.SetValue(attribute.MinValue);
                break;
            case AttributeValueType.MaxValue:
                result.SetValue(attribute.MaxValue);
                break;
            case AttributeValueType.NormalizeValue:
                result.SetValue(attribute.NormalizeValue);
                break;
        }

        EndAction();
    }
}