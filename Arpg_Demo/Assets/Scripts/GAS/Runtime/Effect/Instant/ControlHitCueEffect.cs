using System;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public class ControlHitCueEffect : GameplayInstantEffect
    {
        [LabelText("受击（前）")] public AnimationClip hitF;
        [LabelText("受击（后）")] public AnimationClip hitB;
        [LabelText("受击（左）")] public AnimationClip hitL;
        [LabelText("受击（右）")] public AnimationClip hitR;
        [LabelText("是否开启根运动")] public bool rootMotion = true;
        [LabelText("受控时间")] public float hitControlTime = 0;
        private ICharacterController _character;

        protected override void ApplyEffect()
        {
            Vector3 forward = target.transform.forward;
            Vector3 toSource = source.transform.position - target.transform.position;

            float angle = Vector3.SignedAngle(forward, toSource, Vector3.up);
            _character = target.GetComponent<ICharacterController>();
            AnimancerComponent animancer = _character.Animancer;

            switch (angle)
            {
                case > -45 and <= 45:
                    animancer.Play(hitF, .25f, FadeMode.FromStart);
                    _character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitF.length);
                    break;
                case > 45 and <= 135:
                    animancer.Play(hitR, .25f, FadeMode.FromStart);
                    _character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitR.length);
                    break;
                case > 135 or <= -135:
                    animancer.Play(hitB, .25f, FadeMode.FromStart);
                    _character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitB.length);
                    break;
                case > -135 and <= -45:
                    animancer.Play(hitL, .25f, FadeMode.FromStart);
                    _character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitL.length);
                    break;
            }

            _character.SetRootMotion(rootMotion ? RootMotion : null);
        }

        private void RootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            _character.Move(deltaPos);
            _character.Rotate(deltaRot);
        }
    }
}