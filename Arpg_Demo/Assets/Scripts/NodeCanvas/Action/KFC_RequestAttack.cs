using Character;
using NodeCanvas.Framework;

namespace NodeCanvas.Action
{
    public class KFC_RequestAttack : ActionTask
    {
        public BBParameter<StageController> stageController;
        private IKFCEnemy _KfcEnemy;

        protected override string OnInit()
        {
            _KfcEnemy = agent.GetComponent<IKFCEnemy>();
            return null;
        }

        protected override void OnExecute()
        {
            if (stageController.value == null || _KfcEnemy == null) EndAction(false);
            else
            {
                stageController.value.RequestAttack(_KfcEnemy);
                EndAction(true);
            }
        }
    }
}