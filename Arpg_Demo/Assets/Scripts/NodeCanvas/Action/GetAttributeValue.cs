using cfg;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using Attribute = GAS.Attribute;

public class GetAttributeValue : ActionTask
{
    [Name("属性类型")] public BBParameter<EAttributeType> attrType;
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
        // ASC 里没有这个属性时（角色没初始化 / 属性表缺行）attribute 会是 null，必须判返回值
        if (!_asc.TryGetAttrValue(attrType.value, out Attribute attribute))
        {
            Debug.LogError($"[GetAttributeValue] {agent.name} 的 ASC 里没有属性 {attrType.value}");
            EndAction();
            return;
        }

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