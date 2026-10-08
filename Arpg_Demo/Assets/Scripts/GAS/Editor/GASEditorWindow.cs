using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using GAS.EditorData;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    /// <summary>
    /// GAS 标签数据编辑器：顶部三个页签（属性/效果/其他）分类管理标签。
    /// 列表使用 Odin 默认集合编辑器（可删、改），添加自动排序，数据持久化到 GASEditorData。
    /// 变更即保存（OnCollectionChanged + OnDisable），不依赖窗口关闭。
    /// </summary>
    public class GASEditorWindow : OdinEditorWindow
    {
        private GASEditorData Data => GASEditorData.Instance;

        [TabGroup("Tabs", "标签"), LabelText("标签"), ShowInInspector,
         OnCollectionChanged("OnCollectionChangedCallback"),
         ListDrawerSettings(HideAddButton = true, ShowItemCount = true, DraggableItems = false, HideRemoveButton = false)]
        private List<string> Tags
        {
            get => Data.Tags;
            set => Data.Tags = value;
        }

        [TabGroup("Tabs", "标签"), HideLabel, ShowInInspector]
        [Button("添加标签", parameterBtnStyle: ButtonStyle.FoldoutButton, Icon = SdfIconType.Plus)]
        private void AddTags(string tagStr)
        {
            Data.AddTag(tagStr);
        }

        [TabGroup("Tabs", "标签"), HideLabel, ShowInInspector]
        [GUIColor(0.3f, 0.8f, 0.9f)]
        [Button("自动排序", ButtonSizes.Large, Icon = SdfIconType.SortAlphaDown)]
        private void SortButton()
        {
            Data.SortTags();
        }

        [TabGroup("Tabs", "标签"), HideLabel, ShowInInspector]
        [GUIColor(1f, 0.6f, 0.2f)]
        [Button("删除重复标签", ButtonSizes.Large, Icon = SdfIconType.Filter)]
        private void RemoveDuplicateButton()
        {
            Data.RemoveDuplicateTags();
        }

        public void OnCollectionChangedCallback(CollectionChangeInfo info, object value) => SaveData();

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Data != null) SaveData();
        }

        public void SaveData()
        {
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
        }
    }
}
