using System.Collections.Generic;
using MessagePack;
using Skill.Data;

[MessagePackObject]
public class PlayerData
{
    [Key(0)] public int playerConfigID;
    [Key(3)] public string nickName;
    [Key(1)] public SkillLearnData skillLearnData = new();
    [Key(2)] public Dictionary<int, int> equipSlotDic = new();
    [Key(4)] public int skillPoints = 1;
}