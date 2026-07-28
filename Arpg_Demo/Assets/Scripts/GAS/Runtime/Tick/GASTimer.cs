using System;
using UnityEngine;

namespace GAS
{
    public static class GASTimer
    {
        /// <summary>毫秒级时间戳</summary>
        private static long _timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _deltaStamp;
        public static long Timestamp => _timestamp;
        /// <summary>秒级时间戳</summary>
        private static long _timestampSeconds => _timestamp / 1000;
        public static long TimestampSeconds => _timestampSeconds;
        /// <summary>当前流逝秒数（从初始化开始，浮点数）</summary>
        public static float CurrentTimeSeconds => (_timestamp - _startTimestamp) / 1000f;
        /// <summary>时间戳偏移值</summary>
        private static int _deltaStamp = 0;
        /// <summary>起始时间戳</summary>
        private static long _startTimestamp = 0;
        /// <summary>当前的帧数</summary>
        private static int _currentFrameCount;
        public static int CurrentFrameCount => _currentFrameCount;
        /// <summary>暂停时的时间戳</summary>
        private static long _pauseTimestamp;
        /// <summary>帧率</summary>
        public const int FrameRate = 60;

        /// <summary>初始化起始时间戳</summary>
        public static void InitStartTimestamp() => _startTimestamp = _timestamp;

        /// <summary>更新当前的帧数</summary>
        public static void UpdateCurrentFrameCount() => _currentFrameCount = Mathf.FloorToInt(((_timestamp - _startTimestamp) / 1000f * FrameRate));
        /// <summary>暂停计时</summary>
        public static void Pause() => _pauseTimestamp = _timestamp;
        /// <summary>恢复计时</summary>
        public static void UnPause() => _deltaStamp -= (int)(_timestamp - _pauseTimestamp);
    }
}