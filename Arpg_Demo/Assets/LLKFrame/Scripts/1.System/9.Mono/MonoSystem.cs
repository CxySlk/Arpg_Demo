using LLKFrame;
using System;
using System.Collections;
using UnityEngine;

public class MonoSystem : MonoBehaviour
{
    private static MonoSystem instance;

    private static Action onUpdate;
    private static Action onFixedUpdate;
    private static Action onLateUpdate;

    public static void Init() => instance = GameRoot.Instance.GetComponentInChildren<MonoSystem>();


    private void Update() => onUpdate?.Invoke();
    private void FixedUpdate() => onFixedUpdate?.Invoke();
    private void LateUpdate() => onLateUpdate?.Invoke();

    public static void AddUpdate(Action action) => onUpdate += action;
    public static void AddFixedUpdate(Action action) => onFixedUpdate += action;
    public static void AddLateUpdate(Action action) => onLateUpdate += action;
    public static Coroutine Start_Coroutine(IEnumerator routine) => instance.StartCoroutine(routine);
    public static void Stop_Coroutine(Coroutine coroutine) => instance.StopCoroutine(coroutine);
}
