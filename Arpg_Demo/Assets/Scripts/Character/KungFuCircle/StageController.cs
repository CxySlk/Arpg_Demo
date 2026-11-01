using System.Collections.Generic;
using cfg;
using Data;
using UnityEngine;

namespace Character
{
    public class StageController
    {
        public readonly KungFuCircle kungFuCircle; //功夫圈
        private readonly Transform _circleCenterTrans; //功夫圈的中心（一般为玩家）
        private int _curWeightLimit; //当前剩余的敌人权重
        private int _curAtkLimit; //当前剩余的攻击权重

        private readonly Queue<IKFCEnemy> _requestQueue = new(); //请求发起攻击的敌人队列
        private readonly HashSet<IKFCEnemy> _requestMarks = new(); //标记已进入请求队列的敌人

        private readonly Queue<IKFCEnemy> _waitQueue = new(); //已在等待圈中的敌人队列
        private readonly Dictionary<IKFCEnemy, int> _posIndexTable = new(); //占据等待圈的敌人对应位置列表下标

        private readonly Dictionary<IKFCEnemy, int> _atkIndexTable = new(); //发起攻击敌人所用的对应攻击列表下标

        // <kfcEnemy的配置ID，KFC运行时>
        public readonly Dictionary<int, KFC_EnemyRuntime> runtimes = new();


        public StageController(KfcConfig config, Transform circleCenterTrans)
        {
            _curWeightLimit = config.WeightLimit;
            _curAtkLimit = config.AtkLimit;
            _circleCenterTrans = circleCenterTrans;
            kungFuCircle = new(config);
        }

        /// <summary>
        /// 根据敌人ID获取KFCEnemy运行时
        /// </summary>
        /// <param name="kfcEnemyID">kfc的配置ID</param>
        /// <returns></returns>
        public KFC_EnemyRuntime GetKfcEnemyRuntime(int kfcEnemyID)
        {
            if (!runtimes.TryGetValue(kfcEnemyID, out KFC_EnemyRuntime runtime))
            {
                KFC_EnemyConfig kfcEnemyConfig = ConfigManager.Tables.TbKFCEnemyConfig.Get(kfcEnemyID);
                runtime = new(kfcEnemyConfig);
                runtimes.Add(kfcEnemyID, runtime);
            }

            return runtime;
        }

        /// <summary>
        /// 发起攻击请求
        /// </summary>
        /// <param name="enemy">发起攻击的敌人</param>
        public void RequestAttack(IKFCEnemy enemy)
        {
            if (_requestMarks.Add(enemy))
                _requestQueue.Enqueue(enemy);
        }

        /// <summary>
        /// 为已在等待圈中的敌人重新分配位置
        /// </summary>
        /// <param name="enemyOnWait"></param>
        public void ReSetWaitPos(IKFCEnemy enemyOnWait)
        {
            kungFuCircle.waitPosFlags[_posIndexTable[enemyOnWait]] = false;
            int res = kungFuCircle.GetClosestPosIndex(enemyOnWait.selfTrans.position);
            if (res > -1)
            {
                _posIndexTable[enemyOnWait] = res;
                kungFuCircle.waitPosFlags[res] = true;
            }
        }

        /// <summary>
        /// 遍历请求队列，符合条件的转入等待队列
        /// </summary>
        private void Request2Wait()
        {
            int length = _requestQueue.Count;
            for (int i = 0; i < length; ++i)
            {
                IKFCEnemy cur = _requestQueue.Dequeue();

                if (!_requestMarks.Contains(cur))
                    continue;

                if (TryGetWaitPos(cur))
                {
                    _requestMarks.Remove(cur);
                    _waitQueue.Enqueue(cur);
                }
                else _requestQueue.Enqueue(cur);
            }
        }

