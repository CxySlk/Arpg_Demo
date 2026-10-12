using LLKFrame;
using Sirenix.OdinInspector;

namespace GAS
{
    public enum FrameEventType
    {
        /// <summary>无事件</summary>
        [LabelText("无事件")] None,

        /// <summary>可以释放技能</summary>
        [LabelText("可以释放技能")] CanReleaseSkill,
        [LabelText("锁定目标")] LockTarget,
        [LabelText("取消锁定目标")] UnLockTarget,
        [LabelText("顿帧")] FreezeFrame,

        /// <summary>自定义</summary>
        [LabelText("自定义")] Custom,

        /// <summary>
        /// 前摇结束帧事件：给自己移除"前摇标签"，解锁打断（可移动打断 + 可被受击打断）。
        /// </summary>
        /// <remarks>保持原枚举数值不变：改名/追加新值都不影响既有资产已序列化的旧数值。</remarks>
        [LabelText("前摇结束帧事件")] SkillStartupEnd,

        /// <summary>
        /// 前摇开始帧事件：给自己加上"前摇标签"（标签在 SkillConfig 上配置），
        /// 进入"不可打断"状态（吃伤害但不进受击硬直、也不能被移动打断）。
        /// 没配这个事件的技能不带标签，即不霸体、可被控制也能被打断。
        /// </summary>
        /// <remarks>新增值追加在枚举末尾，不影响既有资产已序列化的旧枚举数值。</remarks>
        [LabelText("前摇开始帧事件")] SkillStartupBegin,

        /// <summary>开启根运动：把动画根运动回调设为施法者的 OnRootMotion（受 AbilityAsset.rootMotionDis 限制位移）。</summary>
        /// <remarks>追加在枚举末尾，不影响既有资产已序列化的旧枚举数值。</remarks>
        [LabelText("开启根运动")] SkillRootMotionOn,

        /// <summary>关闭根运动：清空动画根运动回调，动画位移不再驱动角色。</summary>
        [LabelText("关闭根运动")] SkillRootMotionOff,

        [LabelText("开启武器拖尾")] StartWeaponTrail,
        [LabelText("关闭武器拖尾")] StopWeaponTrail,

        /// <summary>输入打断帧事件：触发后本段解锁"输入打断"（移动/跳跃可打断本段）。</summary>
        /// <remarks>追加在枚举末尾，不影响既有资产已序列化的旧枚举数值。</remarks>
        [LabelText("输入打断帧事件")] SkillInputInterrupt,
    }

    public class FrameEvent : MarkEventBase
    {
        [LabelText("事件类型")] public FrameEventType EventType;

        [LabelText("事件名称"), ShowIf("@EventType==FrameEventType.Custom")]
        public string eventName;

        [LabelText("整形参数")] public int intArg;
        [LabelText("浮点型参数")] public float floatArg;
        [LabelText("字符串参数")] public string strArg;
        [LabelText("其他参数")] public object objArg;

        public FrameEvent DeepCopy()
        {
            FrameEvent frameEvent = PoolSystem.Global.GetOrNewObject<FrameEvent>();
            frameEvent.EventType = EventType;
            frameEvent.eventName = eventName;
            frameEvent.intArg = intArg;
            frameEvent.floatArg = floatArg;
            frameEvent.strArg = strArg;
            frameEvent.objArg = objArg;
            return frameEvent;
        }

        public void Destroy()
        {
            eventName = null;
            strArg = null;
            objArg = null;
            this.PushObject();
        }
    }
}