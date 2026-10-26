using System.Collections.Generic;
using cfg;
using DG.Tweening;
using Level;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

public class UI_LevelSelectView : MonoBehaviour
{
    [SerializeField] private Transform _viewport;
    [SerializeField, MinValue(0)] private float _space = 300;
    [SerializeField, Range(0, 1)] private float _duration = .2f;
    [SerializeField] private GameObject _itemPrefab;
    private Tween _tween;
    private UI_LevelSelectItem[] _items;
    public int CurLevelIndex { get; private set; } = 0;

    public void Init(IReadOnlyList<LevelConfig> levelConfig)
    {
        _items = new UI_LevelSelectItem[levelConfig.Count];
        Vector3 pos = Vector3.zero;
        DataManager.LoadLevelData();

        for (int i = 0; i < levelConfig.Count; i++)
        {
            GameObject obj = Instantiate(_itemPrefab, pos, Quaternion.identity, _viewport);
            _items[i] = obj.GetComponent<UI_LevelSelectItem>();
            _items[i].Init(levelConfig[i]);
            pos.x += _space;
            _items[i].SetLock(!LevelManager.IsUnlock(levelConfig[i].Id));
            _items[i].SetComplete(LevelManager.IsComplete(levelConfig[i].Id));
        }

        if (_items.Length > 0)
            _items[0].transform.localScale = Vector3.one * 1.5f;
    }

    public void ToNextLevel()
    {
        _tween?.Kill();
        _items[CurLevelIndex].DoTweenScale(1, _duration);
        CurLevelIndex++;
        _items[CurLevelIndex].DoTweenScale(1.5f, _duration);
        _tween = _viewport.DOLocalMoveX(-_space * CurLevelIndex, _duration).SetEase(Ease.OutQuad);
    }

    public void ToPreviousLevel()
    {
        _tween?.Kill();
        _items[CurLevelIndex].DoTweenScale(1, _duration);
        CurLevelIndex--;
        _items[CurLevelIndex].DoTweenScale(1.5f, _duration);
        _tween = _viewport.DOLocalMoveX(-_space * CurLevelIndex, _duration).SetEase(Ease.OutQuad);
    }
}