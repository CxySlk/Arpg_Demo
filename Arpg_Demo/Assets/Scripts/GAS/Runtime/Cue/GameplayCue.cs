using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public abstract class GameplayCue : ConfigBase
    {
        [SerializeField, LabelText("需要的标签")]
        private GameplayTagSet _requireTags;
        public GameplayTagSet RequireTags => _requireTags;
        [SerializeField, LabelText("阻塞的标签")]
        private GameplayTagSet _blockedTags;
        public GameplayTagSet BlockedTags => _blockedTags;

        public abstract void ApplyFrom(AbilitySystemComponent asc);
        public virtual bool Triggerable(AbilitySystemComponent asc)
        {
            if (_requireTags != null && !_requireTags.Empty && !asc.CheckAllTags(_requireTags)) return false;
            if (_blockedTags != null && !_blockedTags.Empty && asc.CheckAnyTags(_blockedTags)) return false;
            return true;
        }
    }
    public abstract class GameplayCue<T> : GameplayCue where T : GameplayCueSpec
    {
        public abstract T CreateSpec();
    }
}
