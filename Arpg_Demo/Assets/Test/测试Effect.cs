using GAS;
using UnityEngine;

public class 测试Effect : GameplayDurationalEffect
{
    public int t;

    protected override void ApplyEffect()
    {
    }

    public override void Execute(AbilitySystemComponent source, AbilitySystemComponent target, int stack = 1)
    {
        base.Execute(source, target, stack);
        Debug.Log(t);
    }
}
