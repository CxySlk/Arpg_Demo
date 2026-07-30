using LLKFrame;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Ability 资产基类。预留用于时间轴驱动的技能系统。
    /// </summary>
    public class AbilityAsset : ConfigBase
    {
        public Dictionary<int, GameplayCue> ss;
    }
}
