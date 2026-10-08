using System.Collections.Generic;
using Character;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using NodeCanvas.StateMachines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 生成敌人的 AI 结构:**外层状态机 + 状态内嵌行为树**。
///   1) 战斗行为树 → 资产 <c>Assets/Config/Enemy/Enemy_CombatBT.asset</c>
///      Repeat(Forever)
///      └─ Selector(dynamic)                          发现玩家? 外层分流
///         ├─ Sequencer(dynamic)                       发现了 → 进入战斗
///         │  ├─ 检测玩家 (FindTargetCondition)        扇形检测 + 视线,写入黑板 tareget 与控制器 Target
///         │  └─ Selector(dynamic)                     远 → 追 / 近 → 战斗行为
///         │     ├─ Sequencer(dynamic)                 远:追击
///         │     │  ├─ 检查目标距离(距离 > 追击距离)
///         │     │  └─ 追击玩家 (MoveToTarget)
///         │     └─ Selector(dynamic)                  近:放技能 / 转身 / 盯人 / 环绕
///         │        ├─ Sequencer(dynamic)              ① 放技能(距离够 + 朝向达标才出手)
///         │        │  ├─ 检查目标距离 / 检查目标角度(硬守卫) / 技能就绪 / 间隔+概率
///         │        │  └─ 释放技能 (Enemy_CastSkill)
///         │        ├─ Sequencer(dynamic)              ①.5 转身(距离够但没面向 → 播转身动画)
///         │        │  └─ 检查目标距离 / 检查目标角度(需转向) / 转身 (TurnToTarget)
///         │        ├─ Cooldown → 盯着目标            ② 站住朝向玩家(做完休息一段时间)
///         │        └─ Cooldown → 环绕目标            ③ 绕玩家走位(做完休息一段时间)
///         └─ 原地待机 (Enemy_Idle)                    没发现玩家 → 站着播待机
///   2) 敌人状态机(内嵌在 Enemy.prefab 的 FSMOwner 上)
///      NestedBTState(初始,"子行为树") ← 引用上面那棵树;进入自动跑、退出按 exitMode 停/暂停
///      Enemy_ControlledState         ← 受控(眩晕/受击等失去控制):停移动/打断技能,时长到后 Finish 回战斗状态
///      连线:Controlled --OnFinish--> 子行为树
/// 黑板变量按名字绑定:enemyController(EnemyController)、tareget(Transform→控制器 Target)、
/// frontTargetCatcher/eyeOffset/obstacleLayer(检测与视线)、stopChaseDis(追击距离 = 进入近身战斗的距离)。
/// 变量定义在预制体的 Blackboard 组件上,状态机与子行为树共用这一份(子图通过 parentBlackboard 继承)。
/// 说明:黑板里的值不会被本工具重置,想调"检测/视线/距离"请改黑板;节点上的字面量(技能ID、间隔、概率、时长等)
/// 由下面的常量给出,改常量后重跑本工具即可。
/// </summary>
public static class EnemyBehaviourTreeCreator
{
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemy/Enemy.prefab";
    /// <summary>战斗行为树资产路径(由 FSM 的战斗状态以子图方式引用,敌人预制体上不再需要 BehaviourTreeOwner)</summary>
    private const string BehaviourTreeAssetPath = "Assets/Config/Enemy/Enemy_CombatBT.asset";
    private const string EnemyControllerVariable = "enemyController";
    private const string TargetVariable = "tareget";
    // 以下参数绑定到预制体黑板上的变量(黑板里的值不会被本工具重置,调参请在黑板里调)
    private const string FrontTargetCatcherVariable = "frontTargetCatcher";   // 前方扇形检测(发现玩家)
    private const string EyeOffsetVariable = "eyeOffset";                     // 视线检测起点
    private const string ObstacleLayerVariable = "obstacleLayer";             // 遮挡层(视线被墙挡住)
    private const string ChaseDistanceVariable = "stopChaseDis";              // 追击停止距离 = 进入近身战斗行为的距离

