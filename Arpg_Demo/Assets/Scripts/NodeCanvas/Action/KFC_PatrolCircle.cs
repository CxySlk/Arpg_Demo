using Animancer;
using cfg;
using Character;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Action
{
    [Category("KFC")]
    [Name("KFC 圈外巡逻")]
    public class KFC_PatrolCircle : ActionTask
    {
        public BBParameter<StageController> stageController;

        [Name("切向偏移")] public float patrolSpeed = 1f;
        [Name("参数平滑时间")] public float smoothTime = 0.15f;
        [Name("导航刷新阈值")] public float ignoreDis = 0.5f;

        private static readonly StringReference ParameterX = "MoveX";
        private static readonly StringReference ParameterY = "MoveY";

        private EnemyController _controller;
        private NavMeshAgent _navMeshAgent;
        private Attribute _moveSpeedAttr;
        private Vector2MixerState _state;
        private SmoothedVector2Parameter _smoothed;
        private Vector3 _lastDest;
        private bool _hasDest;

        protected override string OnInit()
        {
            _controller = agent.GetComponent<EnemyController>();
            _navMeshAgent = _controller.NavMeshAgent;
            _controller.Asc.TryGetAttrValue(EAttributeType.MoveSpeed, out _moveSpeedAttr);
            return null;
        }

        protected override void OnExecute()
        {
            if (stageController.value.IsOnWaitCircle(_controller) != -1)
            {
                EndAction(true);
                return;
            }

            _navMeshAgent.updateRotation = false;
            _controller.LockTarget(true);

            _state = _controller.CharacterView.Play("Move2D") as Vector2MixerState;
            _controller.SetRootMotion(null);
            if (_state == null)
            {
                Debug.LogError("[KFC_PatrolCircle] \"Move2D\" 不是 2D 混合(MixerTransition2D)");
                EndAction(false);
                return;
            }

            _state.ParameterNameX = ParameterX;
            _state.ParameterNameY = ParameterY;
            _state.Parameter = Vector2.zero;

            _smoothed?.Dispose();
            _smoothed = new SmoothedVector2Parameter(
                _controller.CharacterView.Animancer.Graph, ParameterX, ParameterY, smoothTime);

            _navMeshAgent.speed = _moveSpeedAttr.CurValue;
            _navMeshAgent.stoppingDistance = 0.1f;
            _hasDest = false;
        }

        protected override void OnUpdate()
        {
            if (stageController.value.IsOnWaitCircle(_controller) != -1)
            {
                EndAction(true);
                return;
            }

            stageController.value.GetOutCirclePatrolPos(
                _controller.selfTrans.position, patrolSpeed, out Vector3 target);

            if (!_hasDest || Vector3.SqrMagnitude(target - _lastDest) > ignoreDis * ignoreDis)
            {
                _navMeshAgent.SetDestination(target);
                _lastDest = target;
                _hasDest = true;
            }

            float speed = Mathf.Max(_navMeshAgent.speed, 0.0001f);
            Vector3 localVelocity = _controller.transform.InverseTransformDirection(_navMeshAgent.velocity);
            _smoothed.TargetValue = Vector2.ClampMagnitude(
                new Vector2(localVelocity.x / speed, localVelocity.z / speed), 1f);
        }

        protected override void OnStop()
        {
            _smoothed?.Dispose();
            _smoothed = null;
            _controller.LockTarget(false);
        }
    }
}
