using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LLKFrame
{
    public class AudioSystem : SingletonMonoBase<AudioSystem>
    {
        #region 组件、预制体相关
        [LabelText("背景音乐播放器"), SerializeField] private AudioSource bgMusicSource;
        private static AudioSource BgMusicSource => instance.bgMusicSource;
        [LabelText("特效音乐预制体"), SerializeField] private GameObject effMusicPrefab;
        private static GameObject EffMusicPrefab => instance.effMusicPrefab;
        #endregion
        #region 音量相关
        [LabelText("背景音乐音量"), SerializeField, Range(0, 1), OnValueChanged("BgMusicVolumeChanged")]
        private float bgMusicVolume = .5f;
        public static float BgMusicTotalVolume => instance.bgMusicVolume * instance.globalVolume;
        [LabelText("特效音乐音量"), SerializeField, Range(0, 1), OnValueChanged("EffMusicVolumeChanged")]
        private float effMusicVolume = .5f;
        public static float EffMusicTotalVolume => instance.effMusicVolume * instance.globalVolume;
        [LabelText("全局音乐音量"), SerializeField, Range(0, 1), OnValueChanged("GlobalVolumeChanged")] private float globalVolume = 1;
#if UNITY_EDITOR
        private void BgMusicVolumeChanged() => SetBgMusicVolume(bgMusicVolume);
        private void EffMusicVolumeChanged() => SetEffMusicVolume(effMusicVolume);
        private void GlobalVolumeChanged() => SetGlobalVolume(globalVolume);
#endif
        #endregion

        private static List<AudioSource> effAudioList = new List<AudioSource>();
        public static void Init()
        {
            effAudioList = new List<AudioSource>();
            BgMusicSource.volume = BgMusicTotalVolume;
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
            if (effAudioList.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                for (int i = 0; i < effAudioList.Count; i++)
                    effAudioList[i].volume = totalVolume;
            }
        }
        public static void SetGlobalVolume(float volume)
        {
            instance.globalVolume = volume;
            // 设置背景音乐音量
            BgMusicSource.volume = BgMusicTotalVolume;
            // 设置特效音量
            if (effAudioList.Count > 0)
            {
                float totalVolume = EffMusicTotalVolume;
                for (int i = 0; i < effAudioList.Count; i++)
                    effAudioList[i].volume = totalVolume;
            }
        }
        #endregion
        #region 音乐播放
        public static void PlayBgMusic(AudioClip audioClip, float volumeMultiplier = 1)
        {
            BgMusicSource.clip = audioClip;
            BgMusicSource.volume = BgMusicTotalVolume * volumeMultiplier;
            BgMusicSource.Play();
        }
        public static void ResumeBgMusic()
        {
            if (!BgMusicSource.isPlaying && BgMusicSource.clip != null)
                BgMusicSource.UnPause();
        }
        public static void PauseBgMusic()
        {
            if (BgMusicSource.isPlaying)
                BgMusicSource.Pause();
        }
        public static void StartBgMusic()
        {
            if (!BgMusicSource.isPlaying && BgMusicSource.clip != null)
                BgMusicSource.Play();
        }
        public static void StopBgMusic()
        {
            if (BgMusicSource.isPlaying)
                BgMusicSource.Stop();
        }
        public static void PlayEffectMusic(AudioClip audioClip, Vector3 pos, float volumeMultiplier = 1, bool is3D = true, bool autoDestroy = true)
        {
            AudioSource audioSource = PoolSystem.GetGameObject<AudioSource>(EffMusicPrefab);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.transform.position = pos;
            audioSource.clip = audioClip;
            audioSource.volume = EffMusicTotalVolume * volumeMultiplier;
            audioSource.Play();
            effAudioList.Add(audioSource);
            // 自动回收到对象池
            instance.StartCoroutine(RecyclyAudioGameObject(audioSource, audioClip));
        }

        private static IEnumerator RecyclyAudioGameObject(AudioSource audioSource, AudioClip clip)
        {
            yield return new WaitForSeconds(clip.length);
            if (audioSource.gameObject == null) yield break;
            audioSource.PushGameObject();
            effAudioList.Remove(audioSource);
        }
        #endregion
    }
}