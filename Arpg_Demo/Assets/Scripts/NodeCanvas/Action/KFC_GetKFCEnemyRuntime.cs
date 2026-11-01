using Character;
using NodeCanvas.Framework;

namespace NodeCanvas.Action
{
    public class KFC_GetKFCEnemyRuntime :ActionTask
    {
        public BBParameter<StageController>stageController;
        private IKFCEnemy _kfcEnemy;

        protected override string OnInit()
        {
            _kfcEnemy = agent.GetComponent<IKFCEnemy>();
            return null;
        }

        protected override void OnExecute()
        {
            if (stageController.value == null || _kfcEnemy == null)
            {
                EndAction(false);
                return;
            }

            stageController.value.GetKfcEnemyRuntime(_kfcEnemy.kfcEnemyConfig.Id);
            EndAction(true);
        }
    }
}