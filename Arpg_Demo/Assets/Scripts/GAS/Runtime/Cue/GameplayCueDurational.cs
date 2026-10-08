using Sirenix.OdinInspector;

namespace GAS
{
    /// <summary>
    /// GameplayCueDurational —— 持续类 Cue 模板基类。DurationSeconds 配置持续时长（秒）。
    /// </summary>
    [System.Serializable]
    public abstract class GameplayCueDurational : GameplayCue
    {
        [LabelText("持续时长(秒)")]
        public float DurationSeconds;

        public abstract GameplayCueDurational DeepCopy();

#if UNITY_EDITOR
        /// <summary>按素材长度自动算出持续时长（秒）</summary>
        public abstract void CalculateDuration();
#endif
    }
}
