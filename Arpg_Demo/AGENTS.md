# Arpg_Demo 工程与编码规范(AGENTS.md)

Unity 工程域知识。**中文交流**;代码与专有名词保留英文。环境/工具/验证见仓库根 `../AGENTS.md`;编辑器内验证闭环见 `.opencode/skills/unity-verify`。

## 项目概况

- 引擎:Unity **6000.3.12f1**(Unity 6),渲染管线 **URP 17.3**。
- 类型:3D ARPG Demo,场景流 `BeginScene → ChooseCharacterScene → GameScene`。
- 输入:InputSystem(新版);资源管理:**Addressables**;序列化:MessagePack。
- 关键包:com.unity.addressables、com.unity.cinemachine、com.unity.inputsystem、com.unity.timeline;MCP 通道 `com.unity.pipeline`(`com.unity.ai.assistant` 已移除)。
- 插件/第三方:Sirenix Odin Inspector(`LLKFrame/Plugin/Sirenix`)、NodeCanvas(ParadoxNotion)、Animancer(动画)、MessagePack-CSharp(git 包)。

## 目录结构与约定

### 代码根目录 `Assets/Scripts`(编入 `Assembly-CSharp`;仅 `GAS/` 子目录有独立 asmdef)

| 目录 | 职责 |
|---|---|
| `GAS/` | 技能系统(asmdef `GAS`)。`Runtime/` 分 `Ability / Attribute / Core / Cue / Effect / Tag / Timeline`;`Editor/`(`GAS.Editor`)、`EditorData/`(`GAS.EditorData`) |
| `Character/` | 角色基类与子类:`CharacterControllerBase`、`CharacterConfigBase`、`WeaponController`、`Weapon`;`Player/{Config, Editor, FSM/State/{AbilityState,GroundState,InAirState}, View(CharacterView)}`;`Enemy/{Config, Editor, FSM}` |
| `Skill/` | 技能运行时:`AbilityPlayer`、`SkillBrainBase`、`SkillBehaviorBase`、`Player_SkillBrain`/`Enemy_SkillBrain`、`Behavior/`、`Config(SkillConfig)`、`Data(SkillLearnData)` |
| `Input/` | InputSystem 输入处理(`InputManager`、`InputNodeBase` 等) |
| `Manager/` | 全局管理器(`ManagerController`) |
| `Data/` | 数据:`DataManager`、`Player/PlayerData`、`VolumeData` |
| `NodeCanvas/` | 行为树/状态机节点:`Action`、`Audio`、`Condition`、`Decorate` |
| `UI/` | UI 层:`BeginScene`/`ChooseCharacterScene`/`GameScene` 三场景窗口 + 通用窗口 |
| `Code/` | Luban 生成的配置表(`Tables`/`Tb*`,勿手改) |
| `BeginScene / ChooseCharacterScene / GameScene` | 各场景业务脚本;`Camera/` 相机表现 |
| `SkillEditor`(实际位于 `Assets/SkillEditor`) | 技能编辑器(Editor 工具) |

### 自研框架 `Assets/LLKFrame`

- `Scripts/0.Base` — 基础类/接口(`SingletonBase`/`SingletonMonoBase`/`ManagerBase`)
- `Scripts/1.System` — 系统层(按编号):`0.Pool` 对象池、`1.Event` 事件、`2.Res` 资源、`3.UI`、`4.Save`、`5.Audio`、`6.Localization`、`7.Log`、`8.Scene`、`9.Mono`、`10.Timeline`(**`TimeSystem`,全局帧驱动/时间系统,技能帧驱动的核心**)
- `Scripts/2.Component` — 组件层:仅 `0.Event`(事件监听组件)
- `Scripts/3.Extension` — 扩展方法
- `Scripts/4.Setting` — 设置与配置基类:`ConfigBase`(**`AbilityAsset` 的基类**)、`LLKFrameSetting`
- `Config / Editor / Plugin / Prefab` — 框架配置、编辑器程序集(`LLKFrame.Editor`)、插件(`Sirenix` — Odin,勿改)、预制体
- `Setting/LLKFrameSetting.asset` — 框架设置资产
- Addressables 配置在 `Assets/AddressableAssetsData`(非 `LLKFrame/Setting`)。

### 资源目录 `Assets/`

`ArtRes` 美术资源、`Res` 资源、`Resources`、`Prefabs` 预制体、`Config` 配置文件、`Scenes` 场景、`Settings` 设置、`Test` 手改验证资源(**非单元测试**)、`SkillEditor` 技能编辑器、`AddressableAssetsData` Addressables 配置、`Plugins` 第三方插件。

### 程序集依赖

见仓库根 `../AGENTS.md` 的依赖表(含 GAS runtime→`GAS.EditorData` 反向依赖、改哪层重编哪个程序集)。

