using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

public class Test : MonoBehaviour
{
    public TransitionAsset sequence ;
    private AnimancerComponent animancer;

    private void Start()
    {
        animancer= GetComponent<AnimancerComponent>();
    }

    [Button("顺序播放动画")]
    private void TestA()
    {
        animancer.Play(sequence,.25f,FadeMode.FromStart);
    }
}