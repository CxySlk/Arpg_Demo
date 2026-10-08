using System;
using Animancer;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        // animancer = GetComponent<AnimancerComponent>();
        // animancer.Play(clip);
    }

    // private void Update()
    // {
    //     Vector2MixerState state = animancer.Play(clip, .25f) as Vector2MixerState;
    //     // 设置参数x、y
    //     state.ParameterX = x;
    //     state.ParameterY = y;
    // }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log(other.name);
    }
}