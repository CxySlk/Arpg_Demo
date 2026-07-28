using GAS;
using Sirenix.OdinInspector;
using UnityEngine;

public class AttributeTest : SerializedMonoBehaviour
{
    private Attribute _attr;

    [Button("测试 AddCurrentValue(BaseValue)")]
    private void Test_AddCurrentValue_BaseValue()
    {
        _attr = new Attribute();
        _attr.SetCurrentValue(100, AttributeDataType.BaseValue);
        _attr.SetCurrentValue(100, AttributeDataType.CurrentValue);
        Debug.Log($"初始 — Base:{_attr.CurValue.BaseValue}  Cur:{_attr.CurValue.CurValue}");

        _attr.AddCurrentValue(50, AttributeDataType.BaseValue);
        Debug.Log($"+50 Base — Base:{_attr.CurValue.BaseValue}  Cur:{_attr.CurValue.CurValue}");

        bool pass = Mathf.Approximately(_attr.CurValue.BaseValue, 150)
                 && Mathf.Approximately(_attr.CurValue.CurValue, 150);
        Debug.Log(pass ? "✓ Base 和 Cur 同步增加" : "✗ Cur 没有同步变化");
    }

    [Button("测试 SetCurrentValue(BaseValue)")]
    private void Test_SetCurrentValue_BaseValue()
    {
        _attr = new Attribute();
        _attr.SetCurrentValue(100, AttributeDataType.BaseValue);
        _attr.SetCurrentValue(100, AttributeDataType.CurrentValue);
        Debug.Log($"初始 — Base:{_attr.CurValue.BaseValue}  Cur:{_attr.CurValue.CurValue}");

        _attr.SetCurrentValue(200, AttributeDataType.BaseValue);
        Debug.Log($"Set Base=200 — Base:{_attr.CurValue.BaseValue}  Cur:{_attr.CurValue.CurValue}");

        bool pass = Mathf.Approximately(_attr.CurValue.BaseValue, 200)
                 && Mathf.Approximately(_attr.CurValue.CurValue, 200);
        Debug.Log(pass ? "✓ Base 和 Cur 同步" : "✗ Cur 仍然=100，Base 改后 Cur 未同步");
    }
}
