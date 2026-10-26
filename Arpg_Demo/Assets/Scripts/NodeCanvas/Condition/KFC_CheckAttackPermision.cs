using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace NodeCanvas.Condition
{
    public class KFC_CheckAttackPermision : ConditionTask
    {
        public BBParameter<StageController> stageController;
        private IKFCEnemy _enemyController;

        protected override string OnInit()
        {
            _enemyController = agent.GetComponent<IKFCEnemy>();
            return null;
        }

        protected override bool OnCheck()
        {
            return stageController.value.IsOnAtkCircle(_enemyController) != -1;
        }
    }
}