using System;
using System.Collections.Generic;
using Animancer;
using GAS;
using Hovl;
using Sirenix.OdinInspector;
using UnityEngine;
using Attribute = GAS.Attribute;

namespace Character
{
    public abstract class CharacterControllerBase : SerializedMonoBehaviour, ICharacterController, IGroundCondition
    {
        #region 组件相关

        [LabelText("ASC系统"), SerializeField] protected AbilitySystemComponent asc;
        [LabelText("玩家View"), SerializeField] protected CharacterView characterView;

        [LabelText("Weapon控制器"), SerializeField]
        protected WeaponController weaponController;

        [LabelText("拖尾组件"), SerializeField] protected HS_SwordTrailAnimationEvents swordTrailAnimationEvents;
        public AbilitySystemComponent Asc => asc;
        public CharacterView CharacterView => characterView;
        public AnimancerComponent Animancer => characterView.Animancer;
        public WeaponController WeaponController => weaponController;
        private HS_SwordTrailAnimationEvents SwordTrailAnimationEvents => swordTrailAnimationEvents;

        #endregion

        #region 地面检测

        [Title("地面检测"), LabelText("检测层级"), SerializeField]
        protected LayerMask detectedLayerMask;

        [LabelText("检测点偏移"), SerializeField] protected Vector3 posOffset;

        [LabelText("检测距离"), SerializeField] protected float maxDis;

        [LabelText("接地"), ShowInInspector]
        public bool IsGrounded
        {
            get
            {
                Vector3 origin = transform.TransformPoint(posOffset);
                return Physics.Raycast(origin, Vector3.down, maxDis, detectedLayerMask);
            }
        }

        #endregion

        protected virtual void Init()
        {
            weaponController.Init(this);
        }

        protected virtual void OnEnable()
        {
            BindHpMonitor();
        }

        protected virtual void Update()
        {
            // 受控时长到点自动退出(状态机那边看到 IsControlled=false 会 Finish 回原状态)
            if (IsControlled && Time.time >= ControlledEndTime) ExitControlled();

            // 索敌
            LookTargetUpdate();
            // 自动恢复效果
            OnAttrHealUpdate();
        }

        protected virtual void OnDisable()
        {
            UnbindHpMonitor();
        }

        #region 锁定目标

        public Transform Target { get; protected set; }
        protected bool isLockTarget;
        [LabelText("旋转速度"), SerializeField] protected float rotateSpeed;

        public void SetTarget(Transform target) => Target = target;
        public virtual void LockTarget(bool isLock) => isLockTarget = isLock;

        protected void LookTargetUpdate()
        {
            if (!isLockTarget || !Target) return;

            // 只在水平面上转向:不抹掉 Y 的话,目标比自己高/矮时角色会前倾后仰
            Vector3 direction = Target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * rotateSpeed);
        }

        #endregion

        #region 死亡监听

        /// <summary>
        /// 绑定 HP 变化监听(死亡检测入口)。
        /// 玩家 asc 在 SetConfig 后才初始化,此时 AttrDic 还没有 Hp,故绑定放到 asc 就绪后调用;
        /// 敌人 asc 在 Awake 内已初始化,OnEnable 即可绑上。
        /// </summary>
        protected void BindHpMonitor()
        {
            if (asc.AttrDic.TryGetValue(AttributeType.Hp, out GAS.Attribute hp))
                hp.AddCurValueChanged(OnHpChanged);
        }

        private void UnbindHpMonitor()
        {
            if (asc.AttrDic.TryGetValue(AttributeType.Hp, out GAS.Attribute hp))
                hp.RemoveCurValueChanged(OnHpChanged);
        }

        #endregion

        #region 播放动画

        /// <summary>播放单个动画片段(Animancer)。返回的 state 可自行设置 Speed/Time/Events。</summary>
        public AnimancerState Play(AnimationClip clip, float fade = .25f, float speed = 1f, float startTime = 0f)
        {
            AnimancerState state = characterView.Play(clip, fade);
            if (state == null) return null;

            state.Speed = speed;
            state.Time = startTime;
            return state;
        }

        /// <summary>播放标准动画字典里的动画(按名字查找)。用 ClipTransition 自身设置，只覆盖播放速度。</summary>
        public AnimancerState Play(string animationName, float speed = 1f)
            => characterView.Play(animationName, speed);

        /// <summary>按名字取动画长度(秒)。供行为树等"等播完"计时。</summary>
        public float GetClipLength(string animationName) => characterView.GetClipLength(animationName);

