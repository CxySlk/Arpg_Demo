using System.Collections.Generic;
using cfg;
using Character;
using Data;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Action
{
    [Category("KFC")]
    [Name("KFC 攻击")]
    public class KFC_AttackAction : ActionTask
    {
        public BBParameter<StageController> stageController;

        [Name("到达停止距离")] public float stopDis = 0.1f;
        [Name("目标刷新阈值")] public float ignoreDis = 0.1f;
        [Name("退出时释放全部权重")] public bool releaseAllOnExit = true;

        private EnemyController _controller;
        private NavMeshAgent _navMeshAgent;
        private Attribute _moveSpeedAttr;
        private int _skillID;
        private int _segmentIndex;
        private bool _moving;          // true=走位, false=等当前段播完
        private bool _casted;          // 已出过招（释放判定用）
        private bool _forceReleaseAll; // 因不能释放而退出 → 强制释放全部
        private bool _moveAnimPlaying;
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
            int atkIndex = stageController.value.IsOnAtkCircle(_controller);
            if (atkIndex < 0) { EndAction(false); return; }

            List<int> ownedSkills = _controller.EnemyConfig.OwnedSkills;
            if (atkIndex >= ownedSkills.Count)
            {
                Debug.LogError($"[KFC_AttackAction] 攻击下标 {atkIndex} 超出 OwnedSkills 数量 {ownedSkills.Count}");
                _forceReleaseAll = true;
                EndAction(false);
                return;
            }
            _skillID = ownedSkills[atkIndex];

            if (!_controller.CanCastSkill(_skillID))
            {
                _forceReleaseAll = true;   // 不能释放 → 退出强制释放全部
                EndAction(false);
                return;
            }

            _navMeshAgent.updateRotation = true;   // 交给 NavMesh 自动转向
            _navMeshAgent.stoppingDistance = stopDis;
            _navMeshAgent.speed = _moveSpeedAttr.CurValue;

            _segmentIndex = 0;
            _moving = true;
            _casted = false;
            _forceReleaseAll = false;
            _moveAnimPlaying = false;
            _hasDest = false;
        }

        protected override void OnUpdate()
        {
            if (_moving)
            {
                // 连段间隙中技能已结束（间隙超时/被打断）→ 收尾
                if (_casted && !_controller.SkillIsPlaying(_skillID))
                {
                    EndAction(true);
                    return;
                }

                float releaseDis = GetReleaseDis(_skillID, _segmentIndex);
                stageController.value.GetAtkCirclePos(_controller.selfTrans.position, releaseDis, out Vector3 target);

                Vector3 toTarget = target - _controller.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= stopDis * stopDis)
                {
                    _controller.CastSkill(_skillID);   // 段0=启动，后续段=续段
                    _casted = true;
                    _moving = false;
                    return;
                }

                // 真正开始走位时才播 Move（已在攻击点则直接出招，不闪一下）
                if (!_moveAnimPlaying)
                    PlayMove();

                if (!_hasDest || Vector3.SqrMagnitude(target - _lastDest) > ignoreDis * ignoreDis)
                {
                    _navMeshAgent.SetDestination(target);
                    _lastDest = target;
                    _hasDest = true;
                }
                return;
            }

            if (_controller.AbilityIsPlaying(_skillID)) return;   // 等当前片段播完

            if (!_controller.SkillIsPlaying(_skillID))            // 整套结束
            {
                EndAction(true);
                return;
            }

            // 还有下一段：按下一段释放距离重新走位，再续段
            _segmentIndex++;
            if (_segmentIndex >= GetSegmentCount(_skillID))
            {
                EndAction(true);
                return;
            }
            _moving = true;
            _moveAnimPlaying = false;
            _hasDest = false;
        }

        protected override void OnResume()
        {
            if (_moving)
            {
                PlayMove();
                _hasDest = false;
            }
        }

        protected override void OnStop()
        {
            if (_forceReleaseAll)
            {
                stageController.value.ReleaseAllWeight(_controller);
                return;
            }
            if (!_casted) return;

            if (releaseAllOnExit) stageController.value.ReleaseAllWeight(_controller);
            else stageController.value.ReleaseAttackWeight(_controller);
        }

        private void PlayMove()
        {
            _controller.CharacterView.Play("Move");
            _controller.SetRootMotion(null);
            _moveAnimPlaying = true;
        }

        private List<float> GetReleaseTable(int skillID) =>
            ConfigManager.Tables.TbSkill.Get(skillID).SkillReleaseDis;

        private int GetSegmentCount(int skillID) => Mathf.Max(1, GetReleaseTable(skillID).Count);

        private float GetReleaseDis(int skillID, int index)
        {
            List<float> table = GetReleaseTable(skillID);
            if (table.Count == 0) return 0f;
            return table[Mathf.Clamp(index, 0, table.Count - 1)];
        }
    }
}
