using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 效果基类。既是配置(标签条件)也是运行时实例：
    /// 技能配置、ASC 预设里直接内联配置一份，运行时由 ASC 拷贝一份再 Init(source, target) 使用，
    /// 因此配置对象本身不会被运行时状态污染。
    /// </summary>
    public abstract class GameplayEffect : ITagCondition
    {
        #region 标签相关

        [BoxGroup("标签集")]
        [GUIColor("green")]
        [SerializeField, LabelText("必备标签")]
        protected GameplayTagSet requiredTags = new();

        public GameplayTagSet RequiredTags => requiredTags;

        [BoxGroup("标签集")]
        [GUIColor("red")]
        [SerializeField, LabelText("禁用标签")]
        protected GameplayTagSet blockedTags = new();

        public GameplayTagSet BlockedTags => blockedTags;

        [BoxGroup("标签集")]
        [GUIColor("cyan")]
        [SerializeField, LabelText("赋予标签")]
        protected GameplayTagSet grantedTags = new();

        public GameplayTagSet GrantedTags => grantedTags;

        [BoxGroup("标签集")]
        [GUIColor("orange")]
        [SerializeField, LabelText("移除标签")]
        protected GameplayTagSet removedTags = new();

        public GameplayTagSet RemovedTags => removedTags;

        #endregion

        #region 运行时

        protected AbilitySystemComponent source;
        protected AbilitySystemComponent target;

        protected virtual void Init(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            this.source = source;
            this.target = target;
        }

        #endregion

        /// <summary>
        /// 拷贝一份用于运行时的实例：配置数据整体浅拷贝(只读共用引用即可)，
        /// 运行时状态(持有者、计时、Cue Spec 等)由 <see cref="OnCopied"/> 清掉。
        /// </summary>
        public GameplayEffect Copy()
        {
            GameplayEffect copy = (GameplayEffect)MemberwiseClone();
            copy.source = null;
            copy.target = null;
            copy.OnCopied();
            return copy;
        }

        /// <summary>拷贝后清理运行时状态（子类覆盖，缓存了运行时对象的记得清）</summary>
        protected virtual void OnCopied()
        {
        }
    }
}