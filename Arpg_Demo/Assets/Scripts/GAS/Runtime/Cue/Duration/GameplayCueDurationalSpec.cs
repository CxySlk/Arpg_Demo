using System;

namespace GAS
{
    public abstract class GameplayCueDurationalSpec : GameplayCueSpec<GameplayCueDurational>
    {
        protected int _startFrame;

        /// <summary>开启Tick</summary>
        public void StartTick(int startFrame)
        {
            _startFrame = startFrame;
            // Tick第0帧
            Tick(startFrame);
        }

        /// <summary>Tick 里面的curFrame是GASTimer里的帧数，不是从0开始</summary>
        public virtual void Tick(int curFrame) => OnTick(curFrame - _startFrame);

        /// <summary>里面的curFrame是真实的帧数，从0开始</summary>
        protected abstract void OnTick(int curFrame);

        public abstract void TriggerAdd();
    }

    public abstract class GameplayCueDurationalSpec<TCue> : GameplayCueDurationalSpec where TCue : GameplayCueDurational
    {
        protected TCue InstantCue => Cue as TCue;
    }
}
