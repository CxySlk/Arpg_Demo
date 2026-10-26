using System;

namespace LLKFrame
{
    /// <summary>
    /// UI 窗口显隐表现接口：把效果组件挂到窗口根物体上即可，<see cref="UI_WindowBase"/> 在初始化时自动收集，
    /// 显示时驱动弹入（<see cref="PlayEnter"/>）、关闭时驱动弹出（<see cref="PlayExit"/>）。
    /// </summary>
    /// <remarks>
    /// - 只在窗口**自身物体**上查找，不查找子物体/父物体。<br/>
    /// - 同一窗口可以挂多个效果（例如缩放 + 透明度），全部都会被应用，窗口等最后一个播完才隐藏。
    /// </remarks>
    public interface IUIWindowEffect
    {
        /// <summary>弹入（窗口显示时由 <see cref="UI_WindowBase"/> 调用）。</summary>
        void PlayEnter();

        /// <summary>弹出（窗口关闭时由 <see cref="UI_WindowBase"/> 调用）。</summary>
        /// <param name="onComplete">
        /// 本次表现播放完毕的回调。**必须且只能调用一次**：基类靠它统计"所有效果是否都播完"，
        /// 全部播完后才隐藏窗口；不回调窗口将永远不隐藏。
        /// </param>
        void PlayExit(Action onComplete);
    }
}
