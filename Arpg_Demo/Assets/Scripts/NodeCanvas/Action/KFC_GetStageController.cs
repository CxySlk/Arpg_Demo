using Character;
using NodeCanvas.Framework;
using UnityEngine;

namespace NodeCanvas.Action
{
    public class KFC_GetStageController : ActionTask
    {
        private CharacterControllerBase _controller;
        private Transform Target => _controller?.Target;
        public BBParameter<StageController> stageController;

        protected override string info => "GetStageController " + stageController;

        protected override string OnInit()
        {
            _controller = agent.transform.GetComponentInChildren<CharacterControllerBase>();
            return null;
        }

        protected override void OnExecute()
        {
            if (!Target)
            {
                EndAction(false);
                return;
            }

            StageController sc = Target.GetComponent<IKFCProvider>()?.StageController;
            stageController?.SetValue(sc);
            EndAction(true);
        }
    }
}