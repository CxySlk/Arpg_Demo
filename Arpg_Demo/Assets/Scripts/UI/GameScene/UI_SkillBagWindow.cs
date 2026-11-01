using System.Collections.Generic;
using Data;
using LLKFrame;
using Sirenix.OdinInspector;
using Skill;
using Skill.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能背包窗口：列出角色拥有的技能，支持 Tab 切换已学习/未学习、查看详情、学习技能和拖拽装备。
/// </summary>
[UIWindow(nameof(UI_SkillBagWindow), nameof(UI_SkillBagWindow), true, 0)]
public class UI_SkillBagWindow : UI_WindowBase
{
    [SerializeField] private GameObject _skillInfoObj;
    [SerializeField] private TMP_Text _txtSkillPoint;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDescription;
    [SerializeField] private GameObject _skillSlotPrefab;
    [LabelText("滚动视图内容"), SerializeField] private Transform _svContent;
    [SerializeField] private UI_TabButton _ownerTab;
    [SerializeField] private UI_TabButton _learnTab;
    [SerializeField] private Button _btnLearn;

    /// <summary>全部技能槽；是否已学习由槽自身状态区分。</summary>
    private readonly List<UI_BagSkillSlot> _slots = new();

    private readonly SkillIconCache _iconCache = new();
    private readonly UI_TabGroup _tabGroup = new();
    private readonly string _prefixStr = "<space=2em>";

    private UI_BagSkillSlot _selectedSlot;

    private SkillLearnData SkillLearnData => DataManager.PlayerData.skillLearnData;

    public override void Init()
    {
        base.Init();

        _tabGroup.AddPage(_ownerTab, () => ShowTab(true));
        _tabGroup.AddPage(_learnTab, () => ShowTab(false));

        InitSkillSlots();

        SkillEquipManager.EquipChanged += OnEquipChanged;
        _btnLearn.onClick.AddListener(OnButtonLearnClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();

        TimeSystem.Global.Pause();
        Cursor.lockState = CursorLockMode.None;
        _skillInfoObj.SetActive(false);

        RefreshSkillPoint();

        _tabGroup.SelectTabPage(_ownerTab);
        if (!_tabGroup.CurTabButton) ShowTab(true);

        RefreshEquipStates();
    }

    protected override void OnClose()
    {
        Cursor.lockState = CursorLockMode.Locked;
        TimeSystem.Global.UnPause();
    }

    protected override void OnDestroy()
    {
        SkillEquipManager.EquipChanged -= OnEquipChanged;
        _iconCache.ReleaseAll();
    }

    private void InitSkillSlots()
    {
        List<cfg.Skill> skillList = DataManager.PlayerConfig.OwnedSkills_Ref;
        HashSet<int> learnedIds = SkillLearnData.skillIdSet;

        foreach (cfg.Skill skill in skillList)
        {
            if (!skill.IsShowUI) continue;

            UI_BagSkillSlot slot = Instantiate(_skillSlotPrefab, _svContent).GetComponent<UI_BagSkillSlot>();
            slot.BtnSlot.onClick.AddListener(() => SelectSlot(slot));
            slot.SetInfo(skill.Id, _iconCache.Get(skill.Id));

            bool learned = learnedIds.Contains(skill.Id);
            slot.SetLearned(learned);
            slot.SetEquipped(learned && SkillEquipManager.FindSlotBySkill(skill.Id) >= 0);

            _slots.Add(slot);
        }
    }

    private void SelectSlot(UI_BagSkillSlot slot)
    {
        _selectedSlot = slot;
        ShowSkillInfo(slot);
    }

    private void ShowTab(bool showLearned)
    {
        _skillInfoObj.SetActive(false);
        _selectedSlot = null;

        foreach (UI_BagSkillSlot slot in _slots)
            slot.gameObject.SetActive(slot.IsLearned == showLearned);
    }

    private void ShowSkillInfo(UI_BagSkillSlot slot)
    {
        cfg.Skill skill = GetSkill(slot.SkillId);
        if (skill == null) return;

        _txtName.text = skill.Name;
        _txtDescription.text = _prefixStr + skill.Description;
        _skillInfoObj.SetActive(true);

        bool learned = slot.IsLearned;
        _btnLearn.gameObject.SetActive(!learned);
        if (learned) return;

        _btnLearn.interactable = DataManager.PlayerData.skillPoints >= skill.LearnCostPoints;
        TMP_Text txt = _btnLearn.GetComponentInChildren<TMP_Text>();
        txt.SetText($"技能点：{skill.LearnCostPoints}\n（解锁）");
    }

    private void OnButtonLearnClicked()
    {
        if (_selectedSlot == null || _selectedSlot.IsLearned) return;

        cfg.Skill skill = GetSkill(_selectedSlot.SkillId);
        if (skill == null) return;

        PlayerData playerData = DataManager.PlayerData;
        if (playerData.skillPoints < skill.LearnCostPoints) return;

        if (!PlayerController.Instance.SkillBrain.LearnSkill(skill.Id)) return;

        playerData.skillPoints -= skill.LearnCostPoints;
        DataManager.SavePlayerData();

        RefreshSkillPoint();

        UI_BagSkillSlot learnedSlot = _selectedSlot;
        learnedSlot.SetLearned(true);

        ShowTab(true);
        _tabGroup.SelectTabPage(_ownerTab);

        _selectedSlot = learnedSlot;
        ShowSkillInfo(learnedSlot);
    }

    private void OnEquipChanged(int slotIndex, int oldSkillId, int newSkillId)
    {
        if (gameObject.activeSelf)
            RefreshEquipStates();
    }

    private void RefreshEquipStates()
    {
        foreach (UI_BagSkillSlot slot in _slots)
        {
            bool equipped = slot.IsLearned && SkillEquipManager.FindSlotBySkill(slot.SkillId) >= 0;
            slot.SetEquipped(equipped);
        }
    }

    private void RefreshSkillPoint()
    {
        _txtSkillPoint.SetText($"技能点：{DataManager.PlayerData.skillPoints}");
    }

    private static cfg.Skill GetSkill(int id) => ConfigManager.Tables.TbSkill.GetOrDefault(id);
}
