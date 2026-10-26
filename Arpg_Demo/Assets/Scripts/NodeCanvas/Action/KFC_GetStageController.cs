using Character;
using NodeCanvas.Framework;
using UnityEngine;

namespace NodeCanvas.Action
{
    public class KFC_GetStageController : ActionTask
    {
        public BBParameter<Transform> target;
        public BBParameter<StageController> stageController;

        protected override void OnExecute()
        {
           StageController sc= target.value.GetComponent<IKFCProvider>()?.StageController;
           stageController.SetValue(sc);
           EndAction();
        }
    }
}