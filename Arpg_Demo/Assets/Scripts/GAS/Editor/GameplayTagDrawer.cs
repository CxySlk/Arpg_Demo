#if UNITY_EDITOR
using GAS;
using GAS.EditorData;
using ParadoxNotion.Design;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{
    /// <summary>
    /// 让 NodeCanvas 图里的 GameplayTag 字段显示标签下拉（与 Odin Inspector 同体验）。
    /// 由 PropertyDrawerFactory 通过反射自动发现并应用于所有 GameplayTag 字段。
    /// </summary>
    public class GameplayTagDrawer : ObjectDrawer<GameplayTag>
    {
        public override GameplayTag OnGUI(GUIContent content, GameplayTag instance)
        {
            List<string> tags = GASEditorData.Instance.GetTags();
            string current = instance.TagName;

            int currentIndex = current == null ? 0 : tags.IndexOf(current);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(content.text, currentIndex, tags.ToArray());

            if (newIndex >= 0 && newIndex < tags.Count && newIndex != currentIndex)
                instance.SetTagName(tags[newIndex]);

            return instance;
        }
    }
}
#endif
