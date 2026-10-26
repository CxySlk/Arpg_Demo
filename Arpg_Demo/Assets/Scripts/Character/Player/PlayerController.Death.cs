using LLKFrame;

public partial class PlayerController
{
    #region 死亡

    /// <summary>死亡表现开始:停 FSM/停技能输入/停技能/禁碰撞,并弹死亡 UI。</summary>
    protected override void OnDeathStart()
    {
        // 停 FSM(不再响应移动/技能状态切换)
        fsm.StopBehaviour();

        // 停技能输入检测(正在播放的技能由基类 Death() 统一 InterruptAllSkills 打断)
        StopSkillInputCheck();

        // 禁 CharacterController:尸体不再被推动/下落/移动
        characterController.enabled = false;

        // 弹出死亡 UI(面板按钮回调 Revive)
        UISystem.Show<UI_DeathWindow>();
    }

    #endregion
}
