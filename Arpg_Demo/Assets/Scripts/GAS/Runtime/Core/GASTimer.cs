using System;

namespace GAS
{
    /// <summary>
    /// GAS 固定帧率计时器。以 60 FPS 为基准，将真实时间转换为整数帧计数，
    /// 保证游戏逻辑的确定性。支持暂停/恢复以补偿暂停时长。
    /// </summary>
    public class GASTimer
    {
        private long _startTimestamp;
        private int _deltaStamp;
        private long _pauseTimestamp;
        private int _currentFrameCount;

        /// <summary>当前毫秒时间戳（含暂停补偿）</summary>
        private long TimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _deltaStamp;
        /// <summary>当前毫秒时间戳</summary>
        public long Timestamp => TimestampMs;
        /// <summary>当前秒时间戳</summary>
        public long TimestampSeconds => TimestampMs / 1000;
        /// <summary>自初始化以来经过的秒数</summary>
        public float CurrentTimeSeconds => (TimestampMs - _startTimestamp) / 1000f;
        /// <summary>当前游戏帧计数（60 FPS）</summary>
        public int CurrentFrameCount => _currentFrameCount;
        /// <summary>帧率常量</summary>
        public const int FrameRate = 60;

        /// <summary>初始化起始时间戳和帧计数</summary>
        public void Init()
        {
            _startTimestamp = TimestampMs;
            _currentFrameCount = 0;
        }

        /// <summary>根据实时间更新当前帧计数</summary>
        public void UpdateCurrentFrameCount()
        {
            _currentFrameCount = (int)((TimestampMs - _startTimestamp) / 1000f * FrameRate);
        }

        /// <summary>记录暂停时刻</summary>
        public void Pause()
        {
            _pauseTimestamp = TimestampMs;
        }

        /// <summary>补偿暂停时长，调整 delta 偏移量</summary>
        public void UnPause()
        {
            _deltaStamp -= (int)(TimestampMs - _pauseTimestamp);
        }
    }
}
