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
        [LabelText("受控时间")] public float hitControlTime = 0;

        protected override void ApplyEffect()
        {
            Vector3 forward = target.transform.forward;
            Vector3 toSource = source.transform.position - target.transform.position;

            float angle = Vector3.SignedAngle(forward, toSource, Vector3.up);
            ICharacterController character = target.GetComponent<ICharacterController>();
            AnimancerComponent animancer = character.Animancer;
            
            switch (angle)
            {
                case > -45 and <= 45:
                    animancer.Play(hitF);
                    character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitF.length);
                    break;
                case > 45 and <= 135:
                    animancer.Play(hitR);
                    character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitR.length);
                    break;
                case > 135 and <= 225:
                    animancer.Play(hitB);
                    character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitB.length);
                    break;
                case > 225 and <= 315:
                    animancer.Play(hitL);
                    character.EnterControlled(hitControlTime > 0 ? hitControlTime : hitL.length);
                    break;
            }
        }
    }
}