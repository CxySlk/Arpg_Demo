using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LLKFrame
{
    public class BlendAnimationNode : AnimationNodeBase
    {
        private AnimationMixerPlayable blendPlayable;
        private AnimationMixerPlayable output;

        public void Init(PlayableGraph graph, AnimationMixerPlayable output, AnimationClip[] clips, float playSpeed, int inputPort)
        {
            this.output = output;
            this.inputPort = inputPort;

            blendPlayable = AnimationMixerPlayable.Create(graph, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                blendPlayable.ConnectInput(i, CreateAnimation(graph, clips[i]), 0);
                blendPlayable.SetInputWeight(i, 1);
            }
            blendPlayable.SetSpeed(playSpeed);
            output.ConnectInput(inputPort, blendPlayable, 0);
        }

        private AnimationClipPlayable CreateAnimation(PlayableGraph graph, AnimationClip clip)
        {
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, clip);
            return clipPlayable;
        }

        public override void SetSpeed(float speed) => blendPlayable.SetSpeed(speed);
        public override void SetWeight(float weight) => output.SetInputWeight(inputPort, weight);
        public override void Destroy()
        {
            base.Destroy();
            blendPlayable.Destroy();
        }
        public void SetBlendWeights(float[] weights)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                blendPlayable.SetInputWeight(i, weights[i]);
            }
        }

        public void SetBlendWeights(float weight)
        {
            blendPlayable.SetInputWeight(0, weight);
            blendPlayable.SetInputWeight(1, 1 - weight);
        }
    }
}
