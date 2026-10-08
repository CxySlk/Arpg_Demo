using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("概率判定")]
[Description("按设定概率掷骰:命中返回 Success,没命中返回 Failure。用来做“有时候放技能、有时候不放”这类随机决策")]
public class Enemy_CheckChance : ConditionTask
{
    [LabelText("成功率(0~1)"), Tooltip("0 = 永远不通过,1 = 永远通过"), Range(0f, 1f)]
    public BBParameter<float> chance = new(0.6f);

    protected override bool OnCheck()
    {
        return Random.value <= Mathf.Clamp01(chance.value);
    }
}
