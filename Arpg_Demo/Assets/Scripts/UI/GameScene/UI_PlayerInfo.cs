using cfg;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerInfo : MonoBehaviour
{
    [LabelText("昵称"), SerializeField] private TMP_Text _txtNickName;
    [LabelText("头像"), SerializeField] private Image _imgHeadIcon;
    [LabelText("血条"), SerializeField] private UI_Bar _hpBar;
    [LabelText("蓝条"), SerializeField] private UI_Bar _mpBar;

    private EventCenter EventCenter => PlayerController.Instance.EventCenter;
    
    public void Init()
    {
        _hpBar.Init();
        _mpBar.Init();
        _txtNickName.text = DataManager.PlayerData.nickName;
    }

    private void OnEnable()
    {
        EventCenter?.Subscribe(GASEventCenterKeys.NormalizeValueChangedKey, OnNormalizeValueChanged);
    }

    private void OnDisable()
    {
        EventCenter?.UnSubscribe(GASEventCenterKeys.NormalizeValueChangedKey, OnNormalizeValueChanged);
    }

    private void OnNormalizeValueChanged(EAttributeType attrType, float value)
    {
        switch (attrType)
        {
            case EAttributeType.Hp:
                _hpBar.SetProgress(value);
                break;
            case EAttributeType.Mp:
                _mpBar.SetProgress(value);
                break;
        }
    }
}