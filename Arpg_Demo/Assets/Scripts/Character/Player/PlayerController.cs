using System.Collections.Generic;
using Sirenix.OdinInspector;
using Character;
using GAS;
using LLKFrame;
using NodeCanvas.StateMachines;
using Skill;
using Skill.Data;
using UnityEngine;

public class PlayerController : CharacterControllerBase
{
    #region 组件相关

    [LabelText("角色控制器"), SerializeField] protected CharacterController characterController;
    [LabelText("FSM"), SerializeField] protected FSMOwner fsm;
    [LabelText("技能大脑"), SerializeField] protected Player_SkillBrain skillBrain;
    [LabelText("技能按键配置"), SerializeField, ListDrawerSettings(AddCopiesLastElement = true)]
    protected List<InputNodeBase> inputNodes = new();

    private static PlayerController _instance;
    public static PlayerController Instance => _instance;
    private Dictionary<int, int> EquipSlotDic => DataManager.PlayerData.equipSlotDic;

    #endregion

    #region 敌人搜寻相关

    [LabelText("目标最远的距离"), SerializeField] private float _targetMaxDistance = 10f;

    [LabelText("搜寻点的中心位置"), SerializeField]
    private Vector3 _searchPosOffset = new(0, .75f, .5f);

    private TargetSectorCatcher _targetCatcher;

    #endregion

    // 玩家配置
    public PlayerConfig PlayerConfig { get; private set; }
    public FSMOwner Fsm => fsm;
    public Player_SkillBrain SkillBrain => skillBrain;

    // 出生点(死亡复活回这里;在 Start 记录)
    private Vector3 _spawnPos;
    private Quaternion _spawnRot;

    private void Awake()
    {
        _instance = this;
        Init();
    }

    protected override void Init()
    {
        base.Init();
        _targetCatcher = new()
        {
            Angle = 360,
            centerPos = _searchPosOffset,
            Height = 1.5f,
            InsideRadius = 0,
            OutsideRadius = _targetMaxDistance,
            Rotation = default,
            LayerMask = 1 << LayerMask.NameToLayer("Enemy"),
        };
    }

    private void Start()
    {
        // 记录出生点(复活时回到这里)
        _spawnPos = transform.position;
        _spawnRot = transform.rotation;
    }

    public override void LockTarget(bool isLock)
    {
        base.LockTarget(isLock);
        if (!isLock || TargetIsLegal(Target)) return;
        SearchTarget();
    }

    public void RefreshLockTarget()
    {
        SearchTarget();
    }

    // 判断目标是否合法
    private bool TargetIsLegal(Transform target)
    {
        // 1、目标存在
        // 2、与目标之间的距离在
        return target &&
               (target.position - transform.position).sqrMagnitude <= Mathf.Pow(_targetMaxDistance, 2) &&
               CheckHasObstacle(target);
    }

    private bool CheckHasObstacle(Transform target)
    {
        return !Physics.Linecast(transform.TransformPoint(_searchPosOffset), target.position + Vector3.up,
            1 << LayerMask.NameToLayer("Env"));
    }

    private void SearchTarget()
    {
        Collider[] targets = _targetCatcher.GetTargets(transform);
        Transform tempTarget = null;
        float sqrMaxDis = float.MaxValue;
        for (int i = 0; i < targets.Length && targets[i]; i++)
        {
            Vector3 workspace = targets[i].transform.position;
            float sqrDic = (workspace - transform.position).sqrMagnitude;
            if (sqrDic < sqrMaxDis && TargetIsLegal(targets[i].transform))
                tempTarget = targets[i].transform;
        }

        SetTarget(tempTarget);
    }

