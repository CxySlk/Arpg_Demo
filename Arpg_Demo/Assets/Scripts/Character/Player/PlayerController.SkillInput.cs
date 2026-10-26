using UnityEngine;

public partial class PlayerController
{
    #region 技能输入

    private void InitSkillInputNodes()
    {
        for (int i = 0; i < inputNodes.Count; i++)
        {
            InputNodeBase inputNode = inputNodes[i];
            inputNode.Init();
            if (i < 2) continue;

            int slotIndex = i - 2;
            inputNode.SetCallBack(() => TriggerSkillBySlotIndex(slotIndex));
        }
    }

    private void TriggerLeftAttack()
    {
        TryReleaseSkillByIndex(0);
    }

    private void TriggerRightAttack()
    {
        TryReleaseSkillByIndex(1);
    }

    private void TriggerSkillBySlotIndex(int slotIndex)
    {
        if (!EquipSlotDic.TryGetValue(slotIndex, out int skillID) || skillID < 0) return;
        TryReleaseSkillByID(skillID);
    }

    /// <summary>按 OwnedSkills 下标释放（仅用于左/右键对应的默认技能）。</summary>
    private void TryReleaseSkillByIndex(int index)
    {
        if (PlayerConfig == null ||
            index < 0 ||
            index >= PlayerConfig.OwnedSkills.Count) return;

        TryReleaseSkillByID(PlayerConfig.OwnedSkills[index]);
    }

    /// <summary>按实际技能 id 释放（热栏槽使用）。</summary>
    private void TryReleaseSkillByID(int skillID)
    {
        if (skillID < 0) return;

        if (skillBrain.CheckCanPlaySkill(skillID))
        {
            skillBrain.PlaySkill(skillID);
            if (fsm.currentRootStateName != Player_SkillState.StateName)
                fsm.TriggerState(Player_SkillState.StateName);
        }
    }

    public void StartSkillInputCheck()
    {
        foreach (InputNodeBase inputNode in inputNodes)
            inputNode.CanInput = true;
    }

    public void StopSkillInputCheck()
    {
        foreach (InputNodeBase inputNode in inputNodes)
            inputNode.CanInput = false;
    }

    /// <summary>按键每帧检测</summary>
    private void OnInputCheckUpdate()
    {
        foreach (InputNodeBase inputNode in inputNodes)
            inputNode.OnUpdate();
    }

    /// <summary>
    /// 输入打断检测：把移动/跳跃输入喂给当前技能行为，由行为决定是否取消自己。
    /// </summary>
    private void OnInputInterruptCheckUpdate()
    {
        if (InputManager.MoveInput != Vector2.zero || InputManager.JumpInput)
            skillBrain.TryInputInterruptCurrentSkill();
    }

    #endregion
}
