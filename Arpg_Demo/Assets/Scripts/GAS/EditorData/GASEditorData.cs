using Sirenix.OdinInspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GAS.EditorData
{
    /// <summary>
    /// GAS 编辑器数据（标签全集）。放在独立程序集 GAS.EditorData 里，
    /// 运行时程序集只在 #if UNITY_EDITOR 下读它（见 GameplayTag 的标签下拉）。
    /// </summary>
    public class GASEditorData : SerializedScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>找不到现有资产时的兜底创建路径；正常情况通过 FindAssets 定位，不依赖固定目录。</summary>
        private const string FallbackAssetPath = "Assets/AoT/Scripts/GAS/EditorData/GASEditorData.asset";
        private static GASEditorData _instance;

        public static GASEditorData Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // 按类型查找：脚本目录改动后也不会读错/创建到旧路径。
                string[] guids = AssetDatabase.FindAssets("t:GASEditorData");
                if (guids.Length > 1)
                    Debug.LogWarning($"找到多个 GASEditorData 资产，使用第一个：{AssetDatabase.GUIDToAssetPath(guids[0])}");

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GASEditorData asset = AssetDatabase.LoadAssetAtPath<GASEditorData>(path);
                    if (asset == null) continue;

                    _instance = asset;
                    break;
                }

                if (_instance == null)
                {
                    _instance = CreateInstance<GASEditorData>();
                    AssetDatabase.CreateAsset(_instance, FallbackAssetPath);
                    AssetDatabase.SaveAssets();
                }

                return _instance;
            }
        }

        /// <summary>全量标签，由编辑器 UI 维护（添加时自动排序、判重，删除/修改由 Odin 列表提供）</summary>
        [BoxGroup("标签管理"), PropertySpace(0, 12), SerializeField,
         ListDrawerSettings(HideAddButton = true, ShowItemCount = true, DraggableItems = false, ElementColor = "red", HideRemoveButton = false)]
        private List<string> _tags = new();
        public List<string> Tags
        {
            get => _tags;
            set => _tags = value;
        }

        [BoxGroup("标签管理")]
        [GUIColor(0, 1, 0)]
        [Button("＋ 添加标签", ButtonSizes.Large, Icon = SdfIconType.Plus, Name = "添加标签", Style = ButtonStyle.FoldoutButton)]
        public void AddTag(string tagStr)
        {
            if (string.IsNullOrEmpty(tagStr) || _tags.Contains(tagStr)) return;
            _tags.Add(tagStr);
            _tags.Sort();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>自动排序（排序按钮触发）</summary>
        public void SortTags()
        {
            _tags.Sort();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>去除重复标签（参考 AbilitySystemComponentPreset.CheckRepeatAttributes）</summary>
        public void RemoveDuplicateTags()
        {
            HashSet<string> tagSet = new();
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                if (!tagSet.Add(_tags[i]))
                    _tags.RemoveAt(i);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public List<string> GetTags() => _tags;
#endif
    }
}
