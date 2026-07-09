using UnityEngine.Playables;

namespace LLKFrame
{
    public abstract class AnimationNodeBase
    {
        protected int inputPort;

        public virtual void Destroy() => this.PushObject();
        public abstract void SetSpeed(float speed);
        public abstract void SetWeight(float weight);
    }
}
