using System.Collections;
using GAS;
using NodeCanvas.StateMachines;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;
using UnityEngine.AI;

namespace Character
{
    public class EnemyController : CharacterControllerBase
    {
        [LabelText("技能大脑"), SerializeField, Required]
        private Enemy_SkillBrain _skillBrain;

        [LabelText("敌人配置"), SerializeField, Required]
        private EnemyConfig _enemyConfig;

        [LabelText("死亡延迟销毁时间(秒)"), SerializeField]
        private float _destroyDelay = 2.5f;

        [LabelText("导航代理"), SerializeField, Required]
        private NavMeshAgent _navMeshAgent;

        public NavMeshAgent NavMeshAgent => _navMeshAgent;

        // 上次下发的寻路目标点:目标几乎没动就不重新寻路(SetDestination 内部会重算路径)
        private Vector3 _lastDestination;
        private bool _hasDestination;

        // 追击停止距离(由行为树任务下发,= 进入近身战斗行为的距离)
        private float _stopChaseDis;

        public EnemyConfig EnemyConfig => _enemyConfig;

        private void Awake()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();
            asc.InitForPreset(_enemyConfig.AscPreset);
            _skillBrain.InitForConfig(_enemyConfig.SkillConfigList);
            characterView.SetStandardAnimation(_enemyConfig.StandardAnimationDic);
            // 朝向交给 AI 控制(追击时始终面向玩家),不让导航代理的自动旋转和代码抢旋转
            _navMeshAgent.updateRotation = false;
            // 状态机与控制器同物体:外层硬状态(战斗/受击)由它管,行为树只在战斗状态内跑
            Fsm = GetComponent<FSMOwner>();
        }

        /// <summary>敌人状态机(与控制器同物体;没挂时为 null,受击会退化成直接置标记)。</summary>
        public FSMOwner Fsm { get; private set; }

        public override void Move(Vector3 deltaPos)
        {
            if (!_navMeshAgent.enabled) return;
            _navMeshAgent.Move(deltaPos);
        }

        #region AI 能力(行为树任务调用)

        /// <summary>追击目标:超出停止距离时寻路靠近,全程朝向目标。</summary>
        public void ChaseTarget(Transform target)
        {
            if (IsDead || !target) return;

            FaceTarget(target);

            // 已经在停止距离内:站住(不再下寻路),朝向由上面保持
            if (IsTargetInStopDis())
            {
                StopMove();
                return;
            }

            asc.AttrDic.TryGetValue(AttributeType.MoveSpeed, out Attribute moveSpeed);
            _navMeshAgent.speed = moveSpeed!.CurValue;
            Vector3 destination = target.position;
            if (!_hasDestination || (destination - _lastDestination).sqrMagnitude > 0.04f)
            {
                if (_navMeshAgent.SetDestination(destination))
                {
                    _lastDestination = destination;
                    _hasDestination = true;
                }
            }
        }

        /// <summary>放弃追击:停止寻路。</summary>
        public void StopChase()
        {
            if (IsDead) return;
            StopMove();
        }

        /// <summary>原地待机:停下并播放待机动画。</summary>
        public void Idle()
        {
            if (IsDead) return;
            StopMove();
            PlayAnimationIfChanged("Idle");
        }

        /// <summary>站住盯着目标:停止移动 + 始终朝向目标 + 播待机动画。</summary>
        public void StandAndFace(Transform target)
        {
            if (IsDead || !target) return;
            StopMove();
            FaceTarget(target);
            PlayAnimationIfChanged("Idle");
        }

        /// <summary>
        /// 环绕目标:以目标为圆心维持 radius 距离切向绕行(同时保持朝向目标)。
        /// speed 为绝对移动速度(不走 MoveSpeed 属性,方便单独调环绕快慢);direction 为 ±1(顺/逆时针)。
        /// </summary>
        public void CircleTarget(Transform target, float radius, float speed, int direction)
        {
            if (IsDead || !target) return;

            FaceTarget(target);

            Vector3 toSelf = transform.position - target.position;
            toSelf.y = 0f;
            float distance = toSelf.magnitude;
            if (distance <= 0.001f) return;

            Vector3 outward = toSelf / distance;
            // 切向绕圈 + 径向修正(离远了往里收,太近了往外退),合起来就是"绕着目标走"
            Vector3 tangent = Vector3.Cross(Vector3.up, outward) * direction;
            Vector3 radial = outward * (radius - distance);
            Vector3 moveDir = tangent + radial;
            if (moveDir.sqrMagnitude <= 0.0001f) return;

            StopMove(); // 环绕不用寻路,清掉旧路径,位移全部由这里给
            Move(moveDir.normalized * (speed * Time.deltaTime));
            PlayAnimationIfChanged("Move");
        }

