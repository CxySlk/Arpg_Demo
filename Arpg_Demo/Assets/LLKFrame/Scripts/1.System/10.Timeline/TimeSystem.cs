using System;
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

        public static readonly float FrameTime = 1f / FrameRate;

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

        /// <summary>暂停</summary>
        public void Pause()
        {
            if (_paused) return;                 // 幂等：避免二次 Pause 把 timeScale 存成 0
            timeScale = Time.timeScale;
            Time.timeScale = 0;
            _pauseTimestamp = TimestampMs;
            StopTimer();                         // 摘掉 Update → 帧真正冻结
            _onTimeRunningStateChanged?.Invoke(false);
        }

        /// <summary>补偿暂停时长，调整 delta 偏移量</summary>
        public void UnPause()
        {
            if (!_paused) return;                // 幂等
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
    }
}