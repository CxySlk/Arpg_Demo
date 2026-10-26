using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using cfg;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Attribute = GAS.Attribute;

[UIWindow(nameof(UI_ChooseCharacterWindow), nameof(UI_ChooseCharacterWindow), false, 0)]
public class UI_ChooseCharacterWindow : UI_WindowBase
{
    #region UI组件相关

    // 角色切换按钮
    [FormerlySerializedAs("btnLeft")] [LabelText("左切换按钮"), SerializeField]
    private Button _btnLeft;

    [FormerlySerializedAs("btnRight")] [LabelText("右切换按钮"), SerializeField]
    private Button _btnRight;

    // 开始按钮
    [FormerlySerializedAs("btnSure")] [LabelText("确定按钮"), SerializeField]
    private Button _btnSure;

    [FormerlySerializedAs("btnBack")] [LabelText("返回按钮"), SerializeField]
    private Button _btnBack;

    // 模型纹理
    [LabelText("模型显示"), SerializeField] private RawImage _modelImage;

    // 拖拽区域
    [FormerlySerializedAs("imgModelDrag")] [LabelText("角色模型拖拽区域"), SerializeField]
    private Image _imgModelDrag;

    // 角色文本相关
    [FormerlySerializedAs("_txtNmae")] [FormerlySerializedAs("txtNmae")] [LabelText("角色名"), SerializeField]
    private TMP_Text _txtName;

    [FormerlySerializedAs("txtDescription")] [LabelText("角色简介"), SerializeField]
    private TMP_Text _txtDescription;

    #endregion

    // 角色配置：运行时从 Addressables 加载（address = 类型名），OnDestroy 释放
    private IReadOnlyList<PlayerConfig> _playerConfigList;

    [LabelText("属性根物体"), SerializeField] private Transform _propertyRoot;

    // 属性模板
    [LabelText("属性模板"), SerializeField] private GameObject _attrPrefab;

    [LabelText("属性显示配置(属性→中文名)")] [DictionaryDrawerSettings(KeyLabel = "属性类型", ValueLabel = "显示名")] [SerializeField]
    private Dictionary<EAttributeType, string> _attrNameDic = new();

    private readonly List<UI_AttrPart> _attrPartList = new();

    private int _curSelIndex = 0;

    private Transform _playerView;
    private string _viewPrefabKey; // 预览模型加载时用的 Addressables 地址，释放时必须用同一个

    public override void Show(int layer)
    {
        base.Show(layer);

        // 角色配置表在 Addressables 里（address = 类型名），本窗口自己加载、销毁时释放
        _playerConfigList = ConfigManager.Tables.TbPlayerConfig.DataList;

        // 按钮事件监听
        _btnLeft.onClick.AddListener(OnLeftBtnClicked);
        _btnRight.onClick.AddListener(OnRightBtnClicked);

        _btnSure.onClick.AddListener(OnSureBtnClicked);

        _btnBack.onClick.AddListener(OnBackBtnClicked);

        // 显示第一个角色配置
        ChangeCharacter(0);
        // 监听拖拽模型事件
        _imgModelDrag.AddEventListener<PointerEventData>(EventSystemType.OnDrag,
            (data) => RotateModel(-data.delta.x * 5));
    }

