using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class UI_AttrPart : MonoBehaviour
{
    [LabelText("属性名Text"),Required("Text组件不能为空")]
    public TMP_Text txtAttrName;
    [LabelText("属性值Text"), Required("Text组件不能为空")]
    public TMP_Text txtAttrValue;

    public void SetInfo(string attrName, string attrValue)
    {
        txtAttrName.text = attrName;
        txtAttrValue.text = attrValue;
    }
}