    protected override void OnEnable()
    {
        // 必须先调基类:基类 OnEnable 里订阅了 HP 变化(死亡检测依赖它)
        base.OnEnable();
        TimeSystem.Global.SubscribeTimeRunningState(OnTimeStateChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        TimeSystem.Global.UnSubscribeTimeRunningState(OnTimeStateChanged);
    }

    protected override void Update()
    {
        base.Update();
        OnInputCheckUpdate();
        OnInputInterruptCheckUpdate();
    }

    public void SetConfig(PlayerConfig playerConfig, SkillLearnData skillLearnData)
    {
        PlayerConfig = playerConfig;
        characterView.SetStandardAnimation(playerConfig.StandardAnimationDic);
        asc.InitForPreset(playerConfig.AscPreset);
        // asc 现在才有 Hp 属性,补绑定死亡监听(OnEnable 时 asc 尚未初始化)
        BindHpMonitor();
        skillBrain.InitForConfig(playerConfig.SkillConfigList, skillLearnData);

        // 学习默认的技能
        foreach (int id in playerConfig.DefaultLearnSkills)
            skillBrain.LearnSkill(id);

        // 开启技能输入检测
        InitSkillInputNodes();
        StartSkillInputCheck();
    }

    private void OnTimeStateChanged(bool isRunning)
    {
        if (isRunning) StartSkillInputCheck();
        else StopSkillInputCheck();
    }

    #region 技能输入按键相关

    private void InitSkillInputNodes()
    {
        for (int i = 0; i < inputNodes.Count; i++)
        {
            InputNodeBase inputNode = inputNodes[i];
            inputNode.Init();
            int slotIndex = i;
            inputNode.SetCallBack(() => TriggerSkillBySlotIndex(slotIndex));
        }
    }

    private void TriggerSkillBySlotIndex(int slotIndex)
    {
        // 检查技能热栏是否装备了技能
        if (!EquipSlotDic.TryGetValue(slotIndex, out int skillID) || skillID < 0) return;
        // 释放技能
        if (skillBrain.CheckCanPlaySkill(skillID))
        {
            skillBrain.PlaySkill(skillID);
            // 技能播放进入 FSM 技能状态（每段独立进出；已在技能状态中连段时不重复重入）
            if (fsm != null && fsm.currentRootStateName != Player_SkillState.StateName)
                fsm.TriggerState(Player_SkillState.StateName);
        }
    }

    public void StartSkillInputCheck()
    {
        foreach (InputNodeBase inputNode in inputNodes)
            inputNode.CanInput = true;
    }

    public void StopSkillInputCheck()
    {
        foreach (InputNodeBase inputNode in inputNodes)
            inputNode.CanInput = false;
    }

    /// <summary>
    /// 按键每帧检测
    /// </summary>
    private void OnInputCheckUpdate()
    {
        foreach (InputNodeBase inputNode in inputNodes)
        {
            inputNode.OnUpdate();
        }
    }

    /// <summary>
    /// 输入打断检测：把移动/跳跃输入喂给当前技能行为，由行为按"是否解锁输入打断"决定是否取消自己。
    /// 打断逻辑归技能行为，本控制器只负责读输入。
    /// </summary>
    private void OnInputInterruptCheckUpdate()
    {
        if (InputManager.MoveInput != Vector2.zero || InputManager.JumpInput)
            skillBrain?.TryInputInterruptCurrentSkill();
    }

    #endregion

    #region 移动、旋转相关

    public void Rotate(Vector2 moveInput)
    {
        Vector3 input = new(moveInput.x, 0, moveInput.y);
        Vector3 target = Quaternion.Euler(0, Camera.main!.transform.eulerAngles.y, 0) * input;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), 10 * Time.deltaTime);
    }

    /// <summary>死亡表现开始:停 FSM/停技能输入/停技能/禁碰撞,并弹死亡 UI。</summary>
    protected override void OnDeathStart()
    {
        // 停 FSM(不再响应移动/技能状态切换)
        if (fsm != null)
            fsm.StopBehaviour();

        // 停技能输入检测(正在播放的技能由基类 Death() 统一 InterruptAllSkills 打断)
        StopSkillInputCheck();

        // 禁 CharacterController:尸体不再被推动/下落/移动
        if (characterController != null)
            characterController.enabled = false;

        // 弹出死亡 UI(面板按钮回调 Revive)
        UISystem.Show<UI_DeathWindow>();
    }

    public override bool SkillIsPlaying(int skillID)
    {
        return skillBrain.SkillIsPlaying(skillID);
    }

    /// <summary>打断所有正在播放的技能(技能状态与片段状态一起结束)。</summary>
    public override void InterruptAllSkills(bool resetCd = true, bool force = false)
    {
        skillBrain?.InterruptAllSkills(resetCd, force);
    }

    #region 受控（眩晕/受击等"失去控制"的表现：由动画 Cue 驱动 → 状态机切到受控状态）

    /// <summary>受控开始:打断技能、停技能输入、清掉旧动画的根运动回调。</summary>
    protected override void OnControlledEnter()
    {
        InterruptAllSkills();                // 受控期间不能继续放技能
        StopSkillInputCheck();               // 也不再响应技能输入(否则会切进技能状态)
        characterView.SetRootMotion(null);   // 受控期间不接受旧动画的根运动回调
    }

    /// <summary>受控结束:恢复技能输入。</summary>
    protected override void OnControlledExit() => StartSkillInputCheck();

    /// <summary>切到状态机的受控状态。</summary>
    protected override void TriggerControlledState() => fsm?.TriggerState(Player_ControlledState.StateName);

    #endregion

    /// <summary>打断指定技能。</summary>
    public override void InterruptSkill(int skillID, bool resetCd = true, bool force = false)
    {
        skillBrain?.InterruptSkill(skillID, resetCd, force);
    }

    /// <summary>复活:回满血、回出生点、恢复 FSM/输入/碰撞/技能。</summary>
    public void Revive()
    {
        if (!IsDead) return;

        // 回满血(先复位 IsDead,避免回血瞬间又触发 OnHpChanged 死亡)
        IsDead = false;
        asc.SetAttrValue(AttributeType.Hp, AttributeValueType.CurValue, asc.AttrDic[AttributeType.Hp].MaxValue);

        // 回到出生点
        transform.SetPositionAndRotation(_spawnPos, _spawnRot);

        // 恢复碰撞与控制
        if (characterController != null)
            characterController.enabled = true;

        // 恢复 FSM(从出生状态重新开始)与技能输入
        fsm?.StartBehaviour();
        StartSkillInputCheck();

        // 回到待机表现
        Play("Idle");
    }

    /// <summary>移除自身(玩家死亡不销毁,只由 Revive 恢复;此方法为统一入口预留)。</summary>
    public void ReviveAt(Vector3 pos, Quaternion rot)
    {
        _spawnPos = pos;
        _spawnRot = rot;
        Revive();
    }

    public override void Move(Vector3 deltaPos) => characterController.Move(deltaPos);

    public void Move(Vector2 moveInput, float moveSpeed)
    {
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        moveDir = Quaternion.Euler(0, Camera.main!.transform.eulerAngles.y, 0) * moveDir;
        characterController.Move(moveDir * (moveSpeed * Time.deltaTime));
    }

    #endregion

    #region Unity Editor

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(posOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * maxDis);
    }

#endif

    #endregion
}