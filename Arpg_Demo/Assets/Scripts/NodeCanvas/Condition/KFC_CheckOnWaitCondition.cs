using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace NodeCanvas.Condition
{
    [Category("KFC")]
    public class KFC_CheckOnWaitCondition : ConditionTask
    {
        [Name("KFC_StageController")] public BBParameter<StageController> stageController;
        private EnemyController _selfController;

        protected override string OnInit()
        {
            _selfController = agent.GetComponent<EnemyController>();
            return null;
        }

        protected override bool OnCheck()
        {
            return stageController.value.IsOnWaitCircle(_selfController) != -1;
        }
    }
}