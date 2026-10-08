using Character;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

public class GetAttributeValue : ActionTask
{
    [Name("属性类型")] public BBParameter<AttributeType> attrType;
    [Name("返回结果")] public BBParameter<float> result;
    [Name("角色控制器")]public BBParameter<CharacterControllerBase>characterController;

    protected override void OnExecute()
    {
        characterController.value.Asc.TryGetAttrValue(attrType.value, out Attribute attribute);
        result.SetValue(attribute.CurValue);
        EndAction();
    }
}