using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Cue 表现型瞬时效果：只负责播表现(动画/音效/粒子)，不改属性、不做结算。
    /// <para>动画 Cue：走"受控表现" —— 由目标角色按动画长度进入/延长受控状态，并在被打断时
    /// 暂停当前动画、播完新动画后从暂停处继续(眩晕被受击打断再恢复眩晕)。</para>
    /// <para>持续类 Cue：各自按持续时长(秒)计时回收，本效果不做持有。</para>
    /// </summary>
    public class GameplayCueEffect : GameplayInstantEffect
    {
        [BoxGroup("基础信息")]
        [InfoBox("只播表现，不改属性。动画 Cue 会按动画长度让目标进入受控状态。")]
        [LabelText("Cue列表")]
        [ShowInInspector]
        public List<GameplayCue> cueList = new();

        /// <summary>运行时安全访问:老资产反序列化后 cueList 可能为 null,按需 new 一个空列表。</summary>
        protected List<GameplayCue> CueList => cueList ??= new();

        [LabelText("作用于目标"), Tooltip("勾选 = 作用于 target（承受方）；取消 = 作用于 source（施法方，例如给自己套霸体/定身）")]
        public bool isTarget = true;
        
        /// <summary>
        /// 播放 Cue 表现（跑在拷贝出来的运行时实例上，source / target 已就绪）。
        /// 子类可以重写它来改变"表现作用到谁身上"，或改成别的播放方式（例如顺序播放）。
        /// </summary>
        protected override void ApplyEffect()
        {
            foreach (GameplayCue cue in CueList)
            {
                cue.Apply(target);
            }
        }
    }
}
