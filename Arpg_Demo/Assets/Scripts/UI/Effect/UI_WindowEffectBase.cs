using System;
using DG.Tweening;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// UI 窗口显隐表现的公共基类：统一时长字段与"重复播放先 Kill 旧补间"。
    /// 具体效果挂在窗口根物体上，由 <see cref="UI_WindowBase"/> 在 Show/Close 时自动驱动。
    /// </summary>
    /// <remarks>
    /// 子类实现 <see cref="PlayEnter"/> / <see cref="PlayExit"/>；<see cref="PlayExit"/> 必须在补间播完时
    /// 回调一次 <c>onComplete</c>（接口约定，基类据此决定何时隐藏窗口）。
    /// 补间一律用 <c>SetUpdate(true)</c>：窗口需要忽略 <see cref="Time.timeScale"/>（例如打开背包时会暂停游戏时间）。
    /// </remarks>
    public abstract class UI_WindowEffectBase : MonoBehaviour, IUIWindowEffect
    {
        [LabelText("时长(秒)"), SerializeField] protected float _duration = .3f;

        /// <summary>当前补间（重复播放/失活时先 Kill，避免叠加）</summary>
        protected Tween _tween;

        public abstract void PlayEnter();

        public abstract void PlayExit(Action onComplete);

        /// <summary>窗口隐藏时一并停掉还在播的补间</summary>
        protected virtual void OnDisable() => _tween?.Kill();
    }
}
