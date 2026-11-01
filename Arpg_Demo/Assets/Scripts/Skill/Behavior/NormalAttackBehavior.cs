using GAS;
using LLKFrame;
using UnityEngine;

namespace Skill
{
    public class NormalAttackBehavior : SkillBehaviorBase
    {
        private float _strengthTimer;
        private bool _isStrength;

         protected override void TickUpdate(int frame)
        {
            base.TickUpdate(frame);
            UpdateStrengthAttackTime();
        }

        private void UpdateStrengthAttackTime()
        {
            if (_isStrength)
            {
                _strengthTimer -= TimeSystem.FrameTime;
                if (_strengthTimer <= 0)
                    _isStrength = false;
            }
        }

        public override SkillBehaviorBase DeepCopy()
        {
            return new NormalAttackBehavior();
        }

        public override void PlaySkill()
        {
            isCurrentAbilityDone = false;
            isAbilityInterval = false;
            // 强化攻击，播放最后一段攻击
            if (_isStrength)
            {
                PlayAbilitySegment(Abilities.Count - 1);
                return;
            }

            base.PlaySkill();
        }

        protected override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            base.OnRootMotion(deltaPos, deltaRot);
        }
    }
}