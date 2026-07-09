using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LLKFrame
{
    public class SingleAnimationNode : AnimationNodeBase
    {
        private AnimationClipPlayable clipPlayable;
        private AnimationMixerPlayable output;

        public void Init(PlayableGraph graph, AnimationMixerPlayable output, AnimationClip clip, float speed, int inputPort)
        {
            this.output = output;
            this.inputPort = inputPort;

            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(speed);
            output.ConnectInput(inputPort, clipPlayable, 0);
        }

        public override void Destroy()
        {
            base.Destroy();
            clipPlayable.Destroy();
        }

        public override void SetSpeed(float speed) => clipPlayable.SetSpeed(speed);
        public override void SetWeight(float weight) => output.SetInputWeight(inputPort, weight);
    }
}
