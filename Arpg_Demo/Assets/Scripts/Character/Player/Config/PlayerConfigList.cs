using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Player.Config
{
    public class PlayerConfigList : ConfigBase
    {
        [LabelText("玩家配置列表"), SerializeField] private List<PlayerConfig> _configList = new();
        public List<PlayerConfig> ConfigList => _configList;
    }
}