## 运行时主链路(技能)

```
输入 InputManager → PlayerController.OnInputCheckUpdate / TryReleaseSkillByID
→ SkillBrainBase.PlaySkill(门禁/CD/playingSkills)
→ SkillBehaviorBase(多段连段 abilityIndex + 续段间隙、帧事件钩子、CD/消耗)
→ AbilityPlayer(订阅 TimeSystem.Global,按相对帧驱动 AbilityAsset 的 effectDic/cueDic/eventDic)
→ ReleaseGEMarkEvent + TargetCatcher 捕捉目标 → ASC.ApplyEffect
   / Cue(Animation/Audio/Particle) 经 CharacterView(Animancer) 表现
→ AbilitySystemComponent 每帧 Update 驱动持续效果 OnUpdate
```

- `TimeSystem.Global` 是全局帧驱动心脏,技能全挂它上面走;统一暂停/时间缩放(改 `Time.timeScale`)。
- Cue / Effect 运行时统一按**秒**计时(配置用 `DurationSeconds`/`durationSeconds`);"帧"只出现在编辑器(帧号与 `DurationFrames` 折算)。

## 技能编辑器架构(View 结构)

`Assets/SkillEditor/Editor/SkillEditorWindow/` 下按职责拆分为多个 View 类(非 partial 拆分),改功能前先定位对应文件:

- `SkillEditorWindow.cs` — 主入口:生命周期(CreateGUI/OnDestroy)、`abilityAsset`/`previewModelRoot`/`previewModel`(均 `[SerializeField]`)、`ApplyConfig`/`SaveConfig`,创建并持有各 View
- `SkillEditorUI.cs` — UXML/USS 元素绑定(`curFrameIndexField`/`skillClipConfigField` 等)
- `SkillEditorConfig.cs` — 编辑器静态配置与帧/秒换算(`DurationFrames(cue)` 由 `DurationSeconds` 折算帧)
- `ContentView.cs` — 左右滚动区域布局
- `EventListView.cs` — 左侧事件列表(增删、拖拽排序、选中高亮,`MarkEventsChanged`/`SelectionChanged` 广播)
- `TimelineView.cs` — 时间轴(`_currentFrameIndex`/`FrameCount`、绘制、缩放)
- `TrackView.cs` — 右侧轨道事件条(拖拽改帧、GE 事件中心对齐、时长条宽度按帧)
- `PropertyPanelView.cs` — 事件详情面板(Cue 内嵌 Inspector、GE 捕捉器参数反射绘制、Effect 列表)
- `SceneGizmoView.cs` — SceneView 中可视化 GE 捕捉范围(盒/球/扇),含 `NormalizeRotation`
- `PlaybackController.cs` — 播放/scrub/粒子预览/根运动计算
- `PreviewSceneManager.cs` — 预览场景与模型管理(`GetPreviewASC`)
- `MarkEventListLayout.cs` / `LayoutToggle.cs` — 事件列表布局

预览资产:`Player_Editor.prefab` / `Enemy_Editor.prefab` + `SkillEditorScene.unity`。

## GAS 关键约定

- `GameplayCueDurational` 子类(Animation/Audio/Particle)用 `DurationSeconds`(秒)控制持续时长,`CalculateDuration()` 按素材时长自动算出;持续类 Effect 亦用 `durationSeconds`/`periodicSeconds`。**帧只存在于编辑器**:`SkillEditorConfig.DurationFrames(cue)` 由秒折算帧用于时间轴显示。
- 动画播放统一经 `CharacterView`(Animancer 封装:`Play(...)`、`SetRootMotion(Action<Vector3,Quaternion>)` → `OnAnimatorMove` 转发 `deltaPosition`/`deltaRotation`),运行时与编辑器共用;技能编辑器的预览/scrub/粒子由 `PreviewSceneManager` + `PlaybackController` 驱动(无 `AnimationController`/`EditorScrubAnimation` 之类独立运行层)。
- 对象池/音频/Mono 在编辑器模式(非 Play)下 `instance` 为 null:`PoolSystem`/`AudioSystem`/`MonoSystem` 有 `#if UNITY_EDITOR` 的编辑器懒初始化路径(如 `MonoSystem.Start_Coroutine_InEditor`、`AudioSystem.PlayEffectMusicInEditor`、`PoolSystem.EnsureEditorInit`)。
- **编辑器模式不能用带延迟的 `Object.Destroy(go, delay)`**,会报 "Destroy may not be called from edit mode";用 `DestroyImmediate` 或 `EditorApplication.update` 轮询延迟销毁。
- GAS 里 `OnValueChanged`/`ValueDropdown` 用**字符串形式**(如 `"OnParticlePrefabChanged"`)而非 `nameof`,因为方法在 `#if UNITY_EDITOR` 里,`nameof` 在非编辑器平台编译报错。
- `TargetCatcher` 各子类字段名:盒型 `HalfExtents`/`Rotation`,球形 `Radius`,扇形 `InsideRadius`/`OutsideRadius`/`Rotation`/`Height`/`Angle`;扇形旋转需要 `NormalizeRotation` 归一化到 ±180。

