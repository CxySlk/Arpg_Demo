using LLKFrame;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 战斗飘字类型（决定颜色/字号表现）。
    /// </summary>
    public enum FloatTextKind
    {
        /// <summary>伤害（黑/橙色系）</summary>
        Damage,

        /// <summary>治疗（绿色系，预留）</summary>
        Heal,
    }

    /// <summary>
    /// 战斗飘字请求数据。发布者：伤害/治疗结算处填充；UI 订阅者消费显示。
    /// </summary>
    public struct FloatTextRequest
    {
        /// <summary>世界坐标（受击/治疗目标的头顶位置）</summary>
        public Vector3 worldPos;

        /// <summary>数值（伤害/治疗量，展示时取整）</summary>
        public float value;

        /// <summary>是否暴击（暴击放大字号/更亮颜色）</summary>
        public bool isCritical;

        /// <summary>飘字类型</summary>
        public FloatTextKind kind;
    }

    /// <summary>
    /// 战斗飘字事件 Key。发布方为效果（结算处），订阅方为 UI（FloatingTextManager）。
    /// 定义在发布侧，保持战斗层与 UI 单向解耦。
    /// </summary>
    public static class FloatTextEventKey
    {
        /// <summary>请求显示一条战斗飘字（参数：请求数据）</summary>
        public static readonly EventKey<FloatTextRequest> RequestKey = "Effect.FloatText.Request";
    }
}
