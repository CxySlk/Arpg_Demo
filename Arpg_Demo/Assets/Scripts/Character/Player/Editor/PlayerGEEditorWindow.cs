using GAS;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Player.Editor
{
    public class PlayerGEEditorWindow : OdinEditorWindow
    {
        [MenuItem("Player/PlayerGE")]
        private static void ShowWindow()
        {
            PlayerGEEditorWindow window = GetWindow<PlayerGEEditorWindow>();
            window.titleContent = new GUIContent("玩家GE管理器");
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private PlayerController _playerController;

        protected override void OnEnable()
        {
            base.OnEnable();
            _playerController = FindAnyObjectByType<PlayerController>();
        }

        /// <summary>编辑器帧循环中强制重绘，保证鼠标静止时数据也实时刷新</summary>
        private void Update()
        {
            Repaint();
        }

        protected override void OnImGUI()
        {
            if (_playerController == null || _playerController.Asc == null)
            {
                EditorGUILayout.HelpBox("场景中未找到 PlayerController / ASC", MessageType.Warning);
                base.OnImGUI();
                return;
            }
            DrawAttributes();
            base.OnImGUI();
        }

        private void DrawAttributes()
        {
            Dictionary<AttributeType, Attribute> attrDic = _playerController.Asc.AttrDic;
            Color originalBg = GUI.backgroundColor;
            Color progressFg = new(0.25f, 0.56f, 1f);
            Color progressBg = new(0.10f, 0.13f, 0.20f);
            Color buttonBg = new(0.23f, 0.26f, 0.31f);

            foreach (KeyValuePair<AttributeType, Attribute> kv in attrDic)
            {
                SirenixEditorGUI.BeginBox();

                // 属性名（左）+ 进度条（右）同一行
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(kv.Key.ToString(), EditorStyles.boldLabel, GUILayout.Width(90));
                Rect rect = EditorGUILayout.GetControlRect(false, 20f);
                SirenixEditorFields.ProgressBarField(rect, GUIContent.none,
                    kv.Value.CurValue, kv.Value.MinValue, kv.Value.MaxValue,
                    new ProgressBarConfig(20, progressFg, progressBg, true, TextAlignment.Center),
                    $"{FormatValue(kv.Value.CurValue)}/{FormatValue(kv.Value.MaxValue)}");
                EditorGUILayout.EndHorizontal();

                // 三个按钮一行，居中
                GUI.backgroundColor = buttonBg;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("−10", GUILayout.Width(70), GUILayout.Height(24))) 
                    kv.Value.AddCurrentValue(-10);
                if (GUILayout.Button("中值", GUILayout.Width(70), GUILayout.Height(24))) 
                    kv.Value.SetCurrentValue((kv.Value.MinValue + kv.Value.MaxValue) / 2f);
                if (GUILayout.Button("+10", GUILayout.Width(70), GUILayout.Height(24))) 
                    kv.Value.AddCurrentValue(10);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = originalBg;

                SirenixEditorGUI.EndBox();
                EditorGUILayout.Space(8);
            }
        }

        /// <summary>整数显示整数，小数显示小数（最多 2 位，去尾零）</summary>
        private static string FormatValue(float value)
        {
            if (Mathf.Approximately(value, Mathf.Round(value)))
                return value.ToString("0");
            return value.ToString("0.##");
        }
    }
}
