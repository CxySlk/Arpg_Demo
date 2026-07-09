using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LLKFrame
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;

        private AnimationNodeBase previousNode;
        private AnimationNodeBase currentNode;

        // 播放动画时（需要过渡的情况）：inputPort0固定为previousNode的端口，inputPort1为currentNode的端口
        private int inputPort0 = 0;
        private int inputPort1 = 1;

        private float playSpeed;
        public float PlaySpeed
        {
            get=> playSpeed;
            set
            {
                playSpeed = value;
                currentNode.SetSpeed(playSpeed);
            }
        }

        private Coroutine transCoroutine; // 切换动画的协程

        public void Init()
        {
            graph = PlayableGraph.Create();
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            output.SetSourcePlayable(mixer);
        }
        private void OnDisable() => graph.Stop();
        private void OnDestroy() => graph.Destroy();

        #region 动画播放
        /// <summary>
        /// 播放单个动画片段
        /// </summary>
        /// <param name="clip">动画片段</param>
        /// <param name="playSpeed">播放速度</param>
        /// <param name="transitionTime">过渡时间</param>
        public void PlaySingleAnimation(AnimationClip clip, float playSpeed = 1, float transitionTime = .25f)
        {
            SingleAnimationNode node = PoolSystem.GetOrNewObject<SingleAnimationNode>();
            // 第一次播放，不需要过渡
            if (currentNode is null)
            {
                node.Init(graph, mixer, clip, playSpeed, inputPort0);
                currentNode = node;
                node.SetWeight(1);
            }
            // 需要过渡
            else
            {
                // 需要先销毁，防止占着端口
                if (previousNode != null)
                {
                    previousNode.Destroy();
                    previousNode = null;
                }
                node.Init(graph, mixer, clip, playSpeed, inputPort1);
                // 可能之前的动画还没有过渡完成，所以需要销毁（回收）
                previousNode = currentNode;
                currentNode = node;
                // 关闭之前的过渡协程，开启新的动画过渡协程
                if (transCoroutine != null) StopCoroutine(transCoroutine);
                transCoroutine = StartCoroutine(TransitionAnimationCoroutine(transitionTime));
            }
            if (!graph.IsPlaying()) graph.Play();
        }
        /// <summary>
        /// 播放混合动画
        /// </summary>
        /// <param name="clips">混合动画</param>
        /// <param name="playSpeed">播放速度</param>
        /// <param name="transitionTime">过渡时间</param>
        public void PlayBlendAnimation(AnimationClip[] clips, float playSpeed = 1, float transitionTime = .25f)
        {
            BlendAnimationNode node = PoolSystem.GetOrNewObject<BlendAnimationNode>();
            // 第一次播放，不需要过渡
            if (currentNode is null)
            {
                node.Init(graph, mixer, clips, playSpeed, inputPort0);
                currentNode = node;
                node.SetWeight(1);
            }
            // 需要过渡
            else
            {
                // 需要先销毁，防止占着端口
                if (previousNode != null)
                {
                    previousNode.Destroy();
                    previousNode = null;
                }
                node.Init(graph, mixer, clips, playSpeed, inputPort1);
                // 可能之前的动画还没有过渡完成，所以需要销毁（回收）
                previousNode = currentNode;
                currentNode = node;
                // 关闭之前的过渡协程，开启新的动画过渡协程
                if (transCoroutine != null) StopCoroutine(transCoroutine);
                transCoroutine = StartCoroutine(TransitionAnimationCoroutine(transitionTime));
            }
            if (!graph.IsPlaying()) graph.Play();
        }
        private IEnumerator TransitionAnimationCoroutine(float transitionTime)
        {
            previousNode.SetWeight(1);
            currentNode.SetWeight(0);

            // 交换端口
            int tempPort = inputPort0;
            inputPort0 = inputPort1;
            inputPort1 = tempPort;

            float weight = 1;

            while (weight > 0)
            {
                yield return null;
                weight = Mathf.Clamp01(weight - Time.deltaTime / transitionTime);
                previousNode?.SetWeight(weight);
                currentNode?.SetWeight(1 - weight);
            }
            // 等待切换的最后一帧
            yield return null;

            // 切换完后，销毁之前的动画节点
            previousNode.Destroy();
            previousNode = null;
        }
        #endregion
        #region 混合动画播放权重
        public void SetBlendWeight(float[] weights) => (currentNode as BlendAnimationNode).SetBlendWeights(weights);
        public void SetBlendWeight(float weight) => (currentNode as BlendAnimationNode).SetBlendWeights(weight);
        #endregion
        #region RootMotion
        private Action<Vector3, Quaternion> onRootMotion;
        private void OnAnimatorMove() => onRootMotion?.Invoke(animator.deltaPosition, animator.deltaRotation);
        /// <summary>设置动画根运动回调函数</summary>
        public void SetRootMotion(Action<Vector3, Quaternion> callBack) => onRootMotion = callBack;
        /// <summary>动画根运动回调函数置空</summary>
        public void ClearRootMotion() => onRootMotion = null;
        #endregion
    }
}