        /// <summary>只转向目标(不改移动、不切动画):给"转向目标"这类 action 用。</summary>
        public void FaceTargetOnly(Transform target)
        {
            if (IsDead || !target) return;
            FaceTarget(target);
        }

        /// <summary>自身前方向与目标方向的水平夹角(度);无目标时返回 180。</summary>
        public float GetTargetAngle()
        {
            if (!Target) return 180f;

            Vector3 direction = Target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return 0f;

            return Vector3.Angle(transform.forward, direction);
        }

        /// <summary>技能是否就绪(未在施法 + CD/消耗/释放标记都满足)。</summary>
        public bool CanCastSkill(int skillID)
        {
            if (IsDead || !_skillBrain) return false;
            return _skillBrain.CheckCanPlaySkill(skillID);
        }

        /// <summary>释放技能:停下移动 + 朝向目标 + 交给技能大脑播放。</summary>
        public void CastSkill(int skillID)
        {
            if (IsDead || !_skillBrain) return;

            StopMove();
            if (Target) FaceTarget(Target);
            _skillBrain.PlaySkill(skillID);
        }

        /// <summary>指定技能的"整套连段"是否正在播放(基类契约;全部片段播完/续段间隙超时/被打断后为 false)。</summary>
        public override bool SkillIsPlaying(int skillID) => _skillBrain.SkillIsPlaying(skillID);

        /// <summary>打断所有正在播放的技能(技能状态与片段状态一起结束)。</summary>
        public override void InterruptAllSkills(bool resetCd = true, bool force = false) =>
            _skillBrain.InterruptAllSkills(resetCd, force);

        /// <summary>打断指定技能。</summary>
        public override void InterruptSkill(int skillID, bool resetCd = true, bool force = false) =>
            _skillBrain.InterruptSkill(skillID, resetCd, force);

        #endregion

        #region 受控（眩晕/受击等"失去控制"的表现：由动画 Cue 驱动 → 外层状态机切到受控状态）

        /// <summary>技能前摇霸体:吃伤害但不进受控(也不打断技能)。</summary>
        protected override bool CanEnterControlled() => !_skillBrain.IsAnySkillUninterruptible;

        /// <summary>受控开始:停寻路、打断所有技能、清掉旧动画的根运动回调。</summary>
        protected override void OnControlledEnter()
        {
            StopChase();                         // 停寻路
            InterruptAllSkills();                // 打断所有技能(技能状态与片段状态一起结束)
            characterView.SetRootMotion(null);   // 受控期间不接受旧动画的根运动回调
        }

        /// <summary>受控结束:回到标准待机表现(战斗状态里的行为树会重新接管)。</summary>
        protected override void OnControlledExit() => PlayAnimationIfChanged("Idle");

        /// <summary>切到外层状态机的受控状态(里面的行为树随之停下)。</summary>
        protected override void TriggerControlledState() => Fsm?.TriggerState(Enemy_ControlledState.StateName);

        #endregion

        private void FaceTarget(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation,
                360 * Time.deltaTime);
        }

        private void StopMove()
        {
            _hasDestination = false;
            if (_navMeshAgent.enabled && _navMeshAgent.hasPath)
                _navMeshAgent.ResetPath();
            PlayAnimationIfChanged("Idle");
        }

        /// <summary>同名动画不重复播放(避免每帧切动画打断过渡/狂建动画节点)。</summary>
        private void PlayAnimationIfChanged(string animationName)
        {
            if (characterView.CurrentAnimationName == animationName) return;
            characterView.Play(animationName);
        }

        protected override void OnDeathStart()
        {
            // 停掉外层状态机(战斗状态退出 → 行为树一起停),死亡表现由控制器负责
            Fsm.StopBehaviour();

            // 死亡后不再参与碰撞与位移(导航代理关闭,防止尸体被推挤/继续移动)
            _navMeshAgent.enabled = false;

            // 播死亡动画结束后延迟销毁,尸体短暂停留后清除
            StartCoroutine(WaitAndDestroy());
        }

        private IEnumerator WaitAndDestroy()
        {
            yield return new WaitForSeconds(_destroyDelay);
            Destroy(gameObject);
        }

        public void SetStopChaseDis(float stopDis)
        {
            _stopChaseDis = stopDis;
            _navMeshAgent.stoppingDistance = stopDis;
        }

        /// <summary>
        /// 目标是否已经进入"追击停止距离"内(水平距离,忽略高度差)。
        /// 行为树的追击守卫条件与 MoveToTarget 的"到达"判定都用这个,保证两边一致 ——
        /// 否则会出现"寻路判定到了、直线判定还没到"来回打架,导致追击节点每帧被重置(角色抽搐)。
        /// </summary>
        public bool IsTargetInStopDis()
        {
            if (!Target) return false;

            Vector3 offset = Target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _stopChaseDis * _stopChaseDis;
        }
    }
}