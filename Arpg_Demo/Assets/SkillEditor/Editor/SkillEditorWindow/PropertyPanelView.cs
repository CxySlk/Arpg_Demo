using System;
using System.Collections.Generic;
using System.Reflection;
using GAS;
using GAS.EditorData;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 属性面板：选中事件的通用字段 + 类型专属配置（Cue 模板 / GE 捕捉器与效果列表）。
    /// 内容变化通过事件广播，由主窗口协调轨道重建与粒子预览刷新。
    /// </summary>
    public class PropertyPanelView
    {
        private readonly SkillEditorUI _ui;
        private AbilityAsset _asset;
        private VisualElement _root;

        /// <summary>当前内联绘制中的效果对象树（Rebuild 时释放）；每个效果一棵，按 Inspector 的方式绘制</summary>
        private readonly Dictionary<GameplayEffect, PropertyTree> _effectTrees = new();

        /// <summary>绘制 Effect 列表时复用的缓存（避免每帧分配）</summary>
        private readonly List<GameplayEffect> _effectKeysCache = new();
        private readonly List<Type> _effectTypes = new();
        private readonly List<string> _effectTypeNames = new();
        private string[] _effectTypeArray = new string[0];
        private int _effectTypeIndex;

        /// <summary>每个效果面板的折叠状态（效果列表长了之后只展开要配的那个）</summary>
        private readonly Dictionary<GameplayEffect, bool> _effectFoldouts = new();
        /// <summary>清理折叠状态时的复用缓存</summary>
        private readonly List<GameplayEffect> _staleEffectKeys = new();

        /// <summary>内容变化（时长等）→ 需重建右侧轨道</summary>
        public event Action ContentChanged;
        /// <summary>起始帧变化 → 只刷新事件条位置,不重建列表、不重新排序</summary>
        public event Action<MarkEventBase> StartFrameChanged;
        /// <summary>事件名变化</summary>
        public event Action<MarkEventBase, string> NameChanged;
        /// <summary>粒子 Cue 位置/旋转变化 → 刷新预览实例</summary>
        public event Action<DurationCueMarkEvent> ParticleTransformChanged;

        public PropertyPanelView(SkillEditorUI ui)
        {
            _ui = ui;
            ScrollView propertyScrollView = new(ScrollViewMode.VerticalAndHorizontal) { style = { flexGrow = 1, paddingTop = 2 } };
            _root = new VisualElement
            {
                style =
                {
                    flexGrow = 1, flexShrink = 0, minWidth = SkillEditorConfig.PropertyPanelContentWidth,
                    paddingLeft = 10, paddingTop = 6, paddingRight = 8
                }
            };
            propertyScrollView.Add(_root);
            _ui.contentRightMost.Add(propertyScrollView);
        }

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        public void Rebuild(MarkEventBase mark)
        {
            // 上一次内联绘制的效果对象树先释放
            ClearEffectTrees();

            _root.Clear();
            if (mark == null)
            {
                _root.Add(new Label("未选中事件"));
                return;
            }

            _root.Add(new Label($"{SkillEditorConfig.GetMarkTypeName(mark)}（{mark.GetType().Name}）"));

            BuildNameField(mark);
            BuildStartFrameField(mark);

            if (mark is DurationCueMarkEvent cueMark)
                BuildCueFields(cueMark);
            else if (mark is ReleaseGEMarkEvent geMark)
                BuildGEFields(geMark);
            else if (mark is FrameEvent frameEvent)
                BuildFrameEventFields(frameEvent);
        }

        private void BuildNameField(MarkEventBase mark)
        {
            VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            row.Add(new Label("名称") { style = { width = 60 } });
            TextField nameField = new TextField { value = mark.Name, style = { flexGrow = 1 } };
            nameField.RegisterValueChangedCallback(evt =>
            {
                mark.Name = evt.newValue;
                EditorUtility.SetDirty(_asset);
                NameChanged?.Invoke(mark, evt.newValue);
            });
            row.Add(nameField);
            _root.Add(row);
        }

        private void BuildStartFrameField(MarkEventBase mark)
        {
            VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            row.Add(new Label("起始帧") { style = { width = 60 } });
            IntegerField frameField = new IntegerField { value = mark.StartFrame, style = { flexGrow = 1 } };
            frameField.RegisterValueChangedCallback(evt =>
            {
                if (_asset.MoveMarkEvent(mark, evt.newValue))
                {
                    EditorUtility.SetDirty(_asset);
                    // 起始帧变化只挪事件条,不重建列表(避免排序跳动;重新排序交给“保存配置”)
                    StartFrameChanged?.Invoke(mark);
                }
                else
                {
                    Debug.LogWarning("该帧已存在 GE 事件，同一帧最多一个 GE，无法移动");
                    frameField.SetValueWithoutNotify(mark.StartFrame);
                }
            });
            row.Add(frameField);
            _root.Add(row);
        }

        #region Cue 事件字段

        private void BuildCueFields(DurationCueMarkEvent cueMark)
        {
            _root.Add(new Label("持续Cue") { style = { marginTop = 8 } });

            // 模板类型选择（普通类实例，下拉创建）
            VisualElement cueRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Button cueBtn = new(() =>
            {
                GenericMenu menu = new();
                menu.AddItem(new GUIContent("动画"), cueMark.DurationCue is GameplayCueDurationalAnimation,
                    () => SetCue(cueMark, new GameplayCueDurationalAnimation()));
                menu.AddItem(new GUIContent("音效"), cueMark.DurationCue is GameplayCueDurationalAudio,
                    () => SetCue(cueMark, new GameplayCueDurationalAudio()));
                menu.AddItem(new GUIContent("粒子"), cueMark.DurationCue is GameplayCueDurationalParticle,
                    () => SetCue(cueMark, new GameplayCueDurationalParticle()));
                menu.AddItem(new GUIContent("移除Cue"), cueMark.DurationCue == null,
                    () => SetCue(cueMark, null));
                menu.ShowAsContext();
            })
            { text = cueMark.DurationCue == null ? "＋Cue" : "更换Cue", style = { width = 100 } };
            cueRow.Add(cueBtn);
            cueRow.Add(new Label(cueMark.DurationCue?.GetType().Name ?? "无") { style = { marginLeft = 6 } });
            _root.Add(cueRow);

            // 内嵌模板字段绘制（反射 + EditorGUILayout，普通类非资产）
            if (cueMark.DurationCue != null)
            {
                IMGUIContainer cueInspector = new(() =>
                {
                    if (cueMark.DurationCue == null) return;
                    GameplayCueDurational cue = cueMark.DurationCue;
                    FieldInfo[] fields = cue.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    EditorGUI.BeginChangeCheck();
                    foreach (FieldInfo field in fields)
                    {
                        if (field.Name == "DurationSeconds" || field.Name == "RootMotion") continue;
                        DrawFieldByType(cue, field);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_asset);
                        ContentChanged?.Invoke();
                        if (cueMark.DurationCue is GameplayCueDurationalParticle)
                            ParticleTransformChanged?.Invoke(cueMark);
                    }
                });
                cueInspector.style.marginTop = 6;
                cueInspector.style.paddingLeft = 8;
                cueInspector.style.flexGrow = 1;
                cueInspector.style.flexShrink = 0;
                cueInspector.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);
                _root.Add(cueInspector);
            }

            // 时长（秒）：DurationSeconds
            VisualElement durationRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            durationRow.Add(new Label("时长(秒)") { style = { width = 60 } });
            FloatField durationField = new FloatField { value = cueMark.DurationCue != null ? cueMark.DurationCue.DurationSeconds : 0f, style = { flexGrow = 1 } };
            durationField.SetEnabled(cueMark.DurationCue != null);
            durationField.RegisterValueChangedCallback(evt =>
            {
                if (cueMark.DurationCue == null) return;
                cueMark.DurationCue.DurationSeconds = Mathf.Max(0.01f, evt.newValue);
                EditorUtility.SetDirty(_asset);
                ContentChanged?.Invoke();
            });
            durationRow.Add(durationField);
            _root.Add(durationRow);

            // RootMotion 开关：仅动画 Cue
            if (cueMark.DurationCue is GameplayCueDurationalAnimation animCue)
            {
                VisualElement rmRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
                rmRow.Add(new Label("RootMotion") { style = { width = 60 } });
                Toggle rmToggle = new Toggle { value = animCue.RootMotion, style = { flexGrow = 1 } };
                rmToggle.RegisterValueChangedCallback(evt =>
                {
                    animCue.RootMotion = evt.newValue;
                    EditorUtility.SetDirty(_asset);
                });
                rmRow.Add(rmToggle);
                _root.Add(rmRow);
            }

            // 重算时长按钮
            Button calcBtn = new(() =>
            {
                if (cueMark.DurationCue == null) return;
                cueMark.DurationCue.CalculateDuration();
                EditorUtility.SetDirty(_asset);
                Rebuild(cueMark);
                ContentChanged?.Invoke();
            })
            { text = "重新计算时长", style = { marginTop = 4 } };
            calcBtn.SetEnabled(cueMark.DurationCue != null);
            _root.Add(calcBtn);
        }

        private void SetCue(DurationCueMarkEvent cueMark, GameplayCueDurational cue)
        {
            cueMark.DurationCue = cue;
            EditorUtility.SetDirty(_asset);
            Rebuild(cueMark);
            ContentChanged?.Invoke();
        }

        #endregion

        #region GE 事件字段

        private void BuildGEFields(ReleaseGEMarkEvent geMark)
        {
            // 目标捕捉器类型选择
            VisualElement catcherRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            catcherRow.Add(new Label("目标捕捉器") { style = { width = 90 } });
            Button catcherBtn = new(() =>
            {
                GenericMenu menu = new();
                menu.AddItem(new GUIContent("盒型"), geMark.targetCatcher is TargetBoxCatcher,
                    () => SetCatcher(geMark, new TargetBoxCatcher()));
                menu.AddItem(new GUIContent("球形"), geMark.targetCatcher is TargetSphereCatcher,
                    () => SetCatcher(geMark, new TargetSphereCatcher()));
                menu.AddItem(new GUIContent("扇形"), geMark.targetCatcher is TargetSectorCatcher,
                    () => SetCatcher(geMark, new TargetSectorCatcher()));
                menu.AddItem(new GUIContent("武器"), geMark.targetCatcher is WeaponCatcher,
                    () => SetCatcher(geMark, new WeaponCatcher()));
                menu.AddItem(new GUIContent("捕捉自身"), geMark.targetCatcher == null,
                    () => SetCatcher(geMark, null));
                menu.ShowAsContext();
            })
            { text = geMark.targetCatcher == null ? "＋捕捉器（自身）" : "更换捕捉器", style = { width = 100 } };
            catcherRow.Add(catcherBtn);
            catcherRow.Add(new Label(geMark.targetCatcher?.GetType().Name ?? "无") { style = { marginLeft = 6 } });
            _root.Add(catcherRow);

            // 捕捉器参数反射绘制
            if (geMark.targetCatcher != null)
            {
                IMGUIContainer catcherInspector = new(() =>
                {
                    if (geMark.targetCatcher == null) return;
                    TargetCatcherBase catcher = geMark.targetCatcher;
                    FieldInfo[] fields = catcher.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    EditorGUI.BeginChangeCheck();
                    foreach (FieldInfo field in fields)
                    {
                        Type fieldType = field.FieldType;
                        object curValue = field.GetValue(catcher);
                        string label = GetDisplayLabel(field);
                        if (fieldType == typeof(Vector3))
                        {
                            field.SetValue(catcher, EditorGUILayout.Vector3Field(label, (Vector3)curValue));
                            if (field.Name == "Rotation")
                            {
                                MethodInfo method = catcher.GetType().GetMethod("OnRotationChanged", BindingFlags.NonPublic | BindingFlags.Instance);
                                method?.Invoke(catcher, null);
                            }
                        }
                        else if (fieldType == typeof(float))
                            field.SetValue(catcher, EditorGUILayout.FloatField(label, (float)curValue));
                        else if (fieldType == typeof(int))
                            field.SetValue(catcher, EditorGUILayout.IntField(label, (int)curValue));
                        else if (fieldType == typeof(bool))
                            field.SetValue(catcher, EditorGUILayout.Toggle(label, (bool)curValue));
                        else if (fieldType == typeof(LayerMask))
                        {
                            LayerMask lm = (LayerMask)curValue;
                            lm.value = EditorGUILayout.MaskField(label, lm.value, GetLayerNames());
                            field.SetValue(catcher, lm);
                        }
                        else
                            EditorGUILayout.LabelField(label, curValue?.ToString() ?? "null");
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_asset);
                        SceneView.RepaintAll();
                    }
                });
                catcherInspector.style.marginTop = 4;
                catcherInspector.style.paddingLeft = 8;
                catcherInspector.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);
                _root.Add(catcherInspector);
            }

            // 持续作用配置：持续帧数 >1 时按帧持续监测；可重复作用决定同一目标是否可多次命中。
            // 作用间隔只对"可重复作用"有意义，关闭时该行隐藏（ShowIf 效果）。
            VisualElement durationRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            durationRow.Add(new Label("持续帧数") { style = { width = 90 } });
            IntegerField durationField = new IntegerField { value = Mathf.Max(1, geMark.durationFrames), style = { flexGrow = 1 } };
            durationField.RegisterValueChangedCallback(evt =>
            {
                int val = Mathf.Max(1, evt.newValue);
                if (val != evt.newValue) durationField.SetValueWithoutNotify(val);
                geMark.durationFrames = val;
                EditorUtility.SetDirty(_asset);
                // 条宽随持续帧数变化 → 重建右侧轨道
                ContentChanged?.Invoke();
            });
            durationRow.Add(durationField);
            _root.Add(durationRow);

            VisualElement repeatRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            repeatRow.Add(new Label("可重复作用") { style = { width = 90 } });
            Toggle repeatToggle = new Toggle { value = geMark.canRepeat, style = { flexGrow = 1 } };
            repeatRow.Add(repeatToggle);
            _root.Add(repeatRow);

            VisualElement intervalRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            intervalRow.Add(new Label("作用间隔(帧)") { style = { width = 90 } });
            IntegerField intervalField = new IntegerField { value = Mathf.Max(0, geMark.hitIntervalFrames), style = { flexGrow = 1 } };
            intervalField.RegisterValueChangedCallback(evt =>
            {
                int val = Mathf.Max(0, evt.newValue);
                if (val != evt.newValue) intervalField.SetValueWithoutNotify(val);
                geMark.hitIntervalFrames = val;
                EditorUtility.SetDirty(_asset);
            });
            intervalRow.Add(intervalField);
            intervalRow.style.display = geMark.canRepeat ? DisplayStyle.Flex : DisplayStyle.None;

            repeatToggle.RegisterValueChangedCallback(evt =>
            {
                geMark.canRepeat = evt.newValue;
                intervalRow.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                EditorUtility.SetDirty(_asset);
            });
            _root.Add(intervalRow);

            // Effect 列表（效果实例 → 层数）：
            // 整段交给一个 IMGUI 容器绘制，每个效果用 Odin 的 PropertyTree 按 Inspector 的方式画
            // （分组可折叠、数组/列表都能增删 —— 所以 cueList 这类字段在这里能看能改）
            if (geMark.Effects == null) geMark.Effects = new Dictionary<GameplayEffect, int>();

            // 效果对象树在这里（Rebuild 阶段）建好，绘制回调里只负责画，避免每帧 new
            foreach (GameplayEffect effect in geMark.Effects.Keys)
            {
                if (effect == null) continue;
                PropertyTree tree = PropertyTree.Create(effect);
                tree.OnPropertyValueChanged += OnEffectTreeValueChanged;
                _effectTrees[effect] = tree;
                if (!_effectFoldouts.ContainsKey(effect)) _effectFoldouts[effect] = true;
            }

            // 已经删掉的效果，顺手把它的折叠状态清掉
            _staleEffectKeys.Clear();
            foreach (GameplayEffect key in _effectFoldouts.Keys)
                if (!geMark.Effects.ContainsKey(key)) _staleEffectKeys.Add(key);
            foreach (GameplayEffect key in _staleEffectKeys) _effectFoldouts.Remove(key);

            _effectTypes.Clear();
            _effectTypeNames.Clear();
            foreach (Type type in GetEffectTypes())
            {
                _effectTypes.Add(type);
                _effectTypeNames.Add(type.Name);
            }
            _effectTypeArray = _effectTypeNames.ToArray();
            _effectTypeIndex = Mathf.Clamp(_effectTypeIndex, 0, Mathf.Max(0, _effectTypeArray.Length - 1));

            _root.Add(new IMGUIContainer(() => DrawEffectList(geMark)) { style = { marginTop = 8 } });
        }

        /// <summary>
        /// Effect 列表的 IMGUI 绘制：每行 = 折叠箭头 + 效果名 + 层数 + 删除，
        /// 展开时才挂 Odin 画的效果本体（效果多了只留要配的那个展开，不然面板太长）。
        /// </summary>
        private void DrawEffectList(ReleaseGEMarkEvent geMark)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Effect列表（效果 → 层数）", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("折叠全部", GUILayout.Width(60))) SetAllFoldouts(geMark, false);
            if (GUILayout.Button("展开全部", GUILayout.Width(60))) SetAllFoldouts(geMark, true);
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            _effectKeysCache.Clear();
            _effectKeysCache.AddRange(geMark.Effects.Keys);

            GameplayEffect removed = null;
            foreach (GameplayEffect effect in _effectKeysCache)
            {
                if (effect == null) continue;

                if (!_effectFoldouts.TryGetValue(effect, out bool expanded)) expanded = true;

                GUILayout.BeginHorizontal();
                Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                    GUILayout.MinWidth(170), GUILayout.ExpandWidth(true));
                bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, effect.GetType().Name, true);
                if (newExpanded != expanded) _effectFoldouts[effect] = newExpanded;

                GUILayout.Label("层数", GUILayout.Width(36));
                int stack = EditorGUILayout.IntField(geMark.Effects[effect], GUILayout.Width(60));
                if (stack >= 1 && stack != geMark.Effects[effect]) geMark.Effects[effect] = stack;
                if (GUILayout.Button("×", GUILayout.Width(24))) removed = effect;
                GUILayout.EndHorizontal();

                // 效果本体：Odin 按 Inspector 的方式绘制（子类字段、分组、列表增删都在）
                if (newExpanded && _effectTrees.TryGetValue(effect, out PropertyTree tree))
                    tree.Draw(false);
            }

            // 新增效果：先选类型，再 new 一个实例写进字典
            if (_effectTypeArray.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("新增效果", GUILayout.Width(60));
                _effectTypeIndex = EditorGUILayout.Popup(_effectTypeIndex, _effectTypeArray);
                if (GUILayout.Button("＋Effect", GUILayout.Width(80)))
                {
                    if (Activator.CreateInstance(_effectTypes[_effectTypeIndex]) is GameplayEffect created)
                    {
                        geMark.Effects[created] = 1;
                        EditorUtility.SetDirty(_asset);
                        RebuildLater(geMark);
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (removed != null)
            {
                geMark.Effects.Remove(removed);
                EditorUtility.SetDirty(_asset);
                RebuildLater(geMark);
            }

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_asset);
        }

        /// <summary>一键折叠/展开当前 ReleaseGE 事件下的所有效果</summary>
        private void SetAllFoldouts(ReleaseGEMarkEvent geMark, bool expanded)
        {
            foreach (GameplayEffect effect in geMark.Effects.Keys)
                if (effect != null) _effectFoldouts[effect] = expanded;
        }

        /// <summary>IMGUI 绘制回调里不能直接改 UI Toolkit 的树，延迟到本帧结束再重建。</summary>
        private void RebuildLater(MarkEventBase mark)
        {
            EditorApplication.delayCall += () =>
            {
                if (mark != null) Rebuild(mark);
            };
        }

        /// <summary>清掉上一次内联绘制的效果对象树</summary>
        private void ClearEffectTrees()
        {
            foreach (PropertyTree tree in _effectTrees.Values)
            {
                tree.OnPropertyValueChanged -= OnEffectTreeValueChanged;
                tree.Dispose();
            }
            _effectTrees.Clear();
        }

        /// <summary>
        /// Odin 树里的改动（标签下拉、cue/标签列表增删等）走的是 Odin 自己的控件，
        /// 不会触发外层的 GUI.changed，这里单独标脏，保证改完能存进资产。
        /// </summary>
        private void OnEffectTreeValueChanged(InspectorProperty property, int selectionIndex)
        {
            if (_asset != null) EditorUtility.SetDirty(_asset);
        }

        /// <summary>所有可配置的效果类型（GameplayEffect 的非抽象子类）</summary>
        private static List<Type> GetEffectTypes()
        {
            List<Type> result = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || !typeof(GameplayEffect).IsAssignableFrom(type)) continue;
                    result.Add(type);
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }

        private void SetCatcher(ReleaseGEMarkEvent geMark, TargetCatcherBase catcher)
        {
            geMark.targetCatcher = catcher;
            EditorUtility.SetDirty(_asset);
            Rebuild(geMark);
        }

        #endregion

        #region 自定义事件字段

        private void BuildFrameEventFields(FrameEvent frameEvent)
        {
            IMGUIContainer frameInspector = new(() =>
            {
                if (frameEvent == null) return;
                FieldInfo[] fields = frameEvent.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                EditorGUI.BeginChangeCheck();
                foreach (FieldInfo field in fields)
                {
                    DrawFieldByType(frameEvent, field);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_asset);
                    ContentChanged?.Invoke();
                }
            });
            frameInspector.style.marginTop = 6;
            frameInspector.style.paddingLeft = 8;
            frameInspector.style.flexGrow = 1;
            frameInspector.style.flexShrink = 0;
            frameInspector.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);
            _root.Add(frameInspector);
        }

        #endregion

        private void DrawFieldByType(object instance, FieldInfo field)
        {
            Type fieldType = field.FieldType;
            object curValue = field.GetValue(instance);
            string label = GetDisplayLabel(field);
            if (fieldType == typeof(AnimationClip))
                field.SetValue(instance, EditorGUILayout.ObjectField(label, (AnimationClip)curValue, typeof(AnimationClip), false));
            else if (fieldType == typeof(AudioClip))
                field.SetValue(instance, EditorGUILayout.ObjectField(label, (AudioClip)curValue, typeof(AudioClip), false));
            else if (fieldType == typeof(GameObject))
            {
                if (instance is GameplayCueDurationalParticle particle && field.Name == "ParticlePrefab")
                    field.SetValue(instance, DrawParticlePrefabField(label, (GameObject)curValue, particle));
                else
                    field.SetValue(instance, EditorGUILayout.ObjectField(label, (GameObject)curValue, typeof(GameObject), false));
            }
            else if (fieldType == typeof(Vector3))
            {
                Vector3 newValue = EditorGUILayout.Vector3Field(label, (Vector3)curValue);
                if (newValue != (Vector3)curValue)
                {
                    field.SetValue(instance, newValue);
                    if (field.Name == "Rotation" && instance is GameplayCueDurationalParticle)
                        InvokeRotationNormalize(instance);
                }
            }
            else if (fieldType == typeof(Vector2))
                field.SetValue(instance, EditorGUILayout.Vector2Field(label, (Vector2)curValue));
            else if (fieldType == typeof(Vector2Int))
                field.SetValue(instance, EditorGUILayout.Vector2IntField(label, (Vector2Int)curValue));
            else if (fieldType == typeof(Vector3Int))
                field.SetValue(instance, EditorGUILayout.Vector3IntField(label, (Vector3Int)curValue));
            else if (fieldType == typeof(Color))
                field.SetValue(instance, EditorGUILayout.ColorField(label, (Color)curValue));
            else if (fieldType == typeof(LayerMask))
            {
                LayerMask lm = (LayerMask)curValue;
                lm.value = EditorGUILayout.MaskField(label, lm.value, GetLayerNames());
                field.SetValue(instance, lm);
            }
            else if (fieldType == typeof(Quaternion))
                field.SetValue(instance, Quaternion.Euler(EditorGUILayout.Vector3Field(label, ((Quaternion)curValue).eulerAngles)));
            else if (fieldType == typeof(Rect))
                field.SetValue(instance, EditorGUILayout.RectField(label, (Rect)curValue));
            else if (fieldType == typeof(Bounds))
                field.SetValue(instance, EditorGUILayout.BoundsField(label, (Bounds)curValue));
            else if (fieldType == typeof(AnimationCurve))
                field.SetValue(instance, EditorGUILayout.CurveField(label, (AnimationCurve)curValue));
            else if (fieldType == typeof(Gradient))
                field.SetValue(instance, EditorGUILayout.GradientField(label, (Gradient)curValue));
            else if (fieldType == typeof(float))
                field.SetValue(instance, EditorGUILayout.FloatField(label, (float)curValue));
            else if (fieldType == typeof(double))
                field.SetValue(instance, EditorGUILayout.DoubleField(label, (double)curValue));
            else if (fieldType == typeof(int))
                field.SetValue(instance, EditorGUILayout.IntField(label, (int)curValue));
            else if (fieldType == typeof(long))
                field.SetValue(instance, EditorGUILayout.LongField(label, (long)curValue));
            else if (fieldType == typeof(bool))
                field.SetValue(instance, EditorGUILayout.Toggle(label, (bool)curValue));
            else if (fieldType == typeof(string))
                field.SetValue(instance, EditorGUILayout.TextField(label, (string)curValue));
            else if (fieldType.IsEnum)
                field.SetValue(instance, DrawEnumPopup(label, fieldType, (Enum)curValue));
            else if (fieldType == typeof(object))
            {
                object objValue = curValue;
                UnityEngine.Object objRef = objValue as UnityEngine.Object;
                UnityEngine.Object newObj = EditorGUILayout.ObjectField(label, objRef, typeof(UnityEngine.Object), true);
                if (newObj != objRef)
                    field.SetValue(instance, newObj);
            }
            else if (fieldType == typeof(GameplayTagSet))
                DrawTagSetEditor((GameplayTagSet)curValue, label);
            else
                EditorGUILayout.LabelField(label, curValue?.ToString() ?? "null");
        }

        /// <summary>字段显示名：优先 Odin LabelText（中文标签），回退字段名</summary>
        private static string GetDisplayLabel(FieldInfo field)
        {
            LabelTextAttribute label = field.GetCustomAttribute<LabelTextAttribute>();
            return label != null ? label.Text : field.Name;
        }

        /// <summary>枚举弹窗：枚举成员带 Odin LabelText 时显示中文选项</summary>
        private static Enum DrawEnumPopup(string label, Type enumType, Enum curValue)
        {
            string[] names = Enum.GetNames(enumType);
            GUIContent[] contents = new GUIContent[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo member = enumType.GetField(names[i]);
                LabelTextAttribute lt = member != null ? member.GetCustomAttribute<LabelTextAttribute>() : null;
                contents[i] = new GUIContent(lt != null ? lt.Text : names[i]);
            }
            int curIndex = Array.IndexOf(names, curValue.ToString());
            int newIndex = EditorGUILayout.Popup(new GUIContent(label), curIndex, contents);
            if (newIndex >= 0 && newIndex != curIndex)
                return (Enum)Enum.Parse(enumType, names[newIndex]);
            return curValue;
        }

        /// <summary>可编辑标签集：现有标签列表（可删除）+ 下拉添加（数据来自 GASEditorData 全量标签）</summary>
        private void DrawTagSetEditor(GameplayTagSet tagSet, string label)
        {
            if (tagSet == null)
            {
                EditorGUILayout.LabelField(label, "null");
                return;
            }
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            List<GameplayTag> tags = new(tagSet.Tags);
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag tag = tags[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tag.TagName ?? tag.ShortTagName);
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    tagSet.RemoveTag(tag);
                    EditorUtility.SetDirty(_asset);
                }
                EditorGUILayout.EndHorizontal();
            }

            List<string> allTags = GASEditorData.Instance.GetTags();
            List<string> available = new();
            for (int i = 0; i < allTags.Count; i++)
            {
                if (!tagSet.HasTag(MakeTag(allTags[i])))
                    available.Add(allTags[i]);
            }
            if (available.Count > 0)
            {
                int selected = EditorGUILayout.Popup("＋添加标签", -1, available.ToArray());
                if (selected >= 0 && selected < available.Count)
                {
                    tagSet.AddTag(MakeTag(available[selected]));
                    EditorUtility.SetDirty(_asset);
                }
            }
            else
            {
                EditorGUILayout.LabelField("＋添加标签", "无可用标签", EditorStyles.miniLabel);
            }
        }

        private static GameplayTag MakeTag(string tagName)
        {
            GameplayTag tag = new();
            tag.SetTagName(tagName);
            return tag;
        }

        /// <summary>调用各类的 OnRotationChanged（私有，UNITY_EDITOR）做旋转归一化</summary>
        private static void InvokeRotationNormalize(object instance)
        {
            MethodInfo method = instance.GetType().GetMethod("OnRotationChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(instance, null);
        }

        /// <summary>
        /// 粒子预制体：直接把预制体拖到这个字段上（不再用弹窗选择器）。
        /// 只接受工程里的预制体（场景物体拖不进来），且它自身或子物体必须带 ParticleSystem；
        /// 不合格就保留原值并在 Console 里给出原因。
        /// </summary>
        private GameObject DrawParticlePrefabField(string label, GameObject current, GameplayCueDurationalParticle particle)
        {
            // allowSceneObjects: false —— 场景里的物体不允许拖入
            GameObject selected = (GameObject)EditorGUILayout.ObjectField(label, current, typeof(GameObject), false);
            if (selected == current) return current;

            if (selected == null)
            {
                particle.ParticlePrefab = null;
                EditorUtility.SetDirty(_asset);
                return null;
            }

            if (!EditorUtility.IsPersistent(selected))
            {
                Debug.LogWarning($"[技能编辑器] {label} 只支持工程里的粒子预制体，不能是场景中的物体：{selected.name}", selected);
                return current;
            }

            if (selected.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                Debug.LogWarning($"[技能编辑器] {label} 自身或子物体上没找到 ParticleSystem，已忽略：{selected.name}", selected);
                return current;
            }

            particle.ParticlePrefab = selected;
            particle.CalculateDuration();
            EditorUtility.SetDirty(_asset);
            SceneView.RepaintAll();
            return selected;
        }

        private static string[] GetLayerNames()
        {
            string[] names = new string[32];
            for (int i = 0; i < 32; i++)
                names[i] = LayerMask.LayerToName(i);
            return names;
        }
    }
}
