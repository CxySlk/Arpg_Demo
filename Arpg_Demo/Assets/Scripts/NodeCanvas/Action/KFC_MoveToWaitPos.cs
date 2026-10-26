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
    public class KFC_MoveToWaitPos : ActionTask
    {
        public BBParameter<StageController> stageController;
        [Name("停止距离")] public float stopDis = .1f;

        [Name("忽略的距离"), Tooltip("防止目标一移动就重新导航，只有当目标与上一次目标的距离超过这个时，才会重新定位")]
        public float ignoreDis = .1f;

        private EnemyController _controller;
        private Vector3 _lastWaitPos;
        private Vector3 _waitPos;
        private int _waitIndex;
        private NavMeshAgent _navMeshAgent;
        private Attribute _moveSpeedAttr;
        private Vector2MixerState _state;
        private SmoothedVector2Parameter _smoothed;

        [Name("参数平滑时间"), Tooltip("秒；越大越柔")] public float smoothTime = 0.15f;

        private static readonly StringReference ParameterX = "MoveX";
        private static readonly StringReference ParameterY = "MoveY";

        protected override string OnInit()
        {
            _controller = agent.GetComponent<EnemyController>();
            _navMeshAgent = _controller.NavMeshAgent;
            _controller.Asc.TryGetAttrValue(EAttributeType.MoveSpeed, out _moveSpeedAttr);
            return null;
        }

        protected override void OnExecute()
        {
            _waitIndex = stageController.value.IsOnWaitCircle(_controller);
            _navMeshAgent.updateRotation = false;
            _state = _controller.CharacterView.Play("Move2D") as Vector2MixerState;
            _controller.SetRootMotion(null);
            if (_state == null)
            {
                Debug.LogError("[KFC_MoveToWaitPos] \"Move2D\" 不是 2D 混合(MixerTransition2D),无法驱动混合参数");
                EndAction(false);
                return;
            }

            // 让 mixer 绑定到这两个参数名，并从 0 开始平滑
            _state.ParameterNameX = ParameterX;
            _state.ParameterNameY = ParameterY;
            _state.Parameter = Vector2.zero;

            // 每次执行重建平滑器（OnStop 会 Dispose）
            _smoothed?.Dispose();
            _smoothed = new SmoothedVector2Parameter(
                _controller.CharacterView.Animancer.Graph, ParameterX, ParameterY, smoothTime);

            _controller.LockTarget(true);
            // 当前agent不在等待队列中
            if (_waitIndex == -1)
            {
                EndAction(false);
                return;
            }

            _waitPos = GetWaitPos(_waitIndex);
            // 到达后不再结束，保持 Running 占住等待位；走到位后 IDLE 混合会平滑回 (0,0)
            _navMeshAgent.SetDestination(_waitPos);
            _navMeshAgent.stoppingDistance = stopDis;
            _lastWaitPos = _waitPos;
        }

        protected override void OnUpdate()
        {
            // 每帧驱动 2D 参数（必须在任何 early return 之前）：本地空间 + 归一化到 -1..1
            float speed = Mathf.Max(_moveSpeedAttr.CurValue, 0.0001f);
            Vector3 localVelocity = _controller.transform.InverseTransformDirection(_navMeshAgent.velocity);
            _smoothed.TargetValue = Vector2.ClampMagnitude(
                new Vector2(localVelocity.x / speed, localVelocity.z / speed), 1f);

            // 只有拿到攻击许可才结束（在等待队列里 != 能出手）
            if (stageController.value.IsOnAtkCircle(_controller) != -1)
            {
                EndAction(true);
                return;
            }

            // 继续等待：维持速度；刷新等待点并导航
            _navMeshAgent.speed = _moveSpeedAttr.CurValue;
            int waitIndex = stageController.value.IsOnWaitCircle(_controller);
            if (waitIndex == -1)
                return;   // 不在等待圈：保持 Running，不结束

            _waitIndex = waitIndex;
            _waitPos = GetWaitPos(_waitIndex);
            if (Vector3.SqrMagnitude(_waitPos - _lastWaitPos) > ignoreDis * ignoreDis)
                _navMeshAgent.SetDestination(_waitPos);
        }

        protected override void OnStop()
        {
            _smoothed?.Dispose();
            _smoothed = null;
            _controller.LockTarget(false);
        }

        private Vector3 GetWaitPos(int waitIndex)
        {
            return stageController.value.kungFuCircle.waitPosArr[waitIndex];
        }
    }
}