    // 生成时的默认参数(节点上的字面量;改这里再重跑生成即可)
    private const int DefaultSkillID = 0;           // 用技能列表里的第 0 个技能
    private const float CastMaxAngle = 45f;         // 放技能的朝向容差(度)
    private const float CastInterval = 3f;          // 每隔多少秒尝试一次放技能
    private const float CastChance = 0.6f;          // 每次尝试的释放概率
    private const float FaceDuration = 1.5f;        // 盯着玩家持续时长
    private const float FaceRestTime = 4f;          // 盯完之后的间隔(间隔内换别的行为)
    private const float CircleRadius = 3f;          // 环绕半径
    private const float CircleSpeed = 2.5f;         // 环绕速度
    private const float CircleDuration = 3f;        // 环绕持续时长
    private const float CircleRestTime = 8f;        // 环绕完之后的间隔
    private const float ChaseHysteresis = 0.4f;     // 追击守卫的迟滞(米):比停止距离多这么多才重新开始追,避免边界抽搐

    [MenuItem("Tools/Enemy/Build Enemy Behaviour Tree", false, 0)]
    public static void BuildEnemyBehaviourTree()
    {
        // 1) 先把战斗行为树做成资产(FSM 的子图必须是资产)
        BehaviourTree behaviourTreeAsset = BuildBehaviourTreeAsset();

        // 2) 再把"FSM(战斗状态里嵌套这棵行为树) + 受击状态"生成/刷新到预制体与场景敌人
        BuildForPrefab(behaviourTreeAsset);
        BuildForOpenScenes(behaviourTreeAsset);
        AssetDatabase.SaveAssets();
    }

    /// <summary>创建/刷新战斗行为树资产(已存在就覆盖内容,保持 GUID 与引用不变)。</summary>
    private static BehaviourTree BuildBehaviourTreeAsset()
    {
        BehaviourTree newTree = BuildTree();
        // 资产主对象名要和文件名一致,否则 Unity 会提示 "Main Object Name does not match filename"
        newTree.name = System.IO.Path.GetFileNameWithoutExtension(BehaviourTreeAssetPath);

        BehaviourTree asset = AssetDatabase.LoadAssetAtPath<BehaviourTree>(BehaviourTreeAssetPath);

        if (asset == null)
        {
            AssetDatabase.CreateAsset(newTree, BehaviourTreeAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"已创建战斗行为树资产:{BehaviourTreeAssetPath}");
            return newTree;
        }

        // 覆盖前把旧树里 TurnToTarget 节点上已填好的转身动画名字拷到新节点(生成器整体覆盖资产,不拷会丢)
        CopyTurnTransitions(asset, newTree);

        EditorUtility.CopySerialized(newTree, asset);
        asset.name = newTree.name;
        Object.DestroyImmediate(newTree);
        EditorUtility.SetDirty(asset);
        Debug.Log($"已刷新战斗行为树资产:{BehaviourTreeAssetPath}");
        return asset;
    }

    /// <summary>
    /// 把已存在树里 <see cref="TurnToTarget"/> 节点上填好的转身动画名字拷到新树对应节点。
    /// 生成器用 CopySerialized 整体覆盖资产,用户在节点上填的字符串会在重跑时丢失,这里按出现顺序回填。
    /// </summary>
    private static void CopyTurnTransitions(BehaviourTree source, BehaviourTree target)
    {
        if (source == null) return;

        List<TurnToTarget> oldTurns = new();
        for (int i = 0; i < source.allNodes.Count; i++)
        {
            if (source.allNodes[i] is ActionNode actionNode && actionNode.action is TurnToTarget oldTurn)
                oldTurns.Add(oldTurn);
        }

        if (oldTurns.Count == 0) return;

        int index = 0;
        for (int i = 0; i < target.allNodes.Count; i++)
        {
            if (target.allNodes[i] is not ActionNode actionNode || actionNode.action is not TurnToTarget newTurn)
                continue;

            TurnToTarget oldTurn = oldTurns[Mathf.Min(index, oldTurns.Count - 1)];
            newTurn.turn90L.value = oldTurn.turn90L?.value;
            newTurn.turn90R.value = oldTurn.turn90R?.value;
            newTurn.turn180L.value = oldTurn.turn180L?.value;
            newTurn.turn180R.value = oldTurn.turn180R?.value;
            index++;
        }
    }