        /// <summary>
        /// 尝试为敌人分配等待圈点位
        /// </summary>
        /// <param name="enemy">当前敌人</param>
        /// <returns>是否分配成功</returns>
        private bool TryGetWaitPos(IKFCEnemy enemy)
        {
            if (enemy.kfcEnemyConfig.SelfWeight <= _curWeightLimit)
            {
                int res = kungFuCircle.GetClosestPosIndex(enemy.selfTrans.position);
                if (res > -1)
                {
                    _curWeightLimit -= enemy.kfcEnemyConfig.SelfWeight;
                    _posIndexTable[enemy] = res;
                    kungFuCircle.waitPosFlags[res] = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 释放攻击权重
        /// </summary>
        /// <param name="enemy"></param>
        public void ReleaseAttackWeight(IKFCEnemy enemy)
        {
            if (_atkIndexTable.TryGetValue(enemy, out int res))
            {
                _curAtkLimit += enemy.kfcEnemyConfig.AtkWeightTalbe[res];
                _atkIndexTable.Remove(enemy);
            }
        }

        /// <summary>
        /// 遍历等待队列，符合条件的转入攻击记录
        /// </summary>
        private void Wait2Attack()
        {
            int count = _waitQueue.Count;
            for (int i = 0; i < count && _curAtkLimit > 0; i++)
            {
                IKFCEnemy cur = _waitQueue.Dequeue();
                // 已经选了攻击方式，但是还没攻击
                if (_atkIndexTable.ContainsKey(cur))
                {
                    _waitQueue.Enqueue(cur);
                    continue;
                }

                if (_posIndexTable.ContainsKey(cur))
                {
                    if (TryAttack(cur, out int res))
                    {
                        _atkIndexTable.Add(cur, res);
                        _waitQueue.Enqueue(cur);
                    }
                    else
                    {
                        _waitQueue.Enqueue(cur);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试进行攻击
        /// </summary>
        /// <param name="enemy">当前敌人</param>
        /// <returns>返回攻击类型的下标（返回-1表示未能攻击）</returns>
        private bool TryAttack(IKFCEnemy enemy, out int res)
        {
            KFC_EnemyConfig enemyConfig = enemy.kfcEnemyConfig;
            int count = enemyConfig.AtkWeightTalbe.Count;
            for (int i = 0; i < count; i++)
            {
                KFC_EnemyRuntime runtime = runtimes[enemyConfig.Id];
                // 检查攻击权重限制、CD时间
                if (enemyConfig.AtkWeightTalbe[i] <= _curAtkLimit &&
                    enemyConfig.AtkCDTable[i] <= Time.time - runtime.lastAtkTimes[i])
                {
                    if (--runtime.curUseTable[i] == 0)
                    {
                        // 如果可用次数耗尽，就进入冷却并重置次数以便下次使用
                        runtime.lastAtkTimes[i] = Time.time;
                        runtime.curUseTable[i] = enemyConfig.UseTimesTable[i];
                    }

                    _curAtkLimit -= enemyConfig.AtkWeightTalbe[i];
                    res = i;
                    return true;
                }
            }

            res = -1;
            return false;
        }

        /// <summary>
        /// 更新功夫圈的点位（可在FixedUpdate中调用）
        /// </summary>
        public void UpdateCirclePos()
        {
            kungFuCircle.PlaceCirclePos(_circleCenterTrans.position, kungFuCircle.WaitRadius, kungFuCircle.waitPosArr);
        }

        /// <summary>
        /// 更新攻击请求与实际攻击（可在LateUpdate中调用）
        /// </summary>
        public void UpdateRequest()
        {
            Request2Wait();
            Wait2Attack();
        }

        /// <summary>
        /// 一并返还占据的位置权重与攻击权重（适用于敌人自身死亡、失去攻击欲望等）
        /// </summary>
        /// <param name="enemy">移除权重的敌人</param>
        public void ReleaseAllWeight(IKFCEnemy enemy)
        {
            _requestMarks.Remove(enemy);
            if (_posIndexTable.TryGetValue(enemy, out int res))
            {
                kungFuCircle.waitPosFlags[res] = false;
                _curWeightLimit += enemy.kfcEnemyConfig.SelfWeight;
                _posIndexTable.Remove(enemy);
            }

            ReleaseAttackWeight(enemy);
        }

        /// <summary>
        /// 查询敌人是否在等待圈中（获得了等待攻击的机会）
        /// </summary>
        /// <param name="enemy">查询的敌人</param>
        /// <returns>如果是，返回等待圈位置下标；否则返回-1</returns>
        public int IsOnWaitCircle(IKFCEnemy enemy)
        {
            return _posIndexTable.TryGetValue(enemy, out int res) ? res : -1;
        }

        /// <summary>
        /// 查询敌人是否在攻击圈中（获得了攻击许可）
        /// </summary>
        /// <param name="enemy">查询的敌人</param>
        /// <returns>如果是，返回攻击类型下标；否则返回-1</returns>
        public int IsOnAtkCircle(IKFCEnemy enemy)
        {
            return _atkIndexTable.GetValueOrDefault(enemy, -1);
        }

        /// <summary>
        /// 根据敌人当前位置，获取圈外徘徊点
        /// </summary>
        /// <param name="curPos">敌人当前位置</param>
        /// <param name="patrolSpeed">徘徊速度</param>
        /// <param name="res">返回的徘徊点位置</param>
        public void GetOutCirclePatrolPos(Vector3 curPos, float patrolSpeed, out Vector3 res)
        {
            res = kungFuCircle.CirclePatrol(curPos, _circleCenterTrans.position, patrolSpeed);
        }

        /// <summary>
        /// 让敌人靠近攻击圈
        /// </summary>
        /// <param name="curPos">敌人当前位置</param>
        /// <param name="res">返回的目标攻击点位置</param>
        public void GetAtkCirclePos(Vector3 curPos, float atkRadius, out Vector3 res)
        {
            Vector3 dir = (curPos - _circleCenterTrans.position).normalized;
            res = _circleCenterTrans.position + dir * atkRadius;
        }
    }
}