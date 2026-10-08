using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

[UIWindow(nameof(UI_GameWindow), nameof(UI_GameWindow), false, 0)]
public class UI_GameWindow : UI_WindowBase
{
    [LabelText("技能热栏槽"), SerializeField] private UI_SkillSlots  _skillSlots;
    private Dictionary<int, int> EquipSlotDic => DataManager.PlayerData.equipSlotDic;
    public override void OnShow()
    {
        // 打开游戏主界面时,按 PlayerData 的热栏装备数据初始化热栏槽(显示图标 + 记录技能 id)
        InitSlotItems();
        // 确保战斗飘字管理器已初始化(订阅伤害事件,创建 Overlay Canvas)
        FloatingTextManager.EnsureInit();
    }

    private void InitSlotItems()
    {
        if (_skillSlots != null)
            _skillSlots.RefreshFromData();
    }
}