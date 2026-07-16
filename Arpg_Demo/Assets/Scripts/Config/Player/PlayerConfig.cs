using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/PlayerConfig")]
public class PlayerConfig : ConfigBase
{
    [LabelText("角色名"), SerializeField] private string playerName;
    [LabelText("描述"), SerializeField, Multiline] private string description;
    [LabelText("预制体"), SerializeField] private GameObject prefab;
    // 属性相关
    [LabelText("攻击力"), SerializeField] private int attackBaseValue;
    [LabelText("防御力"), SerializeField] private int defendBaseValue;
    [LabelText("血量"), SerializeField] private int hpBaseValue;
    [LabelText("魔法值"), SerializeField] private int mpBaseValue;
    [LabelText("移动速度"), SerializeField] private float moveSpeed;
    [LabelText("旋转速度"), SerializeField] private float rotateSpeed;
    [LabelText("空中移动速度"), SerializeField] private float inAirMoveSpeed;
    // 动画相关
    [LabelText("角色基础动画"), DictionaryDrawerSettings(KeyLabel = "动画名", ValueLabel = "动画片段"), SerializeField]
    private Dictionary<string, AnimationClip> standardAnimationDic;

    public string PlayerName => playerName;
    public string Description => description;
    public GameObject Prefab => prefab;
    public int AttackBaseValue => attackBaseValue;
    public int DefendBaseValue => defendBaseValue;
    public int HpBaseValue => hpBaseValue;
    public int MpBaseValue => mpBaseValue;
    public float MoveSpeed => moveSpeed;
    public float RotateSpeed => rotateSpeed;
    public float InAirMoveSpeed => inAirMoveSpeed;

    public Dictionary<string, AnimationClip> StandardAnimationDic { get => standardAnimationDic; set => standardAnimationDic = value; }
}
