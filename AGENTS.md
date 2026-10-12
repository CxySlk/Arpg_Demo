# Arpg_Demo 仓库规则(环境/工具)

本文件面向整个仓库,讲"怎么在这个环境里干活"。**中文交流**;代码与专有名词保留英文。Unity 工程域的架构/编码细节在 `Arpg_Demo/AGENTS.md`,编辑器内验证闭环在 `.opencode/skills/unity-verify`。

## 仓库形态

- git 根 = **工作区根** `D:\Others\Projects\Unity\Arpg_Demo`;Unity 工程在嵌套的 `Arpg_Demo/` 内(含 `.gitignore`、`ProjectSettings/`、`Packages/`)。两个 `AGENTS.md`(本文件 + `Arpg_Demo/AGENTS.md`)都会被自动注入,请勿在其中重复同一内容。
- 杂项/生成物,勿手改:`Arpg_Demo/Assets/_Recovery/*.unity`(临时场景)、根目录 `*.csproj`/`*.sln`/`*.slnx`(Unity 生成且已 gitignore)、`Library/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`。
- 无 CI、无 `.github`、无构建/生成 shell 脚本(仅 Luban 的 `Arpg_Demo/LubanConfig/MiniTemplate/gen.bat|sh`)。改动验证全靠 Unity CLI MCP。

## Unity MCP 与验证工作流

- MCP 由 **Unity CLI**(`unity.exe mcp`,stdio)提供,配置在 `opencode.json`,工具前缀 `unity_`、名称为 snake_case。
- **触发重编译**:`unity_recompile` → `unity_recompile_status` 轮询到 `completed`。
- **看错误**:`unity_console`(`level:"error"`、`tail`、`since`+`since_session` 游标);`unity_console_status` 给编译失败标志与计数。**Unity Console 是唯一可靠错误来源**。
- **编辑器内执行 C#**:
  - `unity_eval`:传代码字符串(Roslyn),适合零散片段。
  - `unity_run_script`:把代码写到 `.cs`(放 `Assets/` 外的 `AgentScripts/`,避免触发导入/域重载),编译后执行命名静态入口(`Namespace.Type.Method`,省略时取唯一 public static 方法或 `Main`),支持 `args`/`dry_run`/`timeout_ms`/`pdb`;结果走 `Debug.Log` + `unity_console` 读取。
- 其他常用:`unity_run_tests`/`unity_test_status`(跑测试)、`unity_build`/`unity_build_status`(出 Player)、`unity_capture_scene_view`/`unity_capture_game_view`(截图)、`unity_editor_play`/`unity_editor_stop`。
- **没有单元测试程序集、也没有 CI**:全 `Assets` 无 `using NUnit`/`[Test]`;`Assets/Test/` 是手改验证资源(Animancer demo、临时 `.asset`),`unity_run_tests` 现在跑不出用例。实际门槛 = 重编译 + `unity_console` 0 错误 + `unity_run_script` 实跑拿到数值。
- **注意**:新 CLI **没有**旧 relay 的 `CommandScript : IRunCommand`/`ExecutionResult` 概念,别照抄旧模板。
- 通用编辑器模式陷阱(带延迟 `Destroy`、编辑器下单例为 null、Odin 回调勿 `SaveAssets`、`[SerializeReference]` 判空等)**详见 `.opencode/skills/unity-verify`**,此处不重复。
- 连接问题用 CLI `unity status` 确认编辑器在线(端口/项目/版本/PID);多开 Editor 时用 `--project-path` 固定本项目;客户端被拒绝时检查 Unity 的 MCP/Pipeline 设置与批准。

## 程序集与依赖(重要)

项目用 **asmdef** 物理隔离,依赖方向必须**单向**(低层不反向依赖高层),禁止循环依赖;跨程序集反向取数据一律走接口或 LLKFrame 事件总线。

| 程序集 | 覆盖 | 关键引用 |
|---|---|---|
| `LLKFrame` / `LLKFrame.Editor` | `Assets/LLKFrame/` | `Unity.Addressables`、`Unity.ResourceManager`、`Unity.TextMeshPro`、`Kybernetik.Animancer` |
| `GAS` / `GAS.Editor` / `GAS.EditorData` | `Assets/Scripts/GAS/` | `LLKFrame`、**`GAS.EditorData`**、`Kybernetik.Animancer` |
| `SKillEditor`(注意大小写) | `Assets/SkillEditor/Editor/` | `GAS`、`GAS.EditorData`、`LLKFrame`、`Animancer` |
| `Assembly-CSharp` | `Assets/Scripts/` 其余目录(**无 asmdef**) | 自动引用上述 |

- **GAS Runtime 引用 `GAS.EditorData`(编辑器数据)**,是已知的"反向依赖",拆 Runtime/Editor 时注意。
- 改动影响面:改 `Assets/Scripts/`(非 GAS)会触发整包 `Assembly-CSharp` 重编;只改 `Assets/SkillEditor/` 只重编 `SKillEditor`,迭代更快。

## 目录所有权(细节见 `Arpg_Demo/AGENTS.md`)

- `Assets/Scripts/GAS/` — GAS 技能系统(命名空间 `GAS`)。
- `Assets/Scripts/` 其余 — 玩家/敌人逻辑、技能运行时(`Skill`)、输入、管理器、UI、Luban 生成表(`Code/`)、各场景脚本。
- `Assets/LLKFrame/` — LLKFrame 框架(命名空间 `LLKFrame`)。
- `Assets/SkillEditor/` — 技能编辑器(Editor 工具)。
- 工程域细节(目录表、运行时链路、技能编辑器 View 结构、GAS 约定、编码规范、陷阱)统一见 `Arpg_Demo/AGENTS.md`,本文件不再重复。

## 协作规则

1. 修改任何文件前,先 `Read` 该文件及相关代码,理解后再动手。
2. 不主动执行 `git commit / push`,除非用户明确要求。
3. 不修改 `Library/`、`Temp/`、`Logs/`、`obj/`、`*.csproj`、`*.sln` 等自动生成内容。
4. 输出用中文;不确定处先问,不做危险批量操作。
5. 涉及 `.unity`/`.prefab`/二进制资产时,优先用 MCP 工具或让用户手动操作,不直接改文本。
