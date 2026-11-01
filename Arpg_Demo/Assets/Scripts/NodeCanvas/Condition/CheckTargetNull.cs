using Character;
using NodeCanvas.Framework;
using UnityEngine;

namespace NodeCanvas.Condition
{
    public class CheckTargetNull : ConditionTask
    {
        private CharacterControllerBase _controller;
        private Transform Target => _controller.Target;

        protected override string info => "Target == null";

        protected override string OnInit()
        {
            _controller = agent.GetComponentInChildren<CharacterControllerBase>();
            if (!_controller) Debug.Log("目标上不存在CharacterControllerBase脚本。");
            return null;
        }

        protected override bool OnCheck() => !Target;
    }
}