using System.Collections.Generic;
using Animancer;
using GAS;
using LLKFrame;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

namespace SkillEditor
{
    /// <summary>
    /// 播放与预览：播放/暂停推进、帧 scrub（动画定位/粒子 Simulate/音效播放）、
    /// 预览粒子统一管理、根运动计算。
    /// </summary>
    public class PlaybackController
    {
        private const string PreviewRootName = "GASEditorPreviewRoot";

        private readonly SkillEditorUI _ui;
        private readonly ContentView _content;
        private readonly TimelineView _timeline;
        private readonly PreviewSceneManager _preview;
        private AbilityAsset _asset;

        private readonly Dictionary<MarkEventBase, GameObject> _previewParticles = new();

        private bool _isPlaying;
        private float _playFrame;
        private int _lastFiredFrame;
        private double _lastEditorUpdateTime;
        private AnimancerComponent _activeAnimancer;
        private AnimationClip _activeAnimClip;

        public PlaybackController(SkillEditorUI ui, ContentView content, TimelineView timeline, PreviewSceneManager preview)
        {
            _ui = ui;
            _content = content;
            _timeline = timeline;
            _preview = preview;
        }

        public void Init() => _ui.btnPlay.clicked += TogglePlayback;

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        public void Reset(AbilityAsset asset)
        {
            BindAsset(asset);
            StopPlayback();
            ClearPreviewObjects();
        }

        #region 预览粒子管理

        /// <summary>获取或创建预览根物体（先查找，不存在才创建）</summary>
        private Transform GetOrCreatePreviewRoot()
        {
            Transform root = GameObject.Find(PreviewRootName)?.transform;
            if (root == null) root = new GameObject(PreviewRootName).transform;
            return root;
        }

        /// <summary>清空所有预览物体（粒子实例 + 统一根下子物体），直接销毁根物体本身</summary>
        public void ClearPreviewObjects()
        {
            foreach (GameObject go in _previewParticles.Values)
                if (go != null) DestroyImmediate(go);
            _previewParticles.Clear();

            Transform root = GameObject.Find(PreviewRootName)?.transform;
            if (root != null) DestroyImmediate(root.gameObject);
        }

        #endregion

        #region 播放

        private void TogglePlayback()
        {
            if (_isPlaying) StopPlayback();
            else StartPlayback();
        }

        private void StartPlayback()
        {
            if (_asset == null)
            {
                Debug.LogWarning("请先选择技能配置");
                return;
            }

            // 播放前先清空上次的预览物体，再开始播放
            ClearPreviewObjects();

            _isPlaying = true;
            if (_timeline.CurrentFrameIndex >= _timeline.FrameCount)
                _timeline.CurrentFrameIndex = 0;
            _playFrame = _timeline.CurrentFrameIndex;
            _lastFiredFrame = _timeline.CurrentFrameIndex;
            _lastEditorUpdateTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += PlaybackUpdate;
            _ui.btnPlay.text = "⏸";
        }

        internal void StopPlayback()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            EditorApplication.update -= PlaybackUpdate;
            if (_ui.btnPlay != null) _ui.btnPlay.text = "▶";
            if (_activeAnimancer != null)
            {
                // 停住并复位到绑定姿势
                _activeAnimancer.Graph.Stop();
                _activeAnimancer.Evaluate();
            }
            _activeAnimancer = null;
            _activeAnimClip = null;
        }

        private void PlaybackUpdate()
        {
            if (_asset == null)
            {
                StopPlayback();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastEditorUpdateTime);
            _lastEditorUpdateTime = now;

            _playFrame += dt * TimeSystem.FrameRate;
            int frame = Mathf.FloorToInt(_playFrame);
            if (frame > _timeline.FrameCount) frame = _timeline.FrameCount;

            while (_timeline.CurrentFrameIndex < frame)
                _timeline.CurrentFrameIndex++;

            if (frame >= _timeline.FrameCount)
            {
                StopPlayback();
                // 自然播完（超出总帧范围）清空预览物体
                ClearPreviewObjects();
            }
        }

        #endregion

        #region scrub 预览

