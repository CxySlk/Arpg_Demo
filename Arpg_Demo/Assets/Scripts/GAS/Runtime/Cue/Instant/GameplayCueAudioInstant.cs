using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 瞬时音效 Cue 资产。配置音频片段、音量倍率和 2D/3D 空间设置。
    /// 触发时通过 AudioSystem.PlayEffectMusic 播放，自动处理对象池和回收。
    /// </summary>
    [CreateAssetMenu(fileName = "Cue_Audio", menuName = "GAS/即时音效Cue")]
    public class GameplayCueAudioInstant : GameplayCueInstant
    {
        [LabelText("音频片段"), SerializeField]
        private AudioClip _audioClip;
        public AudioClip AudioClip => _audioClip;

        [LabelText("音量倍率(0~1)"), SerializeField, Range(0, 1)]
        private float _volumeMultiplier = 1f;
        public float VolumeMultiplier => _volumeMultiplier;

        [LabelText("3D空间音效"), SerializeField]
        private bool _is3D = true;
        public bool Is3D => _is3D;

        public override GameplayCueInstantSpec CreateSpec() => PoolSystem.GetOrNewObject<GameplayCueAudioInstantSpec>();
    }
}