    private void OnBackBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        LoadSceneBySceneName("BeginScene");
    }

    private void OnSureBtnClicked()
    {
        // 创建新存档
        DataManager.CreateArchive();
        // 保存角色配置
        DataManager.InitPlayerData(_curSelIndex);
        DataManager.SetPlayerConfig(_playerConfigList[_curSelIndex]);
        DataManager.SavePlayerData();

        AudioSystem.PlayEffectMusic("ButtonClick");
        LoadSceneBySceneName("SelectLevelScene");
    }

    private void RotateModel(float angle)
    {
        _playerView.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _playerView.rotation;
    }

    private void LoadSceneBySceneName(string sceneName)
    {
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync(sceneName, UISystem.Close<UI_LoadingSceneWindow>);
        UISystem.Close<UI_ChooseCharacterWindow>();
    }

    public void OnLeftBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        ChangeCharacter(_curSelIndex - 1);
    }

    public void OnRightBtnClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        ChangeCharacter(_curSelIndex + 1);
    }

    private void ChangeCharacter(int index)
    {
        if (index < 0 || index >= _playerConfigList.Count) return;
        ReleasePlayerView();
        _curSelIndex = index;
        PlayerConfig playerConfig = _playerConfigList[_curSelIndex];
        _viewPrefabKey = playerConfig.ViewPrefabPath;
        _playerView = ResSystem.LoadAsset<GameObject>(_viewPrefabKey).transform;
        _playerView.GetComponent<CharacterView>().Play("Idle");
        _playerView.position = Vector3.zero;
        _playerView.rotation = Quaternion.identity;
        _txtName.text = playerConfig.Name;
        _txtDescription.text = playerConfig.Description;

        // 角色属性UI显示
        IReadOnlyList<CharacterAttr> allAttrs = ConfigManager.Tables.TbCharacterAttr.DataList;
        List<CharacterAttr> attrs = new();
        int playerID = playerConfig.Id;
        foreach (CharacterAttr attr in allAttrs)
        {
            if (attr.CharacterId == playerID)
            {
                switch (attr.AttrType)
                {
                    case EAttributeType.Hp:
                    case EAttributeType.Mp:
                    case EAttributeType.Atk:
                    case EAttributeType.Def:
                    case EAttributeType.HpRegenSpeed:
                    case EAttributeType.MpRegenSpeed:
                        attrs.Add(attr);
                        break;
                }
            }
        }

        int displayCount = attrs.Count; // 按"要显示的属性个数"，不再用总属性数

        // 动态生成属性显示UI
        for (int i = _attrPartList.Count; i < displayCount; i++)
        {
            _attrPartList.Add(Instantiate(_attrPrefab, _propertyRoot, false).GetComponent<UI_AttrPart>());
        }

        // 属性显示：中文名 + 配置顺序；属性缺失则跳过
        int shown = 0;
        TbCharacterAttr tbAttr = ConfigManager.Tables.TbCharacterAttr;
        foreach (KeyValuePair<EAttributeType, string> kv in _attrNameDic)
        {
            CharacterAttr attr = tbAttr.Get(playerID, kv.Key);
            if (attr == null) continue;
            _attrPartList[shown].SetInfo(kv.Value, attr.Cur + "/" + attr.Max);
            _attrPartList[shown].gameObject.SetActive(true);
            shown++;
        }

        for (int i = shown; i < _attrPartList.Count; i++)
        {
            _attrPartList[i].gameObject.SetActive(false);
        }

        // 动态设置UI属性部分的总高度（按实际显示条数）
        float attrHeight = (_attrPrefab.transform as RectTransform)!.sizeDelta.y * shown;
        RectTransform propertyTransform = _propertyRoot as RectTransform;
        propertyTransform!.sizeDelta = new Vector2(propertyTransform.sizeDelta.x, attrHeight);
    }

    public void SetModelImage(RenderTexture modelImage)
    {
        _modelImage.texture = modelImage;
    }

    /// <summary>销毁并释放当前预览模型（它是场景根物体，不挂在窗口下，窗口销毁不会连带销毁）</summary>
    private void ReleasePlayerView()
    {
        if (!_playerView) return;
        Destroy(_playerView.gameObject);
        ResSystem.Release<GameObject>(_viewPrefabKey);
        _playerView = null;
        _viewPrefabKey = null;
    }

    protected override void OnDestroy()
    {
        ReleasePlayerView();
        // 释放配置表之前先清掉全局引用，避免留下「引用还在、资产已卸载」的假空对象
        DataManager.SetPlayerConfig(null);
        base.OnDestroy();
    }
}