        /// <summary>帧变化时 scrub：越界清理 + 刷新各 Cue 预览</summary>
        /// <param name="previewObject">预览模型物体（动画、粒子、音效都以它为目标）</param>
        public void ScrubTo(int frame, bool outOfRange, GameObject previewObject)
        {
            if (outOfRange) ClearPreviewObjects();
            ScrubCurrentFrame(previewObject);
        }

        private void ScrubCurrentFrame(GameObject previewObject)
        {
            if (_asset == null || previewObject == null) return;

            foreach (MarkEventBase mark in _asset.GetAllMarkEvents())
            {
                if (mark == null) continue;
                if (mark is not DurationCueMarkEvent cueMark) continue;

                // 拖动帧在事件起始帧之前：
                // 若该事件已有预览粒子实例，仍需调用 ScrubCue 走 relativeFrame<0 的清理分支
                if (mark.StartFrame > _timeline.CurrentFrameIndex)
                {
                    if (_previewParticles.ContainsKey(mark))
                        ScrubCue(mark, cueMark.DurationCue, previewObject, _timeline.CurrentFrameIndex - mark.StartFrame);
                    continue;
                }

                ScrubCue(mark, cueMark.DurationCue, previewObject, _timeline.CurrentFrameIndex - mark.StartFrame);
            }
        }

