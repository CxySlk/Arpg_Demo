using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tab 导航容器：只管理按钮选中态与切换回调，不关心页面业务。</summary>
public class UI_TabGroup
{
    private readonly List<UI_TabButton> _tabButtons = new();

    public UI_TabButton CurTabButton { get; private set; }

    public void AddPage(UI_TabButton page, Action clickCallBack)
    {
        if (page == null)
        {
            Debug.LogError("UI_TabGroup: 尝试添加空 Tab");
            return;
        }

        if (!page.Init(this, clickCallBack)) return;
        if (!_tabButtons.Contains(page)) _tabButtons.Add(page);
    }

    public void SelectTabPage(UI_TabButton page)
    {
        if (page == null || !_tabButtons.Contains(page)) return;

        CurTabButton = page;
        foreach (UI_TabButton tabButton in _tabButtons)
        {
            if (tabButton == page) tabButton.Select();
            else tabButton.UnSelect();
        }

        // 默认选中/代码切换也触发页面刷新；点击同一 Tab 由 UI_TabButton 提前拦截。
        page.NotifySelected();
    }
}
