using LLKFrame;
using System;
using System.Collections;
using UnityEngine;

public class MonoSystem : MonoBehaviour
{
    private static MonoSystem _instance;

    private static Action _onUpdate;
    private static Action _onFixedUpdate;
    private static Action _onLateUpdate;

    public static void Init() => _instance = GameRoot.Instance.GetComponentInChildren<MonoSystem>();


    private void Update() => _onUpdate?.Invoke();
    private void FixedUpdate() => _onFixedUpdate?.Invoke();
    private void LateUpdate() => _onLateUpdate?.Invoke();

    public static void AddUpdate(Action action) => _onUpdate += action;
    public static void AddFixedUpdate(Action action) => _onFixedUpdate += action;
    public static void AddLateUpdate(Action action) => _onLateUpdate += action;
    public static void RemoveUpdate(Action action) => _onUpdate -= action;
    public static void RemoveFixedUpdate(Action action) => _onFixedUpdate -= action;
    public static void RemoveLateUpdate(Action action) => _onLateUpdate -= action;
    public static Coroutine Start_Coroutine(IEnumerator routine)
    {
#if UNITY_EDITOR
        // 编辑器模式（未进入 Play）下没有 GameRoot 初始化的 MonoSystem 实例，
        // 走编辑器协程宿主，避免 NullReferenceException
        if (_instance == null)
            return Start_Coroutine_InEditor(routine);
#endif
        return _instance.StartCoroutine(routine);
    }
    public static void Stop_Coroutine(Coroutine coroutine)
    {
#if UNITY_EDITOR
        if (_instance == null)
        {
            if (_editorInstance != null) _editorInstance.StopCoroutine(coroutine);
            return;
        }
#endif
        _instance.StopCoroutine(coroutine);
    }

#if UNITY_EDITOR
    private static MonoSystem _editorInstance;
    private static int _editorInstanceRefCount;

    /// <summary>
    /// 编辑器模式（非运行时）下的协程宿主。
    /// 不依赖 GameRoot/场景中的 MonoSystem 实例，
    /// 创建一个隐藏的临时 GameObject 挂载本组件跑协程（如技能编辑器预览中的延迟回池）。
    /// </summary>
    public static Coroutine Start_Coroutine_InEditor(IEnumerator routine)
    {
        if (_editorInstance == null)
        {
            GameObject go = new GameObject("MonoSystem_Editor")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _editorInstance = go.AddComponent<MonoSystem>();
        }
        _editorInstanceRefCount++;
        return _editorInstance.StartCoroutine(WrapRoutine(routine));
    }

    private static IEnumerator WrapRoutine(IEnumerator routine)
    {
        while (routine.MoveNext())
            yield return routine.Current;
        _editorInstanceRefCount--;
        // 没有协程在跑时回收编辑器宿主
        if (_editorInstanceRefCount <= 0 && _editorInstance != null)
        {
            UnityEngine.Object.DestroyImmediate(_editorInstance.gameObject);
            _editorInstance = null;
        }
    }
#endif
}
