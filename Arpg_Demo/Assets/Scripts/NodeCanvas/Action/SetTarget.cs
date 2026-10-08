using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Action
{
    [Category("通用/Action")]
    public class SetTarget : ActionTask
    {
        // 类型必须与要设空的黑板变量一致（目标是 Transform：黑板 tareget → EnemyController.Target）。
        // 若用 BBParameter<Object>，NodeCanvas 只能读不能写回 Transform 变量，会退化成"只警告不赋值"的空操作，设空无效。
        [Name("控制器")] public BBParameter<CharacterControllerBase> controller;
        [Name("目标值")] public BBParameter<Transform> value;

        protected override void OnExecute()
        {
            controller.value.SetTarget(value.value);
            EndAction();
        }
    }
}