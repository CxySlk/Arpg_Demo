using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

namespace LLKFrame
{
    /// <summary>
    /// 音频系统单例，管理背景音乐循环播放和特效音的对象池复用与自动回收
    /// </summary>
    public class AudioSystem : SerializedMonoBehaviour
    {
        private static AudioSystem _instance;
        [SerializeField] private Transform _audioSystemRoot;
        private static Transform AudioSystemRoot => _instance._audioSystemRoot;

        #region 组件、预制体相关

        [LabelText("背景音乐播放器"), SerializeField] private AudioSource _bgMusicSource;
        private static AudioSource BgMusicSource => _instance._bgMusicSource;
        [LabelText("特效音乐预制体"), SerializeField] private GameObject _effMusicPrefab;
        private static GameObject EffMusicPrefab => _instance._effMusicPrefab;

        #endregion

        #region 音量相关

        [FormerlySerializedAs("bgMusicVolume")]
        [LabelText("背景音乐音量"), SerializeField, Range(0, 1), OnValueChanged("BgMusicVolumeChanged")]
        private float _bgMusicVolume = .5f;

        public static float BgMusicVolume => _instance._bgMusicVolume;
        private static float BgMusicTotalVolume => _instance._bgMusicVolume * _instance._globalVolume;

        [FormerlySerializedAs("effMusicVolume")]
        [LabelText("特效音乐音量"), SerializeField, Range(0, 1), OnValueChanged("EffMusicVolumeChanged")]
        private float _effMusicVolume = .5f;

        public static float EffMusicVolume => _instance._effMusicVolume;
        private static float EffMusicTotalVolume => _instance._effMusicVolume * _instance._globalVolume;

        [FormerlySerializedAs("globalVolume")]
        [LabelText("全局音乐音量"), SerializeField, Range(0, 1), OnValueChanged("GlobalVolumeChanged")]
        private float _globalVolume = 1;

        public static float GlobalMusicVolume => _instance._globalVolume;

        [FormerlySerializedAs("globalAudioConfig")] [LabelText("全局AudioClip配置"), SerializeField]
        private AudioConfig _globalAudioConfig;

        private static AudioConfig GlobalAudioConfig => _instance._globalAudioConfig;
#if UNITY_EDITOR
        private void BgMusicVolumeChanged() => SetBgMusicVolume(_bgMusicVolume);
        private void EffMusicVolumeChanged() => SetEffMusicVolume(_effMusicVolume);
        private void GlobalVolumeChanged() => SetGlobalVolume(_globalVolume);
#endif

        #endregion

        private static HashSet<AudioSource> _effAudioHashSet = new HashSet<AudioSource>();

        /// <summary>初始化音频系统，查找场景中的 AudioSystem 实例并设置背景音乐初始音量</summary>
        public static void Init()
        {
            _instance = GameRoot.Instance.GetComponentInChildren<AudioSystem>();
            _effAudioHashSet = new HashSet<AudioSource>();
        }

        #region 音量设置

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">背景音乐总的音量还需乘上全局音量</param>
        public static void SetBgMusicVolume(float volume)
        {
            _instance._bgMusicVolume = volume;
            BgMusicSource.volume = BgMusicTotalVolume;
        }

        /// <summary>
        /// 设置特效音乐音量
        /// </summary>
        /// <param name="volume">特效音乐总的音量还需乘上全局音量</param>
        public static void SetEffMusicVolume(float volume)
        {
            _instance._effMusicVolume = volume;
            // 设置特效音量
            if (_effAudioHashSet.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                foreach (AudioSource audioSource in _effAudioHashSet)
                    audioSource.volume = totalVolume;
            }
        }

        /// <summary>设置全局音量，同时影响 BGM 和 SFX</summary>
        public static void SetGlobalVolume(float volume)
        {
            _instance._globalVolume = volume;
            // 设置背景音乐音量
            BgMusicSource.volume = BgMusicTotalVolume;
            // 设置特效音量
            if (_effAudioHashSet.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                foreach (AudioSource audioSource in _effAudioHashSet)
                    audioSource.volume = totalVolume;
            }
        }

        #endregion

        #region 音乐播放

