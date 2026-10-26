using cfg;
using DG.Tweening;
using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class UI_LevelSelectItem : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _txtName;
        [SerializeField] private Image _imgLocked;
        [SerializeField] private Image _imgComplete;
        private LevelConfig _config;
        private Tween _tween;
        private UI_LevelInfoWindow _infoWindow;

        #region 生命周期相关

        public void Init(LevelConfig config)
        {
            _config = config;
            _icon.sprite = ResSystem.LoadAsset<Sprite>(config.LevelIcon);
            _txtName.text = config.LevelName;
        }

        private void OnDestroy()
        {
            if (_config == null) return;
            ResSystem.Release<Sprite>(_config.LevelIcon);
        }

        #endregion

        #region DoTween动画

        public void DoTweenScale(float scale, float duration, Ease ease = Ease.OutQuad)
        {
            _tween?.Kill();
            _tween = transform.DOScale(scale, duration).SetEase(ease);
        }

        #endregion

        #region 鼠标事件

        public void OnPointerEnter(PointerEventData eventData)
        {
            _infoWindow = UISystem.Show<UI_LevelInfoWindow>();
            _infoWindow?.ShowInfo(_config.LevelName, _config.LevelDescription);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            _infoWindow?.SetPos(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UISystem.Close<UI_LevelInfoWindow>();
            _infoWindow = null;
        }

        #endregion

        #region 相关函数

        public void SetLock(bool isLocked) => _imgLocked.enabled = isLocked;

        public void SetComplete(bool isComplete) => _imgComplete.enabled = isComplete;

        #endregion
    }
}