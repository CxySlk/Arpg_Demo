# Unity ARPG 项目（Arpg_Demo）

Unity 6000.3.12f1 ARPG 项目，GAS 技能系统 + LLKFrame 框架 + 自定义技能编辑器。通过 **Unity CLI 的 MCP 服务器**在 OpenCode 中操作 Unity。

## Unity MCP 与验证工作流

- MCP 由 **Unity CLI** 提供（`unity.exe mcp`，stdio），配置在项目根/工作区根 `opencode.json`，工具前缀为 `unity_`，工具名为 snake_case（如 `unity_recompile`、`unity_run_script`、`unity_console`）。
- **触发重编译**：`unity_recompile` → 用 `unity_recompile_status` 轮询到 `completed`。
- **看错误**：`unity_console`（`level: "error"` 筛错误，`tail` 限条数，`since` + `since_session` 做游标跟随）；`unity_console_status` 直接给编译失败标志与计数。**Unity 的 Console 是唯一可靠错误来源**。
- **在编辑器里执行 C# 验证**：
  - `unity_eval`：传 C# 代码字符串（Roslyn 编译执行），适合零散片段。
  - `unity_run_script`：把代码写到 `.cs` 文件（可放 `Assets/` 外的 `AgentScripts/`，避免触发导入/域重载），编译后执行**命名的静态入口**（`entry` 形如 `Namespace.Type.Method`，省略时取唯一 public static 方法或 `Main`），支持 `args`/`dry_run`/`timeout_ms`/`pdb`。
- **注意**：新 CLI **没有**旧 relay 的 `CommandScript : IRunCommand` / `ExecutionResult` 概念，别照抄旧模板；运行结果走 `Debug.Log` + `unity_console` 读取。
- 改完代码后：`unity_recompile` → `unity_console` 确认 0 错误。
- 连接问题用 `unity status`（CLI）确认编辑器在线（端口/项目/版本/PID）；多开 Editor 时配置里用 `--project-path` 固定本项目。若客户端被拒绝，检查 Unity 的 MCP / Pipeline 相关设置与批准。

## 目录结构与所有权

- `Assets/Scripts/GAS/` — GAS 技能系统（命名空间 `GAS`）。asmdef `GAS` 引用 `LLKFrame`。`Runtime/` 是核心逻辑，`Editor/`（asmdef `GAS.Editor`）和 `EditorData/` 是编辑器工具。**GAS Runtime 引用 `GASEditorData`（编辑器数据），因此 GAS 拆 Runtime/Editor 需注意此反向依赖。**
- `Assets/Scripts/` 其余子目录（Player/FSM/UI/Input/Config/Manager 等）— 玩家逻辑，编入 `Assembly-CSharp`。
- `Assets/LLKFrame/` — LLKFrame 框架（命名空间 `LLKFrame`）。asmdef `LLKFrame` 引用 `Unity.Addressables`/`Unity.ResourceManager`/`Unity.TextMeshPro`；`LLKFrame.Editor` 是其编辑器程序集。
- `Assets/SkillEditor/Editor/` — 技能编辑器。asmdef `SKillEditor`（注意大小写）引用 `GAS`/`LLKFrame`。**改这里的代码只需重编 SKillEditor 程序集，编译更快。**
- `Assets/Test/` — 测试资源（Ability/Cue/Prefab 等）。

## 技能编辑器架构（partial class）

`Assets/SkillEditor/Editor/SkillEditorWindow/SkillEditorWindow.cs` 拆分为多个 partial 文件，改功能前先定位对应文件：

