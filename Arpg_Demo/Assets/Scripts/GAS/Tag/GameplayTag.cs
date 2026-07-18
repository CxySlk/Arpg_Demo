using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct GameplayTag
{
    [SerializeField, ShowInInspector, ReadOnly, LabelText("哈希值")] private int _hashCode;
    [SerializeField, ShowInInspector, ReadOnly, LabelText("祖先的哈希值")] private int[] _ancestorHashCode;

    public int HashCode => _hashCode;
    public int[] AncestorHashCode => _ancestorHashCode;

    #region Editor
#if UNITY_EDITOR
    [SerializeField, OnValueChanged(nameof(OnTagNameChanged)), ValueDropdown(nameof(TagGetter)), LabelText("标签名")]
    private string tagName;
    [SerializeField, ShowInInspector, ReadOnly, LabelText("标签简称")] private string _shortName;
    [SerializeField, ShowInInspector, ReadOnly, LabelText("祖先标签名")] private string[] _ancestorNames;
    private void OnTagNameChanged()
    {
        _hashCode = tagName.GetHashCode();
        string[] ancestorShortName = tagName.Split('.');
        // 分配空间
        _ancestorHashCode = new int[ancestorShortName.Length - 1];
        _ancestorNames = new string[ancestorShortName.Length - 1];

        string ancestorName = "";
        for (int i = 0; i < ancestorShortName.Length - 1; i++)
        {
            if (i != 0) ancestorName += '.';
            ancestorName += ancestorShortName[i];
            _ancestorHashCode[i] = ancestorName.GetHashCode();
            _ancestorNames[i] = ancestorName;
        }
        // 方便查看
        _shortName = ancestorShortName.Last();
    }

    private IList<string> TagGetter() => GASEditorData.Instance.tagSet;
#endif
    #endregion

    public override int GetHashCode() => HashCode;
    public override bool Equals(object obj) => obj is GameplayTag tag && _hashCode == tag.HashCode;
}