        /// <summary>设置/清空根运动回调(Animator 同物体,由 CharacterView 转发 deltaPosition/deltaRotation)。</summary>
        public void SetRootMotion(Action<Vector3, Quaternion> onRootMotion) =>
            characterView.SetRootMotion(onRootMotion);

        #endregion

        #region 受控状态（眩晕/受击等"失去控制"的通用表现）

        /// <summary>当前是否处于受控状态(失去控制:不能行动、不能放技能)。</summary>
        public bool IsControlled { get; private set; }

        /// <summary>受控结束的绝对时间(Time.time)。受控期间再次进入只取更晚的那个时间。</summary>
        public float ControlledEndTime { get; private set; }

        /// <summary>
        /// 进入/延长受控状态:结束时间取"本次时长"与"原有剩余"里更晚的那个。
        /// 例:10 秒眩晕过了 1 秒,再挨一次 2 秒受击 → 仍然是 9 秒后结束(不是 11 秒)。
        /// </summary>
        public void EnterControlled(float duration)
        {
            if (!CanBeControlled()) return;

            bool isFirst = !IsControlled;
            IsControlled = true;
            ControlledEndTime = isFirst
                ? Time.time + Mathf.Max(0f, duration)
                : Mathf.Max(ControlledEndTime, Time.time + Mathf.Max(0f, duration));

            // 已经在受控里:只延长时间,不重复进入流程
            if (!isFirst) return;

            OnControlledEnter();
            TriggerControlledState();
        }

        /// <summary>退出受控状态(时长到点自动调用;状态机退出时也会调)。</summary>
        public void ExitControlled()
        {
            if (!IsControlled) return;

            IsControlled = false;
            ControlledEndTime = 0f;
            OnControlledExit();
        }

        /// <summary>是否允许进入受控(死亡不收控;能否受控由效果的标签门禁决定)。</summary>
        protected virtual bool CanBeControlled() => !IsDead;

        /// <summary>受控开始(子类:停移动、打断技能、清根运动等)。</summary>
        protected virtual void OnControlledEnter()
        {
        }

        /// <summary>受控结束(子类:恢复表现)。</summary>
        protected virtual void OnControlledExit()
        {
        }

        /// <summary>切到状态机的受控状态(子类各自 TriggerState)。</summary>
        protected virtual void TriggerControlledState()
        {
        }

        #endregion

        /// <summary>移动角色(死亡/禁用控制器时静默忽略,避免残留 RootMotion 回调报错)。</summary>
        public abstract void Move(Vector3 deltaPos);

        public void Rotate(Quaternion deltaRot) => transform.rotation *= deltaRot;

        protected virtual void OnHpChanged(float hp)
        {
            // HP 变化只用于死亡检测(受伤/受控是独立体系,不由扣血驱动)
            if (hp <= 0)
                Death();
        }

        /// <summary>是否已死亡(防重复触发,复活后复位)</summary>
        public bool IsDead { get; protected set; }

        /// <summary>死亡入口:Hp<=0 时触发。标记死亡、停止技能与移动表现、播死亡动画。</summary>
        protected virtual void Death()
        {
            if (IsDead) return;
            IsDead = true;

            // 摘除当前动画的 RootMotion 回调:死亡后角色不再被旧技能/受击动画位移驱动,
            // 否则动画收尾期间 OnAnimatorMove 会触发 Move(且控制器已被禁用而报错)
            if (characterView)
                characterView.SetRootMotion(null);

            // 打断所有正在播放的技能(技能播放状态与片段播放状态一起结束;force:死亡必定打断,不受"前摇不可打断"限制)
            InterruptAllSkills(resetCd: true, force: true);

            // 关闭角色交互(武器/碰撞体/CharacterController):尸体不再被命中,从而不再播受击/受控动画
            CloseInteractionOnDeath();
            OnDeathStart();

            // 播死亡动画(标准动画字典里有 Death 键;没有则跳过不报错)
            PlayDeathAnimation();
        }

