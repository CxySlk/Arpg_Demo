using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家死亡面板:半透明遮罩 + "你死了" 标题 + 复活按钮。
/// 复活按钮点击 → PlayerController.Revive()(回满血/回出生点/恢复控制),并关闭本面板。
/// </summary>
[UIWindow(nameof(UI_DeathWindow), nameof(UI_DeathWindow), false, 4)]
public class UI_DeathWindow : UI_WindowBase
{
    [LabelText("复活按钮文字"), SerializeField] private string _reviveText = "复活";

    private Button _reviveButton;
    private bool _uiBuilt;

    public override void OnShow()
    {
        base.OnShow();
        BuildUI();
    }

    protected override void OnClose()
    {
        base.OnClose();
        if (_reviveButton != null)
            _reviveButton.onClick.RemoveListener(OnReviveClick);
        _reviveButton = null;
        _uiBuilt = false;
    }

    /// <summary>代码构建面板 UI(根节点下):遮罩 Image + 标题 TMP + 复活按钮。</summary>
    private void BuildUI()
    {
        if (_uiBuilt) return;
        _uiBuilt = true;

        // 根铺满父(窗口根),半透明黑遮罩挡操作
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        Image mask = CreateImage("Mask", new Color(0f, 0f, 0f, 0.6f), root, 0);
        Stretch(mask.rectTransform);

        // 标题
        TextMeshProUGUI title = CreateText("Title", "你死了", 72, Color.white, root, 1);
        CenterAt(title.rectTransform, new Vector2(0f, 60f));

        // 复活按钮(内容用代码加,按钮自带可点击区域)
        GameObject btnGo = new("ReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(root, false);
        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.45f, 0.85f, 1f);
        Stretch(btnGo.GetComponent<RectTransform>(), new Vector2(0f, -120f), new Vector2(220f, 80f));

        TextMeshProUGUI btnText = CreateText("Text", _reviveText, 36, Color.white, btnGo.transform as RectTransform, 0);
        Stretch(btnText.rectTransform);

        _reviveButton = btnGo.GetComponent<Button>();
        _reviveButton.onClick.AddListener(OnReviveClick);
    }

    private void OnReviveClick()
    {
        // 玩家控制器复活并关闭面板
        if (PlayerController.Instance != null)
            PlayerController.Instance.Revive();
        UISystem.Close<UI_DeathWindow>();
    }

    #region UI 工具

    private static Image CreateImage(string name, Color color, Transform parent, int sibling)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(sibling);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true; // 遮罩拦截点击
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, string text, float fontSize, Color color,
        RectTransform parent, int sibling)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(sibling);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rt, Vector2 centerOffset, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = centerOffset;
    }

    private static void CenterAt(RectTransform rt, Vector2 anchored)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 120f);
        rt.anchoredPosition = anchored;
    }

    #endregion
}
