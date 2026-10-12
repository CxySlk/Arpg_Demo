using System.Collections;
using Animancer;
using Character;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill
{
    public class ParryAttackBehavior : SkillBehaviorBase
    {
        private bool _parryIng = false;
        [LabelText("格挡标签"), SerializeField] private GameplayTag _parryTag;
        [LabelText("格挡音效"), SerializeField] private AudioClip _parryAudioClip;
        [LabelText("格挡动画"), SerializeField] private AnimationClip _parryAnimClip;
        [LabelText("控制时间"), SerializeField] private float _controlTime = 1;

        public override SkillBehaviorBase DeepCopy()
        {
            return new ParryAttackBehavior()
            {
                _parryTag = _parryTag,
                _parryAudioClip = _parryAudioClip,
                _parryAnimClip = _parryAnimClip
            };
        }

        public override bool CheckCanRelease()
        {
            return base.CheckCanRelease() && abilityIndex == 0;
        }

        protected override void OnCustomEvent(string eventName, FrameEvent frameEvent)
        {
            switch (eventName)
            {
                case "ParryBegin":
                    _parryIng = true;
                    GasOwner.AddDynamicTag(_parryTag);
                    GasOwner.GasEventCenter.Subscribe(GASEventCenterKeys.ParryableAttackEventKey, ParryableAttackHit);
                    break;
                case "ParryEnd":
                    _parryIng = false;
                    GasOwner.RemoveDynamicTag(_parryTag);
                    GasOwner.GasEventCenter.UnSubscribe(GASEventCenterKeys.ParryableAttackEventKey, ParryableAttackHit);
                    break;
            }
        }

        /// <summary>
        /// 收到可以格挡的攻击
        /// </summary>
        /// <param name="obj"></param>
        private void ParryableAttackHit(GameObject obj)
        {
            AudioSystem.PlayEffectMusic(_parryAudioClip, skillBrain.transform);
            SkillBrainBase _skillBrain = obj.GetComponent<SkillBrainBase>();
            _skillBrain.InterruptAllSkills();
            CharacterControllerBase controller = _skillBrain.characterController;
            controller.Animancer.Play(_parryAnimClip, .25f, FadeMode.FromStart);
            controller.SetRootMotion(OnRootMotion);
            controller.EnterControlled(_controlTime);
            // 释放下一段技能
            UnexpectedInterrupt();
            PlayAbilitySegment(1);
        }

        protected override void OnInterrupt()
        {
            // 延迟一帧再摘格挡标签：命中当帧的 控制/伤害 效果检查时标签仍在，才能被 Status.格挡 挡掉
            if (_parryIng)
            {
                MonoSystem.Start_Coroutine(RemoveParryTagNextFrame());
                _parryIng = false;
            }

            GasOwner.GasEventCenter.UnSubscribe(GASEventCenterKeys.ParryableAttackEventKey, ParryableAttackHit);
        }

        /// <summary>延迟一帧移除格挡标签（命中当帧仍需靠它阻塞敌方效果）。</summary>
        private IEnumerator RemoveParryTagNextFrame()
        {
            yield return null;
            GasOwner.RemoveDynamicTag(_parryTag);
        }
    }
}