        /// <summary>
        /// 死亡时关闭角色交互:关武器(碰撞体+拖尾) + 关闭所有子碰撞体 + 关闭 Unity CharacterController。
        /// 目的:尸体不再被任何攻击命中,从而不再触发受击/受控表现。
        /// 不动 Animancer/Animator(死亡倒地动画照常播放)、也不隐藏任何物体(武器仍可见)。
        /// </summary>
        protected virtual void CloseInteractionOnDeath()
        {
            weaponController.Close();

            foreach (Collider col in GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            CharacterController cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
        }

        /// <summary>死亡表现开始时的钩子(子类覆写:停 FSM/停输入/禁碰撞等)</summary>
        protected virtual void OnDeathStart()
        {
        }

        /// <summary>
        /// 停止当前技能播放的钩子(非死亡场景用:传送、进剧情、强制收招等)。
        /// 死亡不走这里——<see cref="Death"/> 统一调 <see cref="InterruptAllSkills"/> 打断所有在播技能。
        /// </summary>
        protected virtual void StopSkill()
        {
        }

        /// <summary>尝试播放死亡动画,无 Death 键时静默。</summary>
        protected virtual void PlayDeathAnimation()
        {
            if (characterView) Play("Death");
        }

        #region 属性自动效果

        protected float hpIntervalTimer = 0;
        protected float hpHealValue = 0;
        protected float mpIntervalTimer = 0;
        protected float mpHealValue = 0;

        protected void OnAttrHealUpdate()
        {
            // 生命恢复效果：每帧按 恢复速度*deltaTime 回血；hpHealValue 只用于攒够 1 秒的飘字显示
            if (asc.TryGetAttrValue(AttributeType.HpRegenSpeed, out Attribute hpRegenSpeedAttr) &&
                asc.TryGetAttrValue(AttributeType.Hp, out Attribute hpAttr))
            {
                float healValue = hpRegenSpeedAttr.CurValue * Time.deltaTime;
                hpHealValue += healValue;
                hpIntervalTimer += Time.deltaTime;
                if (hpIntervalTimer >= 1f)
                {
                    hpIntervalTimer %= 1f;
                    FloatingTextManager.Instance.ShowAt(transform.position, hpHealValue, false, FloatTextKind.Heal);
                    hpHealValue = 0f;
                }

                hpAttr.AddCurrentValue(healValue);
            }

            // 法力恢复效果（同样用 deltaTime，帧率无关）
            if (asc.TryGetAttrValue(AttributeType.MpRegenSpeed, out Attribute mpRegenSpeedAttr) &&
                asc.TryGetAttrValue(AttributeType.Mp, out Attribute mpAttr))
            {
                mpAttr.AddCurrentValue(mpRegenSpeedAttr.CurValue * Time.deltaTime);
            }
        }

        #endregion

        public float GetTargetSqrDis()
        {
            if (!Target) return -1f;
            return (Target.position - transform.position).sqrMagnitude;
        }

        #region 技能状态与打断（抽象契约:玩家/敌人各自转调自己的技能大脑）

        /// <summary>指定技能的"整套连段"是否正在播放(全部片段播完 / 续段间隙超时 / 被打断后为 false)。</summary>
        public abstract bool SkillIsPlaying(int skillID);

        /// <summary>
        /// 打断所有正在播放的技能:技能播放状态与片段播放状态都会结束,并触发各自的结束流程。
        /// 死亡、硬直、眩晕、传送这类"通用流程"直接调它即可,不需要知道具体是哪个技能大脑。
        /// </summary>
        /// <param name="resetCd">是否按一次完整释放结算进入 CD(默认 true)。</param>
        /// <param name="force">true = 无视技能"前摇不可打断"(死亡等必须打断的场合,默认 false)。</param>
        public abstract void InterruptAllSkills(bool resetCd = true, bool force = false);

        /// <summary>打断指定技能(按 skillID)。</summary>
        /// <param name="resetCd">是否按一次完整释放结算进入 CD(默认 true)。</param>
        /// <param name="force">true = 无视技能"前摇不可打断"。</param>
        public abstract void InterruptSkill(int skillID, bool resetCd = true, bool force = false);

        #endregion

        /// <summary>
        /// 开启拖尾
        /// 按位来计算（例如 3（0x11）：开启第0个和第1和拖尾）
        /// 0默认开启所有拖尾
        /// </summary>
        /// <param name="swordTrailKey">按位计算</param>
        /// <param name="meshTrail"></param>
        public void StartWeaponTrail(int swordTrailKey, HS_SwordTrailPreset meshTrail)
        {
            if(meshTrail)
                // swordTrailAnimationEv
                weaponController.ApplyTrailPreset(meshTrail);
            
            if (swordTrailKey == 0)
            {
                swordTrailAnimationEvents.StartSwordTrail();
                return;
            }

            IReadOnlyList<HS_SwordMeshTrail> swordTrails = swordTrailAnimationEvents.SwordTrails;
            int i = 0;
            while (swordTrailKey > 0)
            {
                bool isStart = (swordTrailKey & 1) == 1;
                swordTrailKey >>= 1;
                if (isStart)
                    swordTrails[i].StartTrail();
                i++;
            }
        }

        public void StopWeaponTrail()
        {
            swordTrailAnimationEvents.StopSwordTrail();
        }
    }
}