    /// <summary>生成到预制体。</summary>
    private static void BuildForPrefab(BehaviourTree behaviourTreeAsset)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"找不到敌人预制体:{EnemyPrefabPath}");
            return;
        }

        try
        {
            ApplyStateMachine(prefabRoot, behaviourTreeAsset);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            Debug.Log($"敌人状态机(战斗状态内嵌行为树)已生成到预制体:{EnemyPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    /// <summary>生成到当前打开场景里所有敌人(场景里的敌人往往是独立副本,不吃预制体改动)。</summary>
    private static void BuildForOpenScenes(BehaviourTree behaviourTreeAsset)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
            {
                EnemyController[] enemies = roots[j].GetComponentsInChildren<EnemyController>(true);
                for (int k = 0; k < enemies.Length; k++)
                {
                    ApplyStateMachine(enemies[k].gameObject, behaviourTreeAsset);
                    count++;
                }
            }

            if (count == 0) continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"敌人状态机(战斗状态内嵌行为树)已生成到场景 {scene.name} 的 {count} 个敌人");
        }
    }

    /// <summary>
    /// 生成/刷新敌人的外层状态机:同物体上的 FSMOwner(没有就加上),
    /// 战斗状态用 NestedBTState(子行为树)引用行为树资产,受控状态用 Enemy_ControlledState;
    /// 状态机与行为树共用预制体上的同一个黑板。
    /// 另外会移除旧的 BehaviourTreeOwner 组件(行为树已经作为子图挂在状态机里,留着它会重复跑一遍)。
    /// </summary>
    private static void ApplyStateMachine(GameObject enemyRoot, BehaviourTree behaviourTreeAsset)
    {
        BehaviourTreeOwner oldTreeOwner = enemyRoot.GetComponent<BehaviourTreeOwner>();
        if (oldTreeOwner != null)
        {
            Debug.Log($"移除 {enemyRoot.name} 上多余的 BehaviourTreeOwner(行为树已改为状态机的子图)");
            Object.DestroyImmediate(oldTreeOwner);
        }

        FSMOwner fsmOwner = enemyRoot.GetComponent<FSMOwner>();
        if (fsmOwner == null) fsmOwner = enemyRoot.AddComponent<FSMOwner>();

        // 状态机用预制体上的 Blackboard 组件:子行为树会通过 graph.blackboard.parent 继承它,变量只配一份
        Blackboard blackboard = enemyRoot.GetComponent<Blackboard>();
        if (blackboard != null) fsmOwner.blackboard = blackboard;

        FSM fsm = BuildStateMachine(behaviourTreeAsset);
        fsmOwner.SetBoundGraphReference(fsm);
        Object.DestroyImmediate(fsm);

        EditorUtility.SetDirty(fsmOwner);
    }

    /// <summary>
    /// 构建敌人状态机:
    ///   NestedBTState(初始,子行为树)  ← 受控结束后 OnFinish 回来;状态进入时自动启动子行为树、退出时停/暂停它
    ///   Enemy_ControlledState          ← 动画 Cue / 外部 Fsm.TriggerState("Enemy_ControlledState") 切入
    /// 硬状态(战斗/受控)由状态机管,"战斗里怎么打"由内嵌的行为树管。
    /// </summary>
    private static FSM BuildStateMachine(BehaviourTree behaviourTreeAsset)
    {
        FSM fsm = ScriptableObject.CreateInstance<FSM>();
        fsm.name = nameof(FSM);

        // 战斗状态 = 子行为树:进入自动跑行为树,退出按 exitMode 处理(默认 StopAndRestart:受控后回来重新决策)
        NestedBTState combatState = fsm.AddNode<NestedBTState>(new Vector2(20f, 40f));
        combatState.subGraph = behaviourTreeAsset;
        combatState.executionMode = NestedBTState.BTExecutionMode.Repeat;
        combatState.exitMode = NestedBTState.BTExitMode.StopAndRestart;

        Enemy_ControlledState controlledState = fsm.AddNode<Enemy_ControlledState>(new Vector2(-280f, 200f));
        controlledState.transitionEvaluation = FSMState.TransitionEvaluationMode.CheckAfterStateFinished;

        // 受控结束(Finish) → 回战斗状态;无条件的连线就是"OnFinish"
        fsm.ConnectNodes(controlledState, combatState);

        fsm.primeNode = combatState;
        return fsm;
    }

    private static BehaviourTree BuildTree()
    {
        BehaviourTree tree = ScriptableObject.CreateInstance<BehaviourTree>();
        tree.name = nameof(BehaviourTree);

        #region 骨架节点

        Repeater repeat = tree.AddNode<Repeater>(new Vector2(0f, 0f));
        repeat.repeaterMode = Repeater.RepeaterMode.RepeatForever;

        Selector rootSelector = tree.AddNode<Selector>(new Vector2(40f, 120f));
        rootSelector.dynamic = true;

        Sequencer foundSequencer = tree.AddNode<Sequencer>(new Vector2(-300f, 240f));
        foundSequencer.dynamic = true;

        Selector fightSelector = tree.AddNode<Selector>(new Vector2(-260f, 480f));
        fightSelector.dynamic = true;

        Sequencer chaseSequencer = tree.AddNode<Sequencer>(new Vector2(-560f, 600f));
        chaseSequencer.dynamic = true;

        Selector combatSelector = tree.AddNode<Selector>(new Vector2(60f, 600f));
        combatSelector.dynamic = true;

        Sequencer castSequencer = tree.AddNode<Sequencer>(new Vector2(-220f, 720f));
        castSequencer.dynamic = true;

        #endregion

        #region 检测玩家

        ConditionNode findTargetNode = tree.AddNode<ConditionNode>(new Vector2(-360f, 360f));
        FindTargetCondition findTarget = Task.Create<FindTargetCondition>(tree);
        findTarget.enemyController.name = EnemyControllerVariable;
        findTarget.target.name = TargetVariable;
        findTarget.targetCatcher.name = FrontTargetCatcherVariable;
        findTarget.eyeOffset.name = EyeOffsetVariable;
        findTarget.obstacleLayer.name = ObstacleLayerVariable;
        findTargetNode.condition = findTarget;

        #endregion

        #region 远:追击

        ConditionNode farCheckNode = tree.AddNode<ConditionNode>(new Vector2(-680f, 720f));
        CheckTargetDistance farCheck = Task.Create<CheckTargetDistance>(tree);
        farCheck.target.name = TargetVariable;
        farCheck.checkType = CheckTargetDistance.CheckType.GreaterThan;
        farCheck.minDistance.name = ChaseDistanceVariable; // 距离 > 追击距离 → 需要追上去
        farCheckNode.condition = farCheck;

        ActionNode chaseNode = tree.AddNode<ActionNode>(new Vector2(-480f, 720f));
        MoveToTarget chase = Task.Create<MoveToTarget>(tree);
        chase.enemyController.name = EnemyControllerVariable;
        chase.target.name = TargetVariable;
        chase.stopChaseDis.name = ChaseDistanceVariable;
        chaseNode.action = chase;

        #endregion

        #region 近:① 放技能(近身 → 朝向达标 → Cooldown 控节奏 → 技能就绪 → 概率 → 释放)

        // 距离:复用「检查目标距离」节点(近身判定),放技能不再单独检查技能距离
        ConditionNode inRangeNode = tree.AddNode<ConditionNode>(new Vector2(-560f, 840f));
        CheckTargetDistance inRange = Task.Create<CheckTargetDistance>(tree);
        inRange.target.name = TargetVariable;
        inRange.checkType = CheckTargetDistance.CheckType.LessThanOrEqualTo;
        inRange.maxDistance.name = ChaseDistanceVariable; // 距离 <= 该值 才尝试放技能
        inRangeNode.condition = inRange;

        // 朝向:必须已经面向目标(硬守卫)。不面向 → 本序列失败 → 落到下面的「转身」分支
        ConditionNode castFacingNode = tree.AddNode<ConditionNode>(new Vector2(-420f, 840f));
        CheckTargetAngle castFacing = Task.Create<CheckTargetAngle>(tree);
        castFacing.characterController.name = EnemyControllerVariable;
        castFacing.checkType = CheckTargetAngle.CheckType.LessThanOrEqualTo;
        castFacing.maxAngle.value = CastMaxAngle;
        castFacingNode.condition = castFacing;

        // 节奏:NodeCanvas 自带的 Cooldown 装饰器(一次尝试之后要等这么久才会再试)
        Cooldown castCooldown = tree.AddNode<Cooldown>(new Vector2(-280f, 840f));
        castCooldown.coolDownTime.value = CastInterval;

        // 内层顺序(整串跑完或失败后才开始下一次 Cooldown 计时)
        Sequencer castInnerSequencer = tree.AddNode<Sequencer>(new Vector2(-120f, 840f));
        castInnerSequencer.dynamic = true;

        ConditionNode canCastNode = tree.AddNode<ConditionNode>(new Vector2(-360f, 980f));
        Enemy_CheckCanCast canCast = Task.Create<Enemy_CheckCanCast>(tree);
        canCast.enemyController.name = EnemyControllerVariable;
        canCast.skillID.value = DefaultSkillID;
        canCastNode.condition = canCast;

        ConditionNode chanceNode = tree.AddNode<ConditionNode>(new Vector2(-120f, 980f));
        Enemy_CheckChance chance = Task.Create<Enemy_CheckChance>(tree);
        chance.chance.value = CastChance;
        chanceNode.condition = chance;

        ActionNode castSkillNode = tree.AddNode<ActionNode>(new Vector2(120f, 980f));
        Enemy_CastSkill castSkill = Task.Create<Enemy_CastSkill>(tree);
        castSkill.enemyController.name = EnemyControllerVariable;
        castSkill.skillID.value = DefaultSkillID;
        castSkillNode.action = castSkill;

        #endregion

        #region 近:①.5 转身(距离够但没面向 → 播转身动画/快速摆正)

        Sequencer turnSequencer = tree.AddNode<Sequencer>(new Vector2(-120f, 620f));
        turnSequencer.dynamic = true;

        ConditionNode turnRangeNode = tree.AddNode<ConditionNode>(new Vector2(-360f, 620f));
        CheckTargetDistance turnRange = Task.Create<CheckTargetDistance>(tree);
        turnRange.target.name = TargetVariable;
        turnRange.checkType = CheckTargetDistance.CheckType.LessThanOrEqualTo;
        turnRange.maxDistance.name = ChaseDistanceVariable; // 在近身范围内才原地转身
        turnRangeNode.condition = turnRange;

        ConditionNode needTurnNode = tree.AddNode<ConditionNode>(new Vector2(-220f, 620f));
        CheckTargetAngle needTurn = Task.Create<CheckTargetAngle>(tree);
        needTurn.characterController.name = EnemyControllerVariable;
        needTurn.checkType = CheckTargetAngle.CheckType.GreaterThan;
        needTurn.maxAngle.value = CastMaxAngle;
        needTurnNode.condition = needTurn;

        ActionNode turnNode = tree.AddNode<ActionNode>(new Vector2(-40f, 620f));
        TurnToTarget turn = Task.Create<TurnToTarget>(tree);
        turn.characterController.name = EnemyControllerVariable;
        turn.angleTolerance.value = CastMaxAngle;
        turnNode.action = turn;

        #endregion

        #region 近:② 盯着玩家(做完休息 FaceRestTime 秒)

        Cooldown faceCooldown = tree.AddNode<Cooldown>(new Vector2(120f, 720f));
        faceCooldown.coolDownTime.value = FaceRestTime;

        ActionNode faceNode = tree.AddNode<ActionNode>(new Vector2(120f, 840f));
        Enemy_FaceTarget faceTarget = Task.Create<Enemy_FaceTarget>(tree);
        faceTarget.enemyController.name = EnemyControllerVariable;
        faceTarget.duration.value = FaceDuration;
        faceNode.action = faceTarget;

        #endregion

        #region 近:③ 环绕玩家(做完休息 CircleRestTime 秒)

        Cooldown circleCooldown = tree.AddNode<Cooldown>(new Vector2(340f, 720f));
        circleCooldown.coolDownTime.value = CircleRestTime;

        ActionNode circleNode = tree.AddNode<ActionNode>(new Vector2(340f, 840f));
        Enemy_CircleTarget circle = Task.Create<Enemy_CircleTarget>(tree);
        circle.enemyController.name = EnemyControllerVariable;
        circle.radius.value = CircleRadius;
        circle.speed.value = CircleSpeed;
        circle.duration.value = CircleDuration;
        circleNode.action = circle;

        #endregion

        #region 没发现玩家:待机

        ActionNode idleNode = tree.AddNode<ActionNode>(new Vector2(200f, 240f));
        Enemy_Idle idle = Task.Create<Enemy_Idle>(tree);
        idle.enemyController.name = EnemyControllerVariable;
        idleNode.action = idle;

        #endregion

        #region 连线

        tree.ConnectNodes(repeat, rootSelector);
        tree.ConnectNodes(rootSelector, foundSequencer);
        tree.ConnectNodes(rootSelector, idleNode);

        tree.ConnectNodes(foundSequencer, findTargetNode);
        tree.ConnectNodes(foundSequencer, fightSelector);

        tree.ConnectNodes(fightSelector, chaseSequencer);
        tree.ConnectNodes(fightSelector, combatSelector);

        tree.ConnectNodes(chaseSequencer, farCheckNode);
        tree.ConnectNodes(chaseSequencer, chaseNode);

        tree.ConnectNodes(combatSelector, castSequencer);
        tree.ConnectNodes(combatSelector, turnSequencer);
        tree.ConnectNodes(combatSelector, faceCooldown);
        tree.ConnectNodes(combatSelector, circleCooldown);

        // ① 放技能:距离 → 朝向达标 → Cooldown 控节奏 → [技能就绪 → 概率 → 释放]
        tree.ConnectNodes(castSequencer, inRangeNode);
        tree.ConnectNodes(castSequencer, castFacingNode);
        tree.ConnectNodes(castSequencer, castCooldown);
        tree.ConnectNodes(castCooldown, castInnerSequencer);
        tree.ConnectNodes(castInnerSequencer, canCastNode);
        tree.ConnectNodes(castInnerSequencer, chanceNode);
        tree.ConnectNodes(castInnerSequencer, castSkillNode);

        // ①.5 转身:距离够 + 朝向不达标 → 转身(转好后 dynamic selector 下一轮走「放技能」)
        tree.ConnectNodes(turnSequencer, turnRangeNode);
        tree.ConnectNodes(turnSequencer, needTurnNode);
        tree.ConnectNodes(turnSequencer, turnNode);

        tree.ConnectNodes(faceCooldown, faceNode);
        tree.ConnectNodes(circleCooldown, circleNode);

        tree.primeNode = repeat;

        #endregion

        return tree;
    }
}
