using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerListConfig", menuName = "Config/PlayerListConfig")]
public class PlayerListConfig : ConfigBase
{
    [LabelText("角色配置列表"), SerializeField] private List<PlayerConfig> playerDataList = new List<PlayerConfig>();
    public List<PlayerConfig> PlayerDataList => playerDataList;
}
