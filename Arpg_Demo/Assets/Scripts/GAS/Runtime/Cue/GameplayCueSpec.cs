using LLKFrame;

namespace GAS
{
    /// <summary>
    /// GameplayCueSpec —— Cue 的运行时实例（Spec）基类，由模板创建，负责具体运行。
    /// 通过对象池创建与回收，回收时清空引用。
    /// </summary>
    public abstract class GameplayCueSpec
    {
        protected GameplayCue cue;
        protected AbilitySystemComponent target;

        public virtual void Init(GameplayCue cue, AbilitySystemComponent target)
        {
            this.cue = cue;
            this.target = target;
        }

        public abstract void Apply();

        /// <summary>销毁 Spec：销毁模板引用并回收到对象池</summary>
        protected virtual void Destroy()
        {
            cue = null;
            target = null;
            this.PushObject();
        }
    }
}