- `SkillEditorWindow.cs` — 主窗口、生命周期（CreateGUI/OnDestroy）、配置类
- `SkillEditorWindow.Top.cs` — 顶部栏（场景切换、预览模型、技能配置选择）、`GetPreviewASC`
- `SkillEditorWindow.Timeline.cs` — 时间轴（帧索引、绘制、缩放）、`CurrentFrameIndex`/`FrameCount`
- `SkillEditorWindow.Content.cs` — 左右滚动区域布局
- `SkillEditorWindow.LeftPanel.cs` — 左侧事件列表（增删、拖拽排序、选中高亮）
- `SkillEditorWindow.RightPanel.cs` — 右侧轨道事件条（拖拽改帧、GE 事件中心对齐）
- `SkillEditorWindow.PropertyPanel.cs` — 事件详情面板（Cue 内嵌 Inspector、GE 捕捉器参数反射绘制、Effect 列表）
- `SkillEditorWindow.Playback.cs` — 播放/scrub/粒子预览/根运动计算
- `SkillEditorWindow.SceneGizmos.cs` — SceneView 中可视化 GE 捕捉范围（盒/球/扇）

## GAS 关键约定

- `GameplayCueDurational` 子类（Animation/Audio/Partical）用 `DurationFrame`（帧数）控制持续时长；`CalculateDurationFrame()` 按素材时长自动计算。
- 编辑器预览遵循"**编辑器运行层**"模式：`AnimationController.Editor.cs`（`#if UNITY_EDITOR`）提供 `EditorScrubAnimation`/`EditorPlayAnimation`/`EditorStopScrubPreview`，用独立 PlayableGraph，**不改运行时核心**。编辑器预览相关逻辑应写在 `#if UNITY_EDITOR` 扩展文件里，不要改运行时 `AnimationController.cs` 的播放逻辑。
- 对象池/音频/Mono 在编辑器模式（非 Play）下 `instance` 为 null：`PoolSystem`/`AudioSystem`/`MonoSystem` 有 `#if UNITY_EDITOR` 的编辑器懒初始化路径（如 `MonoSystem.Start_Coroutine_InEditor`、`AudioSystem.PlayEffectMusicInEditor`、`PoolSystem.EnsureEditorInit`）。
- **编辑器模式不能用带延迟的 `Object.Destroy(go, delay)`**，会报 "Destroy may not be called from edit mode"；用 `DestroyImmediate` 或 `EditorApplication.update` 轮询延迟销毁。
- GAS 里 `OnValueChanged`/`ValueDropdown` 用**字符串形式**（如 `"OnParticlePrefabChanged"`）而非 `nameof`，因为方法在 `#if UNITY_EDITOR` 里，`nameof` 在非编辑器平台编译报错。
- `TargetCatcher` 各子类字段名：盒型 `HalfExtents`/`Rotation`，球形 `Radius`，扇形 `InsideRadius`/`OutsideRadius`/`Rotation`/`Height`/`Angle`；扇形旋转需要 `NormalizeRotation` 归一化到 ±180。

## 陷阱与注意

- 项目使用 Odin（Sirenix）序列化：`AbilityAsset : ConfigBase : SerializedScriptableObject`，`MarkEvents` 是 Odin 序列化的 public 字段。改资产字段后 `EditorUtility.SetDirty` 即可；**不要在拖动字段的回调里调 `AssetDatabase.SaveAssets()`（每帧写盘会卡死）**。
- `AbilityAsset.MarkEvents` 用 `[SerializeReference]` 存多态事件，旧资产反序列化失败会产生 null 元素，遍历前要判空。
- `previewModel`/`abilityAsset`/`currentFrameIndex` 是非序列化字段，**域重载（重编译）后会重置为 null/0**；`abilityAsset` 已标 `[SerializeField]` 可保留，`previewModel` 需在编辑器场景重建。
- 场景可视化（`SceneGizmos`）通过 `SceneView.duringSceneGui` 注册/注销，在 `CreateGUI`/`OnDestroy` 挂钩；当前帧命中 `ReleaseGEMarkEvent.StartFrame` 且 `TargetCatcher != null` 时绘制。
- NuGet 包由 NuGetForUnity 管理（`Assets/Plugins/Nuget/packages.config` + `Assets/Packages/`），不要手动改 DLL。