        /// <summary>
        /// 播放全局音效配置里的音效
        /// </summary>
        /// <param name="keyName"></param>
        public static AudioSource PlayEffectMusic(string keyName, float volumeMultiplier = 1)
        {
#if UNITY_EDITOR
            if (GlobalAudioConfig == null)
            {
                LogError("LLKFrame：全局Audio配置不能为空");
                return null;
            }
#endif
            if (!GlobalAudioConfig.ConfigDic.TryGetValue(keyName, out AudioClip audioClip)) return null;
            AudioSource audioSource = PoolSystem.Global.GetGameObject<AudioSource>(EffMusicPrefab, AudioSystemRoot);
            audioSource.spatialBlend = 0;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            _effAudioHashSet.Add(audioSource);
            // 自动回收到对象池
            _instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));

            return audioSource;
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
        public static void PlayEffectMusic(AudioClip audioClip, Vector3 pos, float volumeMultiplier = 1,
            bool is3D = true, bool autoDestroy = true)
        {
#if UNITY_EDITOR
            if (_instance == null)
            {
                PlayEffectMusicInEditor(audioClip, pos, volumeMultiplier, is3D);
                return;
            }
#endif
            AudioSource audioSource = PoolSystem.Global.GetGameObject<AudioSource>(EffMusicPrefab, AudioSystemRoot);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.transform.position = pos;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            _effAudioHashSet.Add(audioSource);
            // 自动回收到对象池
            _instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }      
        /// <summary>在指定位置播放特效音，支持 3D 空间音效，播放完后自动回收 AudioSource 到对象池</summary>
        public static void PlayEffectMusic(AudioClip audioClip, Transform target, float volumeMultiplier = 1,
            bool is3D = true, bool autoDestroy = true)
        {
            AudioSource audioSource = PoolSystem.Global.GetGameObject<AudioSource>(EffMusicPrefab, target);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            _effAudioHashSet.Add(audioSource);
            // 自动回收到对象池
            _instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }

        public static void PlayEffectMusicByRealVolume(AudioClip audioClip, float realVolume)
        {
#if UNITY_EDITOR
            if (_instance == null)
            {
                PlayEffectMusicInEditor(audioClip, Vector3.zero, realVolume, false);
                return;
            }
#endif
            AudioSource audioSource = PoolSystem.Global.GetGameObject<AudioSource>(EffMusicPrefab, AudioSystemRoot);
            audioSource.spatialBlend = 0;
            audioSource.clip = audioClip;
            audioSource.volume = realVolume;
            audioSource.Play();
            // 自动回收到对象池
            _instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }


        /// <summary>等待音频播放完毕后将 AudioSource 回收到对象池</summary>
        private static IEnumerator RecyclyAudioGameObject(AudioSource audioSource, AudioClip clip)
        {
            yield return new WaitForSeconds(clip.length);
            if (audioSource == null) yield break;
            audioSource.PushGameObject();
            _effAudioHashSet.Remove(audioSource);
        }

        #endregion
        
        #region Unity Editor

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器模式（非运行时）播放特效音。
        /// 运行时由 GameRoot 场景中的 AudioSystem 组件初始化，
        /// 此方法仅用于编辑器下（如技能编辑器预览）没有进入 Play 模式时也能播放音效。
        /// 不依赖 AudioSystem 实例，直接创建临时 AudioSource。
        /// </summary>
        private static void PlayEffectMusicInEditor(AudioClip audioClip, Vector3 pos, float volumeMultiplier, bool is3D)
        {
            if (audioClip == null) return;
            GameObject go = new GameObject("EffMusic_Editor");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.position = pos;
            AudioSource source = go.AddComponent<AudioSource>();
            source.spatialBlend = is3D ? 1 : 0;
            source.clip = audioClip;
            source.volume = Mathf.Clamp01(volumeMultiplier);
            source.Play();
            // 编辑模式不能用带延迟的 Object.Destroy，改用编辑器协程延迟销毁
            DelayedDestroyInEditor(go, audioClip.length + 0.1f);
        }

        /// <summary>编辑器模式下延迟销毁对象（用编辑器 update 轮询，避免 Object.Destroy 报错）</summary>
        private static void DelayedDestroyInEditor(GameObject obj, float delay)
        {
            if (obj == null) return;
            float startTime = (float)UnityEditor.EditorApplication.timeSinceStartup;

            void Check()
            {
                if (obj == null)
                {
                    UnityEditor.EditorApplication.update -= Check;
                    return;
                }

                if ((float)UnityEditor.EditorApplication.timeSinceStartup - startTime >= delay)
                {
                    UnityEditor.EditorApplication.update -= Check;
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            UnityEditor.EditorApplication.update += Check;
        }
#endif

        #endregion
    }
}