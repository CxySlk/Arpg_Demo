using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 玩家配置资产：定义角色的名称、资源路径、基础动画、GAS 预设与技能配置。
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/Character/PlayerConfig")]
public class PlayerConfig : CharacterConfigBase
{
    [BoxGroup("基础信息", true)] [LabelText("角色名"), SerializeField, Required]
    private string _playerName;

    [BoxGroup("基础信息")] [LabelText("描述"), SerializeField, Multiline]
    private string _description;

    [BoxGroup("资源路径")] [LabelText("View路径"), SerializeField, Required]
    private string _viewPrefabPath;

    public string PlayerName => _playerName;
    public string Description => _description;
    public string ViewPrefabPath => _viewPrefabPath;
}