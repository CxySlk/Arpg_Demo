using DG.Tweening;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [RequireComponent(typeof(UIBehaviour))]
    public class UI_MouseEnterExitEffect : MonoBehaviour
    {
        [LabelText("UI元素"), SerializeField] private UIBehaviour component;
        [LabelText("放大比例"), SerializeField] private float _scale = 1.2f;
        [LabelText("持续时间"), SerializeField] private float _duration = 0.25f;

        [LabelText("DoTween模式"), SerializeField]
        private Ease _ease = Ease.OutQuad;

        private Tween _tween;
        private Vector3 _baseScale;

        private void Awake()
        {
            component ??= GetComponent<UIBehaviour>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            component.AddEventListener<PointerEventData>(EventSystemType.OnMouseEnter, OnMouseEnterCallBack);
            component.AddEventListener<PointerEventData>(EventSystemType.OnMouseExit, OnMouseExitCallBack);
        }

        private void OnDisable()
        {
            component.RemoveEventListener<PointerEventData>(EventSystemType.OnMouseEnter, OnMouseEnterCallBack);
            component.RemoveEventListener<PointerEventData>(EventSystemType.OnMouseExit, OnMouseExitCallBack);
        }

        private void OnMouseEnterCallBack(PointerEventData eventData)
        {
            _tween?.Kill();
            _tween = component.transform.DOScale(_baseScale * _scale, _duration).SetEase(_ease).SetUpdate(true);
        }

        private void OnMouseExitCallBack(PointerEventData eventData)
        {
            _tween?.Kill();
            _tween = component.transform.DOScale(_baseScale, _duration).SetEase(_ease).SetUpdate(true);
        }
    }
}