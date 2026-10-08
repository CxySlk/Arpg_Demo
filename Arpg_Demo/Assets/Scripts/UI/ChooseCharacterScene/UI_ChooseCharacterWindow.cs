using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Character.Player.Config;
using GAS;
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

    // 角色配置
    [LabelText("角色配置列表数据"), SerializeField]
    private PlayerConfigList _playerConfigList;

    [LabelText("属性根物体"), SerializeField] private Transform _propertyRoot;

    // 属性模板
    [LabelText("属性模板"), SerializeField] private GameObject _attrPrefab;

    private readonly List<UI_AttrPart> _attrPartList = new();

    private int _curSelIndex = 0;

    private Transform _playerView;

    public override void Show(int layer)
    {
        base.Show(layer);

        // 按钮事件监听
        _btnLeft.onClick.AddListener(OnLeftBtnClicked);
        _btnRight.onClick.AddListener(OnRightBtnClicked);

        _btnSure.onClick.AddListener(() =>
        {
            // 创建新存档
            DataManager.CreateArchive();
            // 保存角色配置
            DataManager.InitPlayerData(_curSelIndex);
            DataManager.SetPlayerConfig(_playerConfigList.ConfigList[_curSelIndex]);
            DataManager.SavePlayerData();

            AudioSystem.PlayEffectMusic("ButtonClick");
            LoadSceneBySceneName("GameScene");
        });

        _btnBack.onClick.AddListener(() =>
        {
            AudioSystem.PlayEffectMusic("ButtonClick");
            LoadSceneBySceneName("BeginScene");
        });

        // 显示第一个角色配置
        ChangeCharacter(0);
        // 监听拖拽模型事件
        _imgModelDrag.AddEventListener<PointerEventData>(EventSystemType.OnDrag,
            (data) => RotateModel(-data.delta.x * 5));
    }

    private void RotateModel(float angle)
    {
        _playerView.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _playerView.rotation;
    }

    private void LoadSceneBySceneName(string sceneName)
    {
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync(sceneName, () => { UISystem.Close<UI_LoadingSceneWindow>(); });
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
        if (_playerView != null) Destroy(_playerView.gameObject);

        if (index < 0 || index >= _playerConfigList.ConfigList.Count) return;
        _curSelIndex = index;
        PlayerConfig playerConfig = _playerConfigList.ConfigList[_curSelIndex];
        _playerView = ResSystem.LoadAsset<GameObject>(playerConfig.ViewPrefabPath).transform;
        _playerView.position = Vector3.zero;
        _playerView.rotation = Quaternion.identity;
        _txtName.text = playerConfig.PlayerName;
        _txtDescription.text = playerConfig.Description;

        // 角色属性配置
        Dictionary<AttributeType, Attribute> attrDic = playerConfig.AscPreset.AttrDic;
        // 动态生成属性显示UI
        int addCount = attrDic.Count - _attrPartList.Count;
        for (int i = 0; i < addCount; i++)
        {
            _attrPartList.Add(Instantiate(_attrPrefab, _propertyRoot, false).GetComponent<UI_AttrPart>());
        }

        // 动态设置UI属性部分的总高度
        float attrHeight = (_attrPrefab.transform as RectTransform)!.sizeDelta.y * attrDic.Count;
        RectTransform propertyTransform = _propertyRoot as RectTransform;
        propertyTransform!.sizeDelta = new Vector2(propertyTransform.sizeDelta.x, attrHeight);

        // 属性显示
        int _index = 0;
        foreach (KeyValuePair<AttributeType, Attribute> kv in attrDic)
        {
            Attribute attr = kv.Value;
            _attrPartList[_index].SetInfo(kv.Key.ToString(), attr.CurValue + "/" + attr.MaxValue);
            _attrPartList[_index].gameObject.SetActive(true);
            _index++;
        }

        for (; _index < _attrPartList.Count; _index++)
        {
            _attrPartList[_index].gameObject.SetActive(false);
        }
    }

    public void SetModelImage(RenderTexture modelImage)
    {
        _modelImage.texture = modelImage;
    }
}