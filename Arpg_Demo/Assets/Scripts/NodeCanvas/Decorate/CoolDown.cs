using System.Collections;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.BehaviourTrees
{
    [Name("冷却")]
    [Category("通用/Decorate")]
    public class CoolDown : BTDecorator
    {
        [Name("冷却时间")] public BBParameter<float> coolTime = 3;
        [Name("计时器"), BlackboardOnly] public BBParameter<float> coolTimer = 0;
        [Name("冷却返回状态")] public FinalStatus coolingStatus = FinalStatus.Optional;
        
        protected override Status OnExecute(Component agent, IBlackboard blackboard)
        {
            if (decoratedConnection == null)
            {
                return Status.Optional;
            }

            if (coolTimer.value > 0)
            {
                return (Status)coolingStatus;
            }

            status = decoratedConnection.Execute(agent, blackboard);
            if (status == Status.Success || status == Status.Failure)
            {
                StartSyncedCoroutine(Internal_Cooldown());
            }

            return status;
        }
        
        IEnumerator Internal_Cooldown()
        {
            float currentTime = coolTime.value;
            coolTimer.SetValue(currentTime);
            while (currentTime > 0)
            {
                yield return null;
                currentTime -= Time.deltaTime;
                coolTimer.SetValue(currentTime);
            }
        }
    }
}