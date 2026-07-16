using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using static LLKFrame.LogSystem;
using static UnityEditor.PlayerSettings;
#endif

namespace LLKFrame
{
    /// <summary>
    /// 音频系统单例，管理背景音乐循环播放和特效音的对象池复用与自动回收
    /// </summary>
    public class AudioSystem : SerializedMonoBehaviour
    {
        private static AudioSystem instance;
        [SerializeField] private Transform audioSystemRoot;
        public static Transform AudioSystemRoot => instance.audioSystemRoot;
        #region 组件、预制体相关
        [LabelText("背景音乐播放器"), SerializeField] private AudioSource bgMusicSource;
        private static AudioSource BgMusicSource => instance.bgMusicSource;
        [LabelText("特效音乐预制体"), SerializeField] private GameObject effMusicPrefab;
        public static GameObject EffMusicPrefab => instance.effMusicPrefab;
        #endregion
        #region 音量相关
        [LabelText("背景音乐音量"), SerializeField, Range(0, 1), OnValueChanged("BgMusicVolumeChanged")]
        private float bgMusicVolume = .5f;
        public static float BgMusicVolume => instance.bgMusicVolume;
        private static float BgMusicTotalVolume => instance.bgMusicVolume * instance.globalVolume;
        
        [LabelText("特效音乐音量"), SerializeField, Range(0, 1), OnValueChanged("EffMusicVolumeChanged")]
        private float effMusicVolume = .5f;
        public static float EffMusicVolume => instance.effMusicVolume;
        private static float EffMusicTotalVolume => instance.effMusicVolume * instance.globalVolume;
        
        [LabelText("全局音乐音量"), SerializeField, Range(0, 1), OnValueChanged("GlobalVolumeChanged")]
        private float globalVolume = 1;
        public static float GlobalMusicVolume => instance.globalVolume;
      
        [LabelText("全局AudioClip配置"), SerializeField]
        private AudioConfig globalAudioConfig;
        public static AudioConfig GlobalAudioConfig => instance.globalAudioConfig;
#if UNITY_EDITOR
        private void BgMusicVolumeChanged() => SetBgMusicVolume(bgMusicVolume);
        private void EffMusicVolumeChanged() => SetEffMusicVolume(effMusicVolume);
        private void GlobalVolumeChanged() => SetGlobalVolume(globalVolume);
#endif
        #endregion

        private static HashSet<AudioSource> effAudioHashSet = new HashSet<AudioSource>();

        /// <summary>初始化音频系统，查找场景中的 AudioSystem 实例并设置背景音乐初始音量</summary>
        public static void Init()
        {
            instance = GameRoot.Instance.GetComponentInChildren<AudioSystem>();
            effAudioHashSet = new HashSet<AudioSource>();
        }

        #region 音量设置
        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">背景音乐总的音量还需乘上全局音量</param>
        public static void SetBgMusicVolume(float volume)
        {
            instance.bgMusicVolume = volume;
            BgMusicSource.volume = BgMusicTotalVolume;
        }
        /// <summary>
        /// 设置特效音乐音量
        /// </summary>
        /// <param name="volume">特效音乐总的音量还需乘上全局音量</param>
        public static void SetEffMusicVolume(float volume)
        {
            instance.effMusicVolume = volume;
            // 设置特效音量
            if (effAudioHashSet.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                foreach (AudioSource audioSource in effAudioHashSet)
                    audioSource.volume = totalVolume;
            }
        }
        /// <summary>设置全局音量，同时影响 BGM 和 SFX</summary>
        public static void SetGlobalVolume(float volume)
        {
            instance.globalVolume = volume;
            // 设置背景音乐音量
            BgMusicSource.volume = BgMusicTotalVolume;
            // 设置特效音量
            if (effAudioHashSet.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                foreach (AudioSource audioSource in effAudioHashSet)
                    audioSource.volume = totalVolume;
            }
        }
        #endregion
        #region 音乐播放
        /// <summary>
        /// 播放全局音效配置里的音效
        /// </summary>
        /// <param name="keyName"></param>
        public static void PlayEffectMusic(string keyName, float volumeMultiplier = 1)
        {
#if UNITY_EDITOR
            if (GlobalAudioConfig == null)
            {
                LogError("LLKFrame：全局Audio配置不能为空");
                return;
            }
#endif
            if (!GlobalAudioConfig.ConfigDic.TryGetValue(keyName, out AudioClip audioClip)) return;
            AudioSource audioSource = PoolSystem.GetGameObject<AudioSource>(EffMusicPrefab,AudioSystemRoot);
            audioSource.spatialBlend = 0;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            effAudioHashSet.Add(audioSource);
            // 自动回收到对象池
            instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }

        /// <summary>播放背景音乐，替换当前正在播放的片段</summary>
        public static void PlayBgMusic(AudioClip audioClip, float volumeMultiplier = 1)
        {
            BgMusicSource.clip = audioClip;
            BgMusicSource.volume = BgMusicTotalVolume * volumeMultiplier;
            BgMusicSource.Play();
        }
        /// <summary>恢复已暂停的背景音乐</summary>
        public static void ResumeBgMusic()
        {
            if (!BgMusicSource.isPlaying && BgMusicSource.clip != null)
                BgMusicSource.UnPause();
        }
        /// <summary>暂停当前背景音乐</summary>
        public static void PauseBgMusic()
        {
            if (BgMusicSource.isPlaying)
                BgMusicSource.Pause();
        }
        /// <summary>开始播放背景音乐（未播放且 clip 不为空时才播放）</summary>
        public static void StartBgMusic()
        {
            if (!BgMusicSource.isPlaying && BgMusicSource.clip != null)
                BgMusicSource.Play();
        }
        /// <summary>停止背景音乐</summary>
        public static void StopBgMusic()
        {
            if (BgMusicSource.isPlaying)
                BgMusicSource.Stop();
        }
        /// <summary>在指定位置播放特效音，支持 3D 空间音效，播放完后自动回收 AudioSource 到对象池</summary>
        public static void PlayEffectMusic(AudioClip audioClip, Vector3 pos, float volumeMultiplier = 1, bool is3D = true, bool autoDestroy = true)
        {
            AudioSource audioSource = PoolSystem.GetGameObject<AudioSource>(EffMusicPrefab, AudioSystemRoot);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.transform.position = pos;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            effAudioHashSet.Add(audioSource);
            // 自动回收到对象池
            instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }

        public static void PlayEffectMusicByRealVolume(AudioClip audioClip, float realVolume)
        {
            AudioSource audioSource = PoolSystem.GetGameObject<AudioSource>(EffMusicPrefab, AudioSystemRoot);
            audioSource.spatialBlend = 0;
            audioSource.clip = audioClip;
            audioSource.volume = realVolume;
            audioSource.Play();
            // 自动回收到对象池
            instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }



        /// <summary>等待音频播放完毕后将 AudioSource 回收到对象池</summary>
        private static IEnumerator RecyclyAudioGameObject(AudioSource audioSource, AudioClip clip)
        {
            yield return new WaitForSeconds(clip.length);
            if (audioSource == null) yield break;
            audioSource.PushGameObject();
            effAudioHashSet.Remove(audioSource);
        }
        #endregion
    }
}