## 编码规范

- **性能**:禁止在 `Update` 里每帧 `new` 堆内存对象;优先值类型/结构体(栈上)、对象池复用、初始化时一次获取或懒加载。帧高频路径尤其注意(战斗、移动、GAS 效果结算)。
- **魔法数字/魔法字符串**:常量集中定义,不散落硬编码。
- **禁止 `var`**:一律用显式类型声明变量,不用 `var`(如 `List<int> list = new List<int>()`,而非 `var list = ...`)。
- **`new` 写法**:类型已由声明给出时,用目标类型推断的 `new()`,不重复写类型名(如 `public BBParameter<EnemyController> enemyController = new();`,而非 `= new BBParameter<EnemyController>();`);集合/数组初始化器同理(`public List<int> list = new() { 1, 2 };`)。泛型参数长、命名空间深的类型收益最明显。只有推断不出目标类型时才写完整类型名(例如接口/基类字段里 `new` 一个具体实现、作为实参或返回值时 `new` 对象、以及 `new` 之后还要接 `.成员` 链式调用如 `new Vector3(x, 0, y).normalized`)。声明处依然写明了类型,与"禁止 `var`"不冲突。
- **初始化写法**:反对 `EnsureXxx/Refresh/Rebuild` 式"每个函数都先检查再初始化"的重复防御;初始化时可靠获取或懒加载即可,不做冗余的空/状态检查。
- **空引用检查**:不要到处冗余判空。凡是由使用者在 Inspector/黑板里填好的引用(控制器、资源、信号等),缺失就让它在配置阶段暴露(必要处直接报错),不要每个函数都 `if (x == null) return;`。只在"运行期确实可能为空且必须容错"的地方判空(例如黑板目标可能为空、按名字查动画可能查不到)。
- **解耦**:谁需要数据,接口/事件就留在谁那边;发布者=拥有方,订阅者=多个实现。慎用全局单例/服务定位器(丢失调用链)。
- **命名与风格**:
  - 字段:私有字段以 `_` 开头且后续小写(如 `_frameCount`);`public` 字段以**小写字母**开头(如 `frameCount`)。
  - 属性:以**大写字母**开头(如 `FrameCount`)。
  - 接口:以 `I` 开头 + PascalCase,命名表达**契约角色**——角色名词(`ITarget`/`IOwner`/`IProvider`/`ISource`/`IListener`)、能力形容词(`-able/-ible` 式如 `IDisposable`)、领域实体(`ICharacter`/`IEffect`/`ICue`);避免 `Checker/Handler`(仅有行为方法时使用)、`Helper/Util`(那是静态工具类,不应作接口名)、`Impl/Interface`(冗余)。
  - 其余遵循项目既有风格(参考 LLKFrame、GAS 现有代码),不要另起一套。

## 陷阱与注意

- 项目使用 Odin(Sirenix)序列化:`AbilityAsset : ConfigBase : SerializedScriptableObject`(`ConfigBase` 在 `LLKFrame/Scripts/4.Setting`)。改资产字段后 `EditorUtility.SetDirty` 即可;**不要在拖动字段的回调里调 `AssetDatabase.SaveAssets()`(每帧写盘会卡死)**。
- `AbilityAsset` 用三部字典 `effectDic`/`cueDic`/`eventDic`(键为帧号)承载时间轴事件,编辑器 `GetAllMarkEvents()` 按帧升序合并三部字典;事件对象 `MarkEventBase`(`ReleaseGEMarkEvent`/`DurationCueMarkEvent`/`FrameEvent`)用 `StartFrame` 记录起始帧。旧资产反序列化可能出现 null 元素,遍历前要判空。
- `abilityAsset`/`previewModel`/`previewModelRoot` 已标 `[SerializeField]`(域重载后可保留);当前帧 `_currentFrameIndex` 现位于 `TimelineView`。编辑器场景重建/域重载后引用仍可能为空,需要时在 `SkillEditorScene` 里重新指定。
- 场景可视化(`SceneGizmos`)通过 `SceneView.duringSceneGui` 注册/注销,在 `CreateGUI`/`OnDestroy` 挂钩;当前帧命中 `ReleaseGEMarkEvent.StartFrame` 且 `TargetCatcher != null` 时绘制。
- NuGet 包由 NuGetForUnity 管理(`Assets/Plugins/Nuget/packages.config` + `Assets/Packages/`),不要手动改 DLL。
