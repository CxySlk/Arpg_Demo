using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CueEffectTest : SerializedMonoBehaviour
{
    private AbilitySystemComponent _asc;
    private GameplayEffectAsset _effectAsset;
    private GameplayCue _testCue;

    private void Setup()
    {
        Cleanup();

        var go = new GameObject("TestASC_Cue");
        _asc = go.AddComponent<AbilitySystemComponent>();
        var preset = ScriptableObject.CreateInstance<AbilitySystemComponentPreset>();
        var hpAttr = new Attribute();
        hpAttr.SetCurrentValue(100, AttributeDataType.BaseValue);
        hpAttr.SetCurrentValue(100, AttributeDataType.CurrentValue);
        SetF<HashSet<Attribute>>(preset, "_attributes", new HashSet<Attribute> { hpAttr });
        SetF<GameplayTagSet>(preset, "_tagSet", GameplayTagSet.Create());
        SetF<AbilitySystemComponentPreset>(_asc, "_preset", preset);
        _asc.InitForPreset();

        _testCue = ScriptableObject.CreateInstance<GameplayCueAnimationInstant>();

        _effectAsset = ScriptableObject.CreateInstance<GameplayEffectAsset>();
        var hpMod = CreateMod("HP", 50, 0, 0, 0, true);
        SetF<GameplayEffectAttrModifier[]>(_effectAsset, "_attrModifier", new[] { hpMod });
        SetF<EffectDurationType>(_effectAsset, "_durationType", EffectDurationType.Instance);
        SetF<GameplayCue[]>(_effectAsset, "_onExcuteCues", new GameplayCue[] { _testCue });
        SetF<GameplayCue[]>(_effectAsset, "_onAddCues", new GameplayCue[] { _testCue });
        // 不设 tag 字段，让它们保持 null（ASC 有 null 检查）
    }

    [Button("测试 Cue 在 Instant Effect 中触发")]
    private void Test_CueInInstantEffect()
    {
        Setup();
        Debug.Log($"[测试] 初始 HP Cur={GetHP()}");
        _asc.ApplyEffect(_effectAsset);
        Debug.Log($"[测试] 应用后 HP Cur={GetHP()}");
        Debug.Log("[测试] 如果看到上方有 Cue.ApplyFrom 调用日志，说明 Cue 已触发");
        Debug.Log("━━━━━━━━━━━━━━━━━━━");
    }

    [Button("测试 Cue 在 Durational Effect 中触发")]
    private void Test_CueInDurationalEffect()
    {
        Setup();
        SetF<EffectDurationType>(_effectAsset, "_durationType", EffectDurationType.Durational);
        SetF<float>(_effectAsset, "_durationSeconds", 10f);
        SetF<GameplayEffectStackData>(_effectAsset, "_stackData", MakeStackData(EffectStackAggregateType.OnlyStack, EffectStackDecreaseType.Clear, 1));

        Debug.Log($"[测试] 初始 HP Cur={GetHP()}");
        _asc.ApplyEffect(_effectAsset);
        Debug.Log($"[测试] 应用后 HP Cur={GetHP()}");
        Debug.Log("[测试] OnAddCues 应该在应用时触发");
        Debug.Log("━━━━━━━━━━━━━━━━━━━");
    }

    [Button("测试 直接 ApplyFrom")]
    private void Test_CueDirectApply()
    {
        Setup();
        Debug.Log("[测试] 直接调用 Cue.ApplyFrom");
        _testCue.ApplyFrom(_asc);
        Debug.Log("[测试] ApplyFrom 完成");
        Debug.Log("━━━━━━━━━━━━━━━━━━━");
    }

    private float GetHP()
    {
        var fi = typeof(AbilitySystemComponent).GetField("_attrDic", BindingFlags.NonPublic | BindingFlags.Instance);
        var attrSet = fi.GetValue(_asc) as AttributeSet;
        var dic = attrSet.AttrDic;
        foreach (var kv in dic)
            return kv.Value.CurValue.CurValue;
        return -1;
    }

    private static GameplayEffectAttrModifier CreateMod(string attrName, float val, int modifyType, int sourceType, int targetType, bool restore)
    {
        var mod = GameplayEffectAttrModifier.Create();
        var hash = typeof(GameplayEffectAttrModifier).GetField("_attrHashCode", BindingFlags.NonPublic | BindingFlags.Instance);
        hash.SetValue(mod, attrName.GetHashCode());
        var name = typeof(GameplayEffectAttrModifier).GetField("_attrName", BindingFlags.NonPublic | BindingFlags.Instance);
        name.SetValue(mod, attrName);
        typeof(GameplayEffectAttrModifier).GetField("_modifyType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mod, (CalculateType)modifyType);
        typeof(GameplayEffectAttrModifier).GetField("_modifierSource", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mod, (CalculateSource)sourceType);
        typeof(GameplayEffectAttrModifier).GetField("_modifierTarget", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mod, (AttributeModifyTarget)targetType);
        typeof(GameplayEffectAttrModifier).GetField("_modifierValue", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mod, val);
        typeof(GameplayEffectAttrModifier).GetField("_experiedAutoRestore", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mod, restore);
        mod.RealModifyValue = 0;
        return mod;
    }

    private static GameplayEffectStackData MakeStackData(EffectStackAggregateType agg, EffectStackDecreaseType dec, int max)
    {
        var sd = GameplayEffectStackData.Create();
        sd.Init(agg, dec, max);
        return sd;
    }

    private static void SetF<T>(object obj, string field, T val)
    {
        var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, val);
    }

    [Button("清理")]
    private void Cleanup()
    {
        var go = GameObject.Find("TestASC_Cue");
        if (go != null) DestroyImmediate(go);
        Debug.Log("清理完成");
    }
}
