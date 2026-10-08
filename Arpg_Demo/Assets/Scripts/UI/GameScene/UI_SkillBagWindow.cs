using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using LLKFrame;
using Sirenix.OdinInspector;
using Skill.Data;
using TMPro;
using UnityEngine;

/// <summary>
/// 技能背包窗口：列出所有已学技能（每个 = 一个 <see cref="UI_BagSkillSlot"/>）。
/// 支持把技能拖拽装备到热栏槽；装备/返还状态由 <see cref="SkillEquipManager"/> 数据驱动并持久化。
/// </summary>
[UIWindow(nameof(UI_SkillBagWindow), nameof(UI_SkillBagWindow), true, 0)]
public class UI_SkillBagWindow : UI_WindowBase
{
    [SerializeField] private GameObject _skillInfoObj;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDescription;
    [SerializeField] private GameObject _skillSlotPrefab;
    [LabelText("滚动视图内容")] [SerializeField] private Transform _svContent;

    [LabelText("开合动画时长(秒)"), SerializeField]
    private float _animDuration = .4f;

    [LabelText("缩放起始倍率"), SerializeField] private float _scaleFrom = .3f;

    /// <summary>当前开合动画（重复 Show/Close 时先 Kill 旧的）</summary>
    private Tween _animTween;

    /// <summary>背包槽对象池（按已学技能数量创建/复用）</summary>
    private readonly List<UI_BagSkillSlot> _skillSlotItemList = new();

    /// <summary>技能 id → 背包槽 索引（定位/刷新装备状态用，不依赖列表顺序）</summary>
    private readonly Dictionary<int, UI_BagSkillSlot> _skillSlotDic = new();

    private readonly string _prefixStr = "<space=2em>";

    private bool _subscribed;

    private List<SkillConfig> SkillConfigList => DataManager.PlayerConfig.SkillConfigList;
    private SkillLearnData SkillLearnData => DataManager.PlayerData.skillLearnData;

    private Vector3 _initPos;
    private float _offX;
    private Vector3 _targetScale;

    public override void Init()
    {
        RectTransform rt = (RectTransform)transform;
        _initPos = rt.anchoredPosition;
        _targetScale = rt.localScale;
    }


    /// <summary>显示时：从屏幕右侧外平移到设计位置，同时从很小弹开放大（入场动画）</summary>
    public override void OnShow()
    {
        base.OnShow();
        // 暂停时间
        TimeSystem.Global.Pause();

        _skillInfoObj.SetActive(false);
        // 技能槽的初始化
        InitSkillSlots();

        SubscribeEquipChanged();
        AudioSystem.PlayEffectMusic("ButtonClick");
        PlayShowAnimation();
    }

    protected override void OnClose()
    {
        TimeSystem.Global.UnPause();
    }

    /// <summary>入场：平移入场 + 弹开放大（OutBack 会略微超过目标再回弹，产生"弹开"感）</summary>
    private void PlayShowAnimation()
    {
        _animTween?.Kill();
        RectTransform rt = (RectTransform)transform;
        _offX = rt.rect.width;

        // 起点：屏幕右侧外 + 缩得很小
        rt.anchoredPosition = new Vector2(_initPos.x + _offX, _initPos.y);
        rt.localScale = _targetScale * _scaleFrom;

        // 同时：平移到目标 + 弹开放大
        _animTween = DOTween.Sequence()
            .Append(rt.DOAnchorPos(_initPos, _animDuration).SetEase(Ease.OutQuad).SetUpdate(true))
            .Join(rt.DOScale(_targetScale, _animDuration).SetEase(Ease.OutBack).SetUpdate(true));
    }

