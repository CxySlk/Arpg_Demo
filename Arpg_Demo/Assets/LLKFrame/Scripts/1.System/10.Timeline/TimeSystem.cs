using System;
using System.Collections;
using UnityEngine;

namespace LLKFrame
{
    public class TimeSystem
    {
        public static readonly TimeSystem Global = new ();

        private readonly long _startTimestamp;
        private int _deltaStamp;
        private long _pauseTimestamp;
        private int _curFrame;
        private bool _paused;
        /// <summary>暂停引用计数：可嵌套，只有归零才真正恢复</summary>
        private int _pauseCount;
        private Action<int> _onCurFrameChanged;
        private Action<bool> _onTimeRunningStateChanged;

        /// <summary>当前毫秒时间戳（含暂停补偿）</summary>
        private long TimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _deltaStamp;

        /// <summary>自初始化以来经过的秒数</summary>
        public float CurrentTimeSeconds => (TimestampMs - _startTimestamp) / 1000f;

        /// <summary>当前游戏帧计数（60 FPS）</summary>
        public int CurFrame => _curFrame;

        public int NextFrame => _curFrame + 1;
        /// <summary>帧率常量</summary>
        public const int FrameRate = 30;

        public static readonly float FrameTime = 1f / FrameRate * Time.timeScale;

        /// <summary>初始化起始时间戳和帧计数</summary>
        public TimeSystem()
        {
            _startTimestamp = TimestampMs;
            _curFrame = -1;
            MonoSystem.AddUpdate(Update);
        }

        /// <summary>根据实时间更新当前帧计数</summary>
        private void Update()
        {
            int totalFrame = (int)(CurrentTimeSeconds * FrameRate);
            if (totalFrame == _curFrame) return;
            while (_curFrame < totalFrame)
            {
                _curFrame++;
                Publish(_curFrame);
            }
        }

        #region 暂停、恢复

        private void StartTimer()
        {
            if (!_paused) return;      // 已经在跑，别重复订阅
            MonoSystem.AddUpdate(Update);
            _paused = false;
        }

        private void StopTimer()
        {
            if (_paused) return;       // 已经停了
            MonoSystem.RemoveUpdate(Update);
            _paused = true;
        }

        private float timeScale = 1f;

        /// <summary>暂停（引用计数：可嵌套，计数归零才真正恢复）</summary>
        public void Pause()
        {
            _pauseCount++;
            if (_paused) return;                 // 实际已冻结：只记数，避免覆盖 timeScale
            timeScale = Time.timeScale;
            Time.timeScale = 0;
            _pauseTimestamp = TimestampMs;
            StopTimer();                         // 摘掉 Update → 帧真正冻结
            _onTimeRunningStateChanged?.Invoke(false);
        }

        /// <summary>恢复（引用计数：计数归零才真正恢复）</summary>
        public void UnPause()
        {
            if (_pauseCount <= 0) return;
            _pauseCount--;
            if (_pauseCount > 0) return;         // 还有别的暂停源
            if (!_paused) return;
            Time.timeScale = timeScale;
            _deltaStamp -= (int)(TimestampMs - _pauseTimestamp);
            StartTimer();
            _onTimeRunningStateChanged?.Invoke(true);
        }

        /// <summary>强制暂停全部：不论当前状态/计数，直接冻结，并把暂停计数归零</summary>
        public void PauseAll()
        {
            _pauseCount = 0;
            if (_paused) return;
            timeScale = Time.timeScale;
            Time.timeScale = 0;
            _pauseTimestamp = TimestampMs;
            StopTimer();
            _onTimeRunningStateChanged?.Invoke(false);
        }

        /// <summary>强制恢复全部：不论当前状态/计数，直接恢复，并把暂停计数归零</summary>
        public void ResumeAll()
        {
            _pauseCount = 0;
            if (!_paused) return;
            Time.timeScale = timeScale;
            _deltaStamp -= (int)(TimestampMs - _pauseTimestamp);
            StartTimer();
            _onTimeRunningStateChanged?.Invoke(true);
        }

        public void SubscribeTimeRunningState(Action<bool> action) => _onTimeRunningStateChanged += action;

        public void UnSubscribeTimeRunningState(Action<bool> action) => _onTimeRunningStateChanged -= action;

        #endregion

        #region 监听帧数变化

        /// <summary>
        /// 订阅帧数变化事件
        /// </summary>
        public void Subscribe(Action<int> action) => _onCurFrameChanged += action;

        /// <summary>
        /// 取消订阅帧数变化事件
        /// </summary>
        public void UnSubscribe(Action<int> action) => _onCurFrameChanged -= action;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        private void Publish(int frame) => _onCurFrameChanged?.Invoke(frame);

        #endregion

        private static Coroutine _freezeRoutine;

        /// <summary>顿帧：把 timeScale 从 startTimeScale 拉回 1。重复触发会停掉上一个再开新的（硬切重来）。</summary>
        public static void FreezeFrame(float startTimeScale, float duration)
        {
            // 上一个顿帧还没结束 → 停掉，开新的
            if (_freezeRoutine != null)
                MonoSystem.Stop_Coroutine(_freezeRoutine);

            _freezeRoutine = MonoSystem.Start_Coroutine(FreezeRoutine(startTimeScale, duration));
        }

        private static IEnumerator FreezeRoutine(float startTimeScale, float duration)
        {
            if (!Global._paused)
                Time.timeScale = startTimeScale;

            float t = 0f;
            while (t < duration)
            {
                // 暂停期间不推进、不写 timeScale；恢复后继续
                if (!Global._paused)
                {
                    t += Time.unscaledDeltaTime;                 // 真实时间，时长准确
                    Time.timeScale = Mathf.Lerp(startTimeScale, 1f, t / duration);
                }
                yield return null;
            }

            if (!Global._paused)
                Time.timeScale = 1f;

            _freezeRoutine = null;
        }
    }
}