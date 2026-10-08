# Arpg_Demo 项目规则(AGENTS.md)

本文件为 opencode 在此项目中的工作规则,每次会话自动注入。**中文交流**;代码与专有名词保留英文。

## 项目概况

- 引擎:Unity **6000.3.12f1**(Unity 6),渲染管线 **URP 17.3**
- 类型:3D ARPG Demo,场景流:`BeginScene → ChooseCharacterScene → GameScene`
- 输入:InputSystem(新版);资源管理:**Addressables**;序列化:MessagePack
- 关键包:com.unity.addressables、com.unity.cinemachine、com.unity.inputsystem、com.unity.test-framework、com.unity.timeline
- MCP:由外部 **Unity CLI**(`unity mcp`,stdio)提供,不再依赖 `com.unity.ai.assistant` 包(已移除)
- 插件/第三方:Sirenix Odin Inspector(位于 `LLKFrame/Plugin/Sirenix`)、NodeCanvas(ParadoxNotion)、MessagePack-CSharp(git 包)

## 目录结构与约定

### 代码根目录 `Assets/Scripts`

| 目录 | 职责 |
|---|---|
| `LLKFrame`(实际位于 `Assets/LLKFrame`) | 自研框架,见下 |
| `GAS` | 能力系统:GAS/Runtime 下分 `Ability / Attribute / CheckRelease / Component / Core / Cue(Duration·Instant) / DataRuntime / Effect(Data) / Tag / Timeline`;另有 `GAS/Editor`、`GAS/EditorData` |
| `SkillEditor` | 技能编辑器(Editor 工具) |
| `Player` | 玩家逻辑(与 `FSM`、`Input`、`Manager`、`Data`、`Config` 配合) |
| `FSM/Player` | 玩家状态机:`State/{AbilityState | GroundState | InAirState}`、`Condition` |
| `Config` | 配置数据:`Config/{Player | Skill}` |
| `Data` | 数据类:`Data/{Config | Player}` |
| `Input` | 输入处理 |
| `Manager` | 全局管理器 |
| `UI` | UI 层:`UI/{BeginScene | ChooseCharacterScene | ...}` |
| `BeginScene / ChooseCharacterScene / GameScene` | 各场景业务脚本 |

### 自研框架 `Assets/LLKFrame`

- `Scripts/0.Base` — 基础类/接口
- `Scripts/1.System` — 系统层(按编号):`0.Pool` 对象池、`1.Event` 事件、`2.Res` 资源、`3.UI`、`4.Save`、`5.Audio`、`6.Localization`、`7.Log`、`8.Scene`、`9.Mono`
- `Scripts/2.Component` — 组件层:`0.Event`、`1.Animation`
- `Scripts/3.Extension` — 扩展方法
- `Scripts/4.Setting` — 设置
- `Plugin/Sirenix` — Odin 插件(勿改)
- `Setting/AddressableAssetsData` — Addressables 配置

### 资源目录 `Assets/`

- `ArtRes` 美术资源、`Res` 资源、`Prefabs` 预制体、`Config` 配置文件、`Scenes` 场景、`Settings` 设置、`Test` 测试、`Editor` 编辑器工具

## 程序集与依赖(重要)

项目代码已通过 **asmdef** 物理隔离:`LLKFrame(.Editor)`、`GAS(.Editor)`、`SKillEditor` 各自独立程序集。

- **依赖方向必须单向**:低层(框架/系统)不反向依赖高层(玩法/UI)。
- **禁止循环依赖**:跨程序集反向取数据/调用,一律走接口或事件总线(用 LLKFrame 自带的 `1.System/1.Event` 事件系统桥接)。
- 新增代码遵循同样的分层归属,不要把系统逻辑写进 UI 或反之。

## 编码规范

- **性能**:禁止在 `Update` 里每帧 `new` 堆内存对象;优先值类型/结构体(栈上)、对象池复用、初始化时一次获取或懒加载。帧高频路径尤其注意(战斗、移动、GAS 效果结算)。
- **魔法数字/魔法字符串**:常量集中定义,不散落硬编码。
- **禁止 `var`**:一律用显式类型声明变量,不用 `var`(如 `List<int> list = new List<int>()`,而非 `var list = ...`)。
- **`new` 写法**:类型已由声明给出时,用目标类型推断的 `new()`,不重复写类型名(如 `public BBParameter<EnemyController> enemyController = new();`,而非 `= new BBParameter<EnemyController>();`);集合/数组初始化器同理(`public List<int> list = new() { 1, 2 };`)。泛型参数长、命名空间深的类型收益最明显。只有推断不出目标类型时才写完整类型名(例如接口/基类字段里 `new` 一个具体实现、作为实参或返回值时 `new` 对象、以及 `new` 之后还要接 `.成员` 链式调用如 `new Vector3(x, 0, y).normalized`)。Unity 6 支持该写法,与上面"禁止 `var`"不冲突——声明处依然写明了类型。
- **初始化写法**:反对 `EnsureXxx/Refresh/Rebuild` 式"每个函数都先检查再初始化"的重复防御;初始化时可靠获取或懒加载即可,不做冗余的空/状态检查。
- **空引用检查**:不要到处冗余判空。凡是由使用者在 Inspector/黑板里填好的引用(控制器、资源、信号等),缺失就让它在配置阶段暴露(必要处直接报错),不要每个函数都 `if (x == null) return;`。只在"运行期确实可能为空且必须容错"的地方判空(例如黑板目标可能为空、按名字查动画可能查不到)。判空写在该写的地方,不做无差别防御。
- **解耦**:谁需要数据,接口/事件就留在谁那边;发布者=拥有方,订阅者=多个实现。慎用全局单例/服务定位器(丢失调用链)。
- **命名与风格**:
  - 字段:私有字段以 `_` 开头且后续小写(如 `_frameCount`);`public` 字段以**小写字母**开头(如 `frameCount`)。
  - 属性:以**大写字母**开头(如 `FrameCount`)。
  - 接口:以 `I` 开头 + PascalCase,命名表达**契约角色**——
    - 角色名词:`ITarget`(被作用方)/`IOwner`(持有者)/`IProvider`(提供者)/`ISource`(来源)/`IListener`(订阅方)。
    - 能力形容词:`-able/-ible` 式(如 `IDisposable`/`IComparable`)。
    - 领域实体:`ICharacter`/`IEffect`/`ICue`。
    - 避免:`Checker/Handler`(仅当有行为方法时使用)、`Helper/Util`(那是静态工具类,不应作接口名)、`Impl/Interface`(冗余)。
  - 其余遵循项目既有风格(参考 LLKFrame、GAS 现有代码),不要另起一套。

## 验证方式

- **编译**:改完代码确保无编译错误(可让 opencode 检查相关 asmdef 依赖关系)。
- **测试**:项目使用 Unity Test Framework(`com.unity.test-framework`),测试位于 `Assets/Test`。改动核心逻辑后尽量跑相关测试。
- **编辑器验证**:必要时用 Unity CLI 的 MCP 工具(`unity_*`)协助在编辑器内验证(GameScene 运行检查)。

## 协作规则

1. 修改任何文件前,先 `Read` 该文件及相关代码,理解后再动手。
2. 不主动执行 `git commit / push`,除非用户明确要求。
3. 不修改 `Library/`、`Temp/`、`Logs/`、`obj/`、`*.csproj`、`*.sln` 等自动生成内容。
4. 输出用中文;不确定处先问,不做危险批量操作。
5. 涉及场景/预制体/资产等 `.unity`/`.prefab`/二进制资源时,优先用 MCP 工具或让用户手动操作,不直接改文本。
