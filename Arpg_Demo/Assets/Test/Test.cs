using GAS;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class Test : SerializedMonoBehaviour
{
    [SerializeField]
    public GameplayCueAnimationInstant effect;

    [Button("播放动画")]
    private void ApplyEffect()
    {
        effect.ApplyFrom(GetComponent<AbilitySystemComponent>());
    }
}