    /// <summary>关闭：平移到屏幕右侧外，同时缩小（与入场相反），动画播完再隐藏</summary>
    public override void Close()
    {
        IsShow = false;
        OnClose(); // 窗口清理（取消事件订阅等，基类约定）
        _animTween?.Kill();
        AudioSystem.PlayEffectMusic("ButtonClick");

        RectTransform rt = (RectTransform)transform;
        _offX = rt.rect.width;

        _animTween = DOTween.Sequence()
            .Append(rt.DOAnchorPos(new Vector2(_initPos.x + _offX, _initPos.y), _animDuration).SetEase(Ease.InQuad)
                .SetUpdate(true))
            .Join(rt.DOScale(_targetScale * _scaleFrom, _animDuration).SetEase(Ease.InBack).SetUpdate(true))
            .OnComplete(() => gameObject.SetActive(false));
    }

    protected override void OnDestroy()
    {
        _animTween?.Kill();
        if (_subscribed)
        {
            _subscribed = false;
            SkillEquipManager.EquipChanged -= OnEquipChanged;
        }
    }

    private void OnDisable()
    {
        // 窗口隐藏（含关闭动画播完后 SetActive(false)）→ 退订，避免下次打开重复订阅
        if (_subscribed)
        {
            _subscribed = false;
            SkillEquipManager.EquipChanged -= OnEquipChanged;
        }
    }

    private void SubscribeEquipChanged()
    {
        if (_subscribed) return;
        _subscribed = true;
        SkillEquipManager.EquipChanged += OnEquipChanged;
    }

    /// <summary>装备/返还变化 → 刷新本窗口所有槽的"已给/可拖"状态（含顶替/返还后的图标恢复）</summary>
    private void OnEquipChanged(int slotIndex, int oldSkillId, int newSkillId)
    {
        if (gameObject.activeSelf)
            RefreshEquipStates();
    }

    /// <summary>按装备数据刷新每个背包槽：已装备 → 隐藏图标/禁拖；未装备 → 恢复图标/可拖</summary>
    private void RefreshEquipStates()
    {
        foreach (KeyValuePair<int, UI_BagSkillSlot> kv in _skillSlotDic)
        {
            bool equipped = SkillEquipManager.FindSlotBySkill(kv.Key) >= 0;
            kv.Value.SetEquipped(equipped);
        }
    }

    private void InitSkillSlots()
    {
        // 按技能 id 排序生成，保证列表顺序稳定（HashSet 本身无序）
        List<int> ids = SkillLearnData.skillIdSet.OrderBy(x => x).ToList();
        int slotCount = _skillSlotItemList.Count;
        int needCount = ids.Count;

        // 数量不够 → 新建
        for (int i = slotCount; i < needCount; i++)
        {
            UI_BagSkillSlot slot = Instantiate(_skillSlotPrefab, _svContent).GetComponent<UI_BagSkillSlot>();
            // 点击看详情只绑定一次（点击时读取该槽当前 SkillId，避免重复监听）
            slot.BtnSlot.onClick.AddListener(() => ShowSkillInfo(slot.SkillId));
            _skillSlotItemList.Add(slot);
        }

        // 数量多出 → 删除末尾多余槽
        for (int i = needCount; i < slotCount; i++)
        {
            GameObject obj = _skillSlotItemList[^1].gameObject;
            _skillSlotItemList.RemoveAt(_skillSlotItemList.Count - 1);
            Destroy(obj);
        }

        // 重建 技能id → 槽 映射，并设置信息
        _skillSlotDic.Clear();
        for (int i = 0; i < needCount; i++)
        {
            int id = ids[i];
            UI_BagSkillSlot slot = _skillSlotItemList[i];
            Sprite icon = SkillConfigList[id].SkillIcon;
            slot.SetInfo(id, icon);
            _skillSlotDic[id] = slot;
        }

        RefreshEquipStates();
    }

    private void ShowSkillInfo(int skillID)
    {
        if (skillID < 0 || skillID >= SkillConfigList.Count) return;
        SkillConfig skillConfig = SkillConfigList[skillID];
        _txtName.text = skillConfig.SkillName;
        _txtDescription.text = _prefixStr + skillConfig.SkillDescription;
        _skillInfoObj.SetActive(true);
    }
}
