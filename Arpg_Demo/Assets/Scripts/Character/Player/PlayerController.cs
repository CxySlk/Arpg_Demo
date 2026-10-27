using System;
using System.Collections.Generic;
using cfg;
using Data;
using Sirenix.OdinInspector;
using Character;
using GAS;
using LLKFrame;
using NodeCanvas.StateMachines;
using Skill;
using Skill.Data;
using UnityEngine;

public partial class PlayerController : CharacterControllerBase, IKFCProvider
{
    #region 组件与引用

    [LabelText("角色控制器"), SerializeField] protected CharacterController characterController;
    [LabelText("FSM"), SerializeField] protected FSMOwner fsm;
    [LabelText("技能大脑"), SerializeField] protected Player_SkillBrain skillBrain;

    [LabelText("技能按键配置"), SerializeField, ListDrawerSettings(AddCopiesLastElement = true)]
    protected List<InputNodeBase> inputNodes = new();

    private static PlayerController _instance;
    public static PlayerController Instance => _instance;
    private Dictionary<int, int> EquipSlotDic => DataManager.PlayerData.equipSlotDic;

    // 玩家配置
    public PlayerConfig PlayerConfig { get; private set; }
    public FSMOwner Fsm => fsm;
    public Player_SkillBrain SkillBrain => skillBrain;

    // 摄像机控制器
    [LabelText("摄像机控制器"), SerializeField, Required]
    private CameraController _cameraController;

    #endregion

    #region 生命周期

    // 出生点(死亡复活回这里;在 Start 记录)
    private Vector3 _spawnPos;
    private Quaternion _spawnRot;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        // 记录出生点(复活时回到这里)
        _spawnPos = transform.position;
        _spawnRot = transform.rotation;
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

    private void FixedUpdate()
    {
        _stageController.UpdateCirclePos();
    }

    private void LateUpdate()
    {
        _stageController.UpdateRequest();
    }

    /// <summary>玩家初始化：传入玩家配置（Luban）。属性来自属性表，ascPreset 走地址加载，技能按技能 id 装配。</summary>
    public void Init(PlayerConfig config, SkillLearnData learnData)
    {
        PlayerConfig = config;
        InitShared();
        InitTargetLock();
        // 摄像机初始化
        _cameraController.Init();
        // 初始化功夫圈
        _kungFuCircle = new(config.KfcConfig_Ref);
        _stageController = new StageController(config.KfcConfig_Ref, transform);

        // ASC 预设（tags/默认效果）走 Addressable 地址
        AbilitySystemComponentPreset preset = string.IsNullOrEmpty(config.AscPresetPath)
            ? null
            : ResSystem.LoadAsset<AbilitySystemComponentPreset>(config.AscPresetPath);
        asc.Init(preset, CollectAttrs(config.Id), EventCenter);

        skillBrain.InitForConfig(config, learnData);

        // 学习默认的技能（技能 id）
        foreach (int skillId in config.DefaultLearnSkills)
            skillBrain.LearnSkill(skillId);

        // 开启技能输入检测
        InitSkillInputNodes();
        StartSkillInputCheck();
    }

    private void OnTimeStateChanged(bool isRunning)
    {
        if (isRunning)
        {
            // 开启技能输入
            StartSkillInputCheck();
            // 开启摄像机
            _cameraController.StartCurCamera();
        }
        else
        {
            // 关闭技能输入            
            StopSkillInputCheck();
            // 关闭Cinemachine
            _cameraController.StopCurCamera();
        }
    }

    #endregion

    #region 复活

    /// <summary>复活:回满血、回出生点、恢复 FSM/输入/碰撞/技能。</summary>
    public void Revive()
    {
        if (!IsDead) return;

        // 回满血(先复位 IsDead,避免回血瞬间又触发 OnAttrCurValueChanged 死亡)
        IsDead = false;
        asc.SetAttrValue(EAttributeType.Hp, AttributeValueType.CurValue, asc.AttrDic[EAttributeType.Hp].MaxValue);

        // 回到出生点
        transform.SetPositionAndRotation(_spawnPos, _spawnRot);

        // 恢复碰撞与控制
        characterController.enabled = true;

        // 恢复 FSM(从出生状态重新开始)与技能输入
        fsm.StartBehaviour();
        StartSkillInputCheck();

        // 回到待机表现
        Play("Idle");
    }

    #endregion
}