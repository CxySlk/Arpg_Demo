using System.Collections.Generic;
using LLKFrame;
using MessagePack;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 属性集合。以属性类型为键的字典，存储角色所有属性。
    /// 支持对象池回收。
    /// </summary>
    public class AttributeDic
    {
        public Dictionary<AttributeType, Attribute> attrDic = new();
        public bool Empty => attrDic.Count == 0;

        /// <summary>根据属性类型检查是否存在</summary>
        public bool HasAttr(AttributeType attrType) => attrDic.ContainsKey(attrType);
        /// <summary>尝试获取属性</summary>
        public bool TryGetAttrValue(AttributeType attrType, out Attribute attr) => attrDic.TryGetValue(attrType, out attr);
        /// <summary>设置属性值</summary>
        public void SetAttrValue(AttributeType attrType, AttributeValueType valueType, float attrValue)
        {
            if (attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.SetCurrentValue(attrValue);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.SetMinValue(attrValue);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.SetMaxValue(attrValue);
                        break;
                }
            }
        }
        /// <summary>属性值增加</summary>
        public void AddAttrValue(AttributeType attrType, AttributeValueType valueType, float addValue)
        {
            if (attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.AddCurrentValue(addValue);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.AddMinValue(addValue);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.AddMaxValue(addValue);
                        break;
                }
            }
        }
    }
}