        private void ScrubCue(MarkEventBase mark, GameplayCueDurational cue, GameObject previewObject, int relativeFrame)
        {
            if (cue == null) return;

            float playTime = (float)relativeFrame / TimeSystem.FrameRate;

            // 动画：定位并暂停
            if (cue is GameplayCueDurationalAnimation animCue &&
                animCue.AnimationClip != null &&
                relativeFrame <= SkillEditorConfig.DurationFrames(animCue))
            {
                AnimancerComponent animancer = previewObject.GetComponent<AnimancerComponent>();
                float clipFrameCount = animCue.AnimationClip.length * TimeSystem.FrameRate;
                playTime = (relativeFrame % (int)clipFrameCount) / (float)TimeSystem.FrameRate;

                GetAnimationPosAndRotation(_asset, _timeline.CurrentFrameIndex, out Vector3 position, out Quaternion rotation);
                _preview.PreviewModel.transform.position = position;
                _preview.PreviewModel.transform.rotation = rotation;

                if (animancer != null)
                {
                    // 编辑器 scrub：播放并定位到指定时间后暂停(速度 0)，Evaluate 立即把姿势应用到模型
                    AnimancerState state = animancer.Play(animCue.AnimationClip);
                    state.Speed = 0;
                    state.Time = playTime;
                    animancer.Evaluate();
                    _activeAnimancer = animancer;
                }
            }
            // 粒子：Simulate 到对应时间（同一个事件复用同一个实例，实现暂停/继续）
            else if (cue is GameplayCueDurationalParticle particleCue && particleCue.ParticlePrefab != null)
            {
                if (relativeFrame >= SkillEditorConfig.DurationFrames(cue) || relativeFrame < 0)
                {
                    if (_previewParticles.TryGetValue(mark, out GameObject old) && old != null)
                        DestroyImmediate(old);
                    _previewParticles.Remove(mark);
                    return;
                }

                if (!_previewParticles.TryGetValue(mark, out GameObject instance) || instance == null)
                {
                    Transform parent = particleCue.FollowTarget
                        ? previewObject.transform
                        : GetOrCreatePreviewRoot();
                    instance = Instantiate(particleCue.ParticlePrefab, parent);
                    instance.transform.position = _preview.PreviewModel.TransformPoint(particleCue.Position);
                    // 旋转顺序与运行时一致：模型(角色)旋转在前、偏移在后 → 偏移在角色局部坐标生效
                    instance.transform.rotation = _preview.PreviewModel.rotation * Quaternion.Euler(particleCue.Rotation);
                    _previewParticles[mark] = instance;
                }
                instance.SetActive(true);

                foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>())
                {
                    // 第三个参数必须为 true：playTime 是"相对事件起始帧的绝对时间"，不是增量。
                    // 传 false 会从当前状态再快进 playTime 秒，每次刷新都累加 → 粒子很快被快进到寿命之外而消失
                    ps.Simulate(playTime, true, true);
                    ps.Pause();
                }
            }
            // 音效：直接播放（不创建 Spec，避免编辑器预览触发对象池）
            else if (cue is GameplayCueDurationalAudio audioCue && relativeFrame == 0)
                AudioSystem.PlayEffectMusic(audioCue.AudioClip, previewObject.transform.position, audioCue.VolumeMultiplier, audioCue.Is3D);
        }

        /// <summary>粒子 Cue 参数（位置/旋转）变化时实时更新预览实例</summary>
        public void RefreshPreviewParticleTransform(DurationCueMarkEvent cueMark)
        {
            if (cueMark == null || cueMark.DurationCue is not GameplayCueDurationalParticle particleCue) return;
            if (!_previewParticles.TryGetValue(cueMark, out GameObject instance) || instance == null) return;

            instance.transform.position = _preview.PreviewModel.TransformPoint(particleCue.Position);
            // 旋转顺序与运行时一致：模型(角色)旋转在前、偏移在后 → 偏移在角色局部坐标生效
            instance.transform.rotation = _preview.PreviewModel.rotation * Quaternion.Euler(particleCue.Rotation);

            int relativeFrame = _timeline.CurrentFrameIndex - cueMark.StartFrame;
            if (relativeFrame < 0) return;
            if (relativeFrame >= SkillEditorConfig.DurationFrames(particleCue))
            {
                DestroyImmediate(instance);
                _previewParticles.Remove(cueMark);
                return;
            }
            instance.SetActive(true);

            float playTime = (float)relativeFrame / TimeSystem.FrameRate;
            foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>())
            {
                // 同 ScrubCue：绝对时间定位必须 restart=true，否则每拖一帧都在累加快进
                ps.Simulate(playTime, true, true);
                ps.Pause();
            }
        }

        #endregion

        #region 根运动计算

        private void GetAnimationPosAndRotation(AbilityAsset abilityAsset, int curFrame, out Vector3 position, out Quaternion rotation)
        {
            Transform previewModel = _preview.PreviewModel;
            Vector3 tempPos = previewModel.position;
            Quaternion tempRotation = previewModel.rotation;

            previewModel.position = Vector3.zero;
            previewModel.rotation = Quaternion.identity;

            position = Vector3.zero;
            rotation = Quaternion.identity;

            Transform trans = Instantiate(previewModel.gameObject).transform;
            trans.position = Vector3.zero;
            trans.rotation = Quaternion.identity;

            float frameTime = 1f / TimeSystem.FrameRate;

            foreach (MarkEventBase mark in abilityAsset.GetAllMarkEvents())
            {
                int startFrame = mark.StartFrame;
                if (startFrame >= curFrame) continue;
                switch (mark)
                {
                    case DurationCueMarkEvent durationCue:
                        if (durationCue.DurationCue is not GameplayCueDurationalAnimation animCue) continue;
                        if (!animCue.RootMotion) continue;

                        int durationFrame = SkillEditorConfig.DurationFrames(animCue);
                        AnimationClip clip = animCue.AnimationClip;
                        if (clip == null) continue;

                        float clipFrameCount = clip.length * TimeSystem.FrameRate;

                        int realFrameCount = startFrame + durationFrame <= curFrame ? durationFrame : curFrame - startFrame;
                        int loopCount = realFrameCount / (int)clipFrameCount;

                        clip.SampleAnimation(trans.gameObject, clip.length);
                        previewModel.position += loopCount * previewModel.TransformPoint(trans.position);
                        Quaternion rot = Quaternion.Euler(previewModel.TransformDirection(trans.rotation.eulerAngles));
                        for (int i = 0; i < loopCount; i++)
                            previewModel.rotation = rot * previewModel.rotation;

                        if ((realFrameCount % (int)clipFrameCount) == 0) continue;

                        float remainFrame = realFrameCount % clipFrameCount;
                        clip.SampleAnimation(trans.gameObject, remainFrame * frameTime);
                        previewModel.position += previewModel.TransformDirection(trans.position);
                        previewModel.rotation = Quaternion.Euler(previewModel.TransformDirection(trans.eulerAngles)) * previewModel.rotation;
                        break;
                }
            }

            position = previewModel.position;
            rotation = previewModel.rotation;

            previewModel.transform.position = tempPos;
            previewModel.transform.rotation = tempRotation;

            DestroyImmediate(trans.gameObject);
        }

        #endregion
    }
}
