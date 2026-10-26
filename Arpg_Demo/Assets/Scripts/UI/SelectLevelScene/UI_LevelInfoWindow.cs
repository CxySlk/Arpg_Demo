using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[UIWindow(nameof(UI_LevelInfoWindow), nameof(UI_LevelInfoWindow), true, 0)]
public class UI_LevelInfoWindow : UI_WindowBase
{
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDescription;

    [LabelText("光标与窗口的间隙"), SerializeField]
    private float _gap = 20f;

    public void ShowInfo(string name, string description)
    {
        _txtName.text = name;
        _txtDescription.text = description;

        // 改完文字后立刻重排，保证 SetPos 读到的宽高是当前内容的尺寸。
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    public void SetPos(PointerEventData eventData)
    {
        RectTransform self = (RectTransform)transform;
        RectTransform parent = self.parent as RectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        // 光标在父节点的哪一半：决定窗口的 pivot，也就是窗口朝哪个方向展开。
        bool isLeft = localPoint.x < parent.rect.center.x;
        bool isTop = localPoint.y > parent.rect.center.y;

        Vector2 pivot;
        pivot.x = isLeft ? 0f : 1f;
        pivot.y = isTop ? 1f : 0f;
        self.pivot = pivot;

        // 往窗口展开的方向推 gap，避免光标压在窗口角上。
        float offsetX = isLeft ? _gap : -_gap;
        float offsetY = isTop ? -_gap : _gap;
        self.localPosition = new Vector3(
            localPoint.x + offsetX,
            localPoint.y + offsetY,
            self.localPosition.z);

        // 按 pivot 换算成左下角，再夹在父节点范围内，避免贴边出屏。
        Vector2 size = self.rect.size;
        Vector2 bottomLeft = (Vector2)self.localPosition - Vector2.Scale(self.pivot, size);
        bottomLeft.x = Mathf.Clamp(bottomLeft.x, parent.rect.xMin, parent.rect.xMax - size.x);
        bottomLeft.y = Mathf.Clamp(bottomLeft.y, parent.rect.yMin, parent.rect.yMax - size.y);
        self.localPosition = new Vector3(
            bottomLeft.x + self.pivot.x * size.x,
            bottomLeft.y + self.pivot.y * size.y,
            self.localPosition.z);
    }
}
