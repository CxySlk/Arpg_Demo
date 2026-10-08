using System;
using Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 输入节点基类：一个"输入节点"代表一个可配置的按键/鼠标输入源。
/// 子类（键盘 <see cref="Input.InputKeyNode"/> / 鼠标 <see cref="Input.InputMouseNode"/>）实现三种触发状态的检测，
/// 基类统一管理回调、输入缓存与"是否可输入"开关。
/// </summary>
/// <remarks>
/// 由 <see cref="Input.InputController"/> 在物体上挂载并逐帧调用 <see cref="OnUpdate"/> 驱动。
/// - <see cref="inputType"/> 决定检测"按下/抬起/按住"中的哪一种；
/// - 开启缓存后，一次有效输入会保持 <see cref="cacheTime"/> 秒，供外部消费（如技能连段预输入）。
/// </remarks>
public abstract class InputNodeBase
{
    // 注意：这些字段放在 Odin 分组/ShowIf 等属性里，在该类被 [SerializeReference] 列表 + 预制体修改
    // 组合绘制时会导致 Odin 编辑器空引用崩溃，故只保留 LabelText/SerializeField 等安全属性。
    [LabelText("开启输入缓存")] [SerializeField] public bool isCache;

    [LabelText("缓存时间(秒)")] [SerializeField, ShowIf("@isCache==true")]
    public float cacheTime = .25f;

    [LabelText("触发类型")] [SerializeField] public InputType inputType;

    /// <summary>缓存倒计时（秒）：缓存期间 <see cref="HasInput"/> 保持为 true。</summary>
    protected float cacheTimer;

    /// <summary>是否允许输入（外部可关闭，如 UI 弹窗/过场时禁用输入）。</summary>
    [LabelText("是否可输入"), SerializeField] protected bool canInput = true;

    public bool CanInput
    {
        get => canInput;
        set
        {
            if (!value) hasInput = false;
            canInput = value;
        }
    }

    /// <summary>触发时执行的回调（外部通过 <see cref="AddCallBack"/> / <see cref="SetCallBack"/> 绑定）。</summary>
    [LabelText("按键点击事件"), SerializeField] protected UnityAction callBack;

    /// <summary>本轮是否产生了有效输入（缓存未过期）。</summary>
    protected bool hasInput;

    /// <summary>本轮是否产生有效输入（含缓存窗口，供外部查询/消费）。</summary>
    public virtual bool HasInput => hasInput;

    /// <summary>追加一个触发回调（可多个）。</summary>
    /// <param name="action">触发时执行的回调。</param>
    public virtual void AddCallBack(UnityAction action)
    {
        callBack += action;
    }

    /// <summary>设置触发回调（覆盖之前的回调）。</summary>
    /// <param name="action">触发时执行的回调。</param>
    public virtual void SetCallBack(UnityAction action)
    {
        callBack = action;
    }

    /// <summary>清除本轮输入缓存（外部消费完本次输入后调用，避免同一输入被重复消费）。</summary>
    public virtual void ResetCache()
    {
        hasInput = false;
    }


    /// <summary>每帧驱动入口：按触发类型分发检测，并推进缓存倒计时。</summary>
    public virtual void OnUpdate()
    {
        if (!canInput) return;
        switch (inputType)
        {
            case InputType.ClickDown:
                OnClickDownUpdate();
                break;
            case InputType.ClickUp:
                OnClickUpUpdate();
                break;
            case InputType.Click:
                OnClickedUpdate();
                break;
        }

        // 缓存输入：缓存期间倒计时，归零后清除有效输入标记
        if (isCache && hasInput && cacheTimer > 0)
        {
            cacheTimer -= Time.deltaTime;
            if (cacheTimer <= 0) hasInput = false;
        }
    }

    /// <summary>检测"按下瞬间"（子类实现）。</summary>
    protected abstract void OnClickDownUpdate();

    /// <summary>检测"抬起瞬间"（子类实现）。</summary>
    protected abstract void OnClickUpUpdate();

    /// <summary>检测"持续按住"（子类实现）。</summary>
    protected abstract void OnClickedUpdate();

    /// <summary>触发一次有效输入：执行回调并开启缓存窗口。</summary>
    protected void OnClick()
    {
        callBack?.Invoke();
        cacheTimer = cacheTime;
        hasInput = true;
    }

    /// <summary>初始化：按配置的按键类型绑定按钮控件。</summary>
    public virtual  void Init()
    {
    }
}