using LLKFrame;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GameplayCueDurationalSpec —— 持续类 Cue 的 Spec 基类。
    /// 由 TimeSystem 的帧回调当"心跳"，时长按**秒**判断（与持续效果一致），到期自动回收。
    /// </summary>
    public abstract class GameplayCueDurationalSpec : GameplayCueSpec
    {
        /// <summary>本段 Cue 的开始时间（秒）</summary>
        protected float startTime;

        protected GameplayCueDurational DurationCue => cue as GameplayCueDurational;

        protected abstract void StartTick(float startTime);

        protected abstract void Tick(int curFrame);

        public abstract void DisApply();
    }

    /// <summary>
    /// 泛型持续 Spec 基类，提供类型化的模板访问。
    /// </summary>
    public abstract class GameplayCueDurationalSpec<TCue> : GameplayCueDurationalSpec
        where TCue : GameplayCueDurational
    {
        protected TCue DurationalCue => cue as TCue;

        public override void Apply() => StartTick(Time.time);

        protected override void StartTick(float startTime)
        {
            this.startTime = startTime;
            TimeSystem.Global.Subscribe(Tick);
        }

        protected override void Tick(int curFrame)
        {
            // 暂停/时间缩放由 Time.timeScale 统一处理，所以暂停时 Time.time 不推进
            if (Time.time - startTime >= DurationalCue.DurationSeconds)
                DisApply();
        }

        public override void DisApply()
        {
            TimeSystem.Global.UnSubscribe(Tick);
            Destroy();
        }
    }
}
