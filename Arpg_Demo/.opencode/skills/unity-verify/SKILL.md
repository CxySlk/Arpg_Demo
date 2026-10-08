---
name: unity-verify
description: Unity 编辑器内的编译与运行验证闭环。改动 C# 后用 Unity CLI 的 MCP 工具触发重编译(`unity_recompile`)、读取 Console(`unity_console`)确认 0 错误，或用 `unity_run_script`/`unity_eval` 在编辑器里实际执行逻辑、预览数据、跑测试。当用户说"编译检查/验证一下/看看 Console/跑一下/触发重编译/确认没报错/在编辑器里试一下"时使用。
---

# Unity 验证

把「改代码 → 编译 → 看 Console → 实际执行验证」做成闭环。**Unity 的 Console 是唯一可靠的错误来源**，不要靠肉眼或静态推断判断成败。

## 何时用

- 改完任何 `.cs` 后，确认没有编译错误。
- 想在**非 Play** 的编辑器状态下实际执行一段逻辑（造对象、改设置、读数据、调 GAS/编辑器 API）。
- 跑 `Assets/Test` 下相关测试。
- 排查"运行结果不对"，需要拿到真实运行时数值。

## 工具

MCP 由 **Unity CLI** 提供（`unity.exe mcp`，stdio），工具前缀 `unity_`、名称为 snake_case。

| 目的 | MCP 工具 |
|---|---|
| 触发重编译（无需聚焦编辑器） | `unity_recompile` → `unity_recompile_status` 轮询 |
| 读 Console 日志（错误唯一来源） | `unity_console`（`level`/`tail`/`since`+`since_session`） |
| 编译失败标志 / Console 计数（不拉条目） | `unity_console_status` |
| 执行 C# 片段（Roslyn） | `unity_eval` |
| 执行 .cs 文件的静态入口 | `unity_run_script` |
| 截场景视图 | `unity_capture_scene_view` |
| 截 Game 视图 | `unity_capture_game_view` |
| 跑测试 | `unity_run_tests` → `unity_test_status` |
| 看场景树 / 找对象 | `unity_get_scene_hierarchy` / `unity_find_gameobjects` |
| 进出 Play 模式 | `unity_editor_play` / `unity_editor_stop` / `unity_editor_status` |

## 标准闭环

1. **改完代码 → 触发重编译**：`unity_recompile`，再用 `unity_recompile_status` 轮询到 `completed`。
2. **读 Console 确认 0 错误**：`unity_console`（`level: "error"` 直接筛错误；`tail` 限条数）；或先用 `unity_console_status` 看 `compile-failure` 标志。
3. **有错就贴给用户/自己修**，回到第 1 步，直到干净。
4. **需要行为验证**时，用 `unity_run_script`（多行、可带 `args`）或 `unity_eval`（零散片段）在编辑器内实际跑，关键数值 `Debug.Log` 出来后用 `unity_console` 读。
5. **需要测试**时，`unity_run_tests`（`mode: editor|playmode|all`，`filter` 支持 testName/assembly/category）。

> 编译通过 ≠ 逻辑正确；执行过、拿到数值才算验证完成。

## 在编辑器里执行 C#（编写规则）

**零散片段**用 `unity_eval`（传 `code` 字符串，`timeout` 毫秒）。**多行/需要参数**用 `unity_run_script`：

- `file`：`.cs` 文件路径。相对路径按**项目根**（`Assets/` 的父目录）解析。放在 `Assets/` **外面**（如 `AgentScripts/`）可避免触发资源导入与域重载。
- `entry`：静态入口，形如 `Namespace.Type.Method`（或 `Type.Method` / 裸方法名）；省略时取唯一 public static 方法，否则找 `Main`。
- `args`：JSON 数组，按入口参数类型强制转换（基元、枚举名、string[]、`UnityEngine.Object` 用对象句柄）。
- `dry_run: true` 只编译不执行；`pdb: true` 可让异常栈映射到 `file:line`。
- 运行结果不通过返回值读，**走 `Debug.Log` + `unity_console`**。

```csharp
using UnityEngine;
using UnityEditor;

public static class VerifyScript
{
    public static void Run()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Debug.Log($"[verify] created {cube.name} @ {cube.transform.position}");
    }
}
```

调用：`unity_run_script(file: "AgentScripts/VerifyScript.cs", entry: "VerifyScript.Run")`。

> 旧 relay 的 `internal class CommandScript : IRunCommand` / `ExecutionResult` 模板**已废弃**，新 CLI 里不存在，别照抄。

## 编辑器模式陷阱（非 Play 下）

- **不能用带延迟的销毁**：`Object.Destroy(go, delay)` 会报 "Destroy may not be called from edit mode"。用 `DestroyImmediate`，或 `EditorApplication.update` 轮询做延迟。
- **系统单例为 null**：`PoolSystem`/`AudioSystem`/`MonoSystem` 的 `instance` 在编辑器模式可能未初始化，它们各自有 `#if UNITY_EDITOR` 懒初始化路径（`MonoSystem.Start_Coroutine_InEditor`、`AudioSystem.PlayEffectMusicInEditor`、`PoolSystem.EnsureEditorInit`），走这些入口。
- **域重载会重置非序列化字段**：`previewModel`/`currentFrameIndex` 等在重编译后清零；`abilityAsset` 已 `[SerializeField]` 可保留。`unity_recompile`（以及会触发编译的操作）会引发域重载，别指望它保状态。
- **Odin 资产**：改字段后 `EditorUtility.SetDirty` 即可；**绝不在拖动字段的回调里 `AssetDatabase.SaveAssets()`**（每帧写盘会卡死）。
- **`[SerializeReference]` 多态列表**遍历前判 null（旧资产反序列化可能产生 null 元素）。

## 测试

- 框架：`com.unity.test-framework`，测试资源在 `Assets/Test`。
- 改核心逻辑（GAS 结算、TargetCatcher、Attribute）后尽量跑相关用例。
- 测试里同样注意编辑器模式限制；不要假设运行时单例存在。

## 偶发故障处理

- **连接不上 / 超时**：先用 CLI `unity status` 确认编辑器在线（端口/项目/版本/PID）；多开 Editor 时配置里用 `--project-path` 固定本项目。偶发超时重试即可，不是代码问题。
- **客户端被拒绝**：检查 Unity 的 MCP / Pipeline 相关设置与批准。
- **`unity_console_status` 显示编译失败**：用 `unity_console`（`level: "error"`）拿具体错误再改。

## 不要做

- 不直接改 `.unity` / `.prefab` / 二进制资源文本；场景与预制体优先用 MCP 工具或让用户手动操作，改完再验证。
- 不在没有实际执行过的情况下声称"已验证通过"。
