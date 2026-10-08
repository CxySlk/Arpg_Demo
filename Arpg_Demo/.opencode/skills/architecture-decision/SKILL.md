---
name: architecture-decision
description: "创建架构决策记录（ADR），记录重要的技术决策、上下文、考虑的替代方案和后果。每个主要技术选择都应该有一个 ADR。"
argument-hint: "[title] [--review full|lean|solo]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Task, AskUserQuestion
---

调用此技能时：

## 0. 解析参数 — 检测改造模式

解析审查模式（一次，存储用于此运行的所有门生成）：
1. 如果传入了 `--review [full|lean|solo]` → 使用该模式
2. 否则读取 `production/review-mode.txt` → 使用该值
3. 否则 → 默认为 `lean`

请参阅 `.opencode/docs/director-gates.md` 了解完整检查模式。

**如果参数以 `retrofit` 开头，后跟文件路径**
（例如，`/architecture-decision retrofit docs/architecture/adr-0001-event-system.md`）：

进入**改造模式**：

1. 完整读取现有 ADR 文件。
2. 通过扫描标题识别模板部分是否存在：
   - `## Status` — **缺失时阻塞**：`/story-readiness` 无法检查 ADR 验收
   - `## ADR Dependencies` — 缺失时高：依赖排序中断
   - `## Engine Compatibility` — 缺失时高：截止后风险未知
   - `## GDD Requirements Addressed` — 缺失时中：可追溯性丢失
3. 呈现给用户：
   ```
   ## 改造：[ADR 标题]
   文件：[路径]

   已存在的部分（不会被修改）：
   ✓ Status：[当前值，或"缺失 — 将添加"]
   ✓ [部分]

   缺失的部分需要添加：
   ✗ Status — 阻塞（没有这个，用户故事无法验证 ADR 验收）
   ✗ ADR Dependencies — 高
   ✗ Engine Compatibility — 高
   ```
4. 询问："我应该添加这 [N] 个缺失的部分吗？我不会修改任何现有内容。"
5. 如果是：
   - 对于 **Status**：询问用户——"此决策的当前状态是什么？"
     选项："Proposed"、"Accepted"、"Deprecated"、"Superseded by ADR-XXXX"
   - 对于 **ADR Dependencies**：询问——"此决策是否依赖于任何其他 ADR？
     它是否启用或阻止任何其他 ADR 或史诗？" 每个字段接受"None"。
   - 对于 **Engine Compatibility**：读取引擎参考文档（与下面的步骤 1 相同）
     并请用户确认领域。然后使用经过验证的数据生成表格。
   - 对于 **GDD Requirements Addressed**：询问——"哪些 GDD 系统促成了此决策？
     此 ADR 解决了每个 GDD 中的什么具体需求？"
   - 使用 Edit 工具将每个缺失的部分追加到 ADR 文件。
   - **永远不要修改任何现有部分。** 只追加或填写缺失的部分。
6. 添加所有缺失部分后，如果 `## Date` 字段缺失，则更新它。
7. 建议："现在此 ADR 有了 Status 和 Dependencies 字段，运行 `/architecture-review` 重新验证覆盖范围。"

如果不在改造模式中，继续到下面的步骤 1（正常 ADR 编写）。

**无参数保护**：如果未提供参数（标题为空），在运行阶段 0 之前询问：

> "您要记录什么技术决策？请提供一个简短的标题
> （例如，`event-system-architecture`、`physics-engine-choice`）。"

使用用户的回答作为标题，然后继续到步骤 1。

---

## 1. 加载引擎上下文（始终首先）

在执行任何其他操作之前，确定引擎环境：

1. 读取 `docs/engine-reference/[engine]/VERSION.md` 获取：
   - 引擎名称和版本
   - LLM 知识截止日期
   - 截止后版本风险级别（低 / 中 / 高）

2. 从标题或用户描述中识别此架构决策的**领域**。常见领域：Physics、Rendering、UI、Audio、Navigation、Animation、Networking、Core、Input、Scripting。

3. 如果存在，读取相应的模块参考：
   `docs/engine-reference/[engine]/modules/[domain].md`

4. 读取 `docs/engine-reference/[engine]/breaking-changes.md` — 标记相关领域中任何在 LLM 训练截止日期之后的更改。

5. 读取 `docs/engine-reference/[engine]/deprecated-apis.md` — 标记相关领域中不应使用的任何 API。

6. 如果领域具有中或高风险，在继续之前**显示知识差距警告**：

   ```
   ⚠️  引擎知识差距警告
   引擎：[名称 + 版本]
   领域：[领域]
   风险级别：高 — 此版本在 LLM 截止之后。

   从引擎参考文档验证的关键更改：
   - [与此领域相关的更改 1]
   - [更改 2]

   此 ADR 将与引擎参考库交叉引用。
   仅使用经过验证的信息继续 — 不要仅依赖训练数据。
   ```

   如果尚未配置引擎，提示："未配置引擎。
   先运行 `/setup-engine`，或告诉我您使用的是哪个引擎。"

---

## 2. 确定下一个 ADR 编号

扫描 `docs/architecture/` 中的现有 ADR 以找到下一个编号。

---

## 3. 收集上下文

读取相关代码、现有 ADR 和来自 `design/gdd/` 的相关 GDD。

### 3a：架构注册表检查（阻塞门）

读取 `docs/registry/architecture.yaml`。提取与此 ADR 的领域和决策相关的条目（通过系统名称、领域关键字或正在修改的状态进行 grep）。

在协作设计开始**之前**向用户呈现任何相关立场作为锁定约束：

```
## 现有架构立场（不得矛盾）

状态所有权：
  player_health → 由 health-system 拥有 (ADR-0001)
  接口：HealthComponent.current_health（只读浮点数）
  → 如果此 ADR 读取或写入玩家健康，它必须使用此接口。

接口契约：
  damage_delivery → 信号模式 (ADR-0003)
  信号：damage_dealt(amount, target, is_crit)
  → 如果此 ADR 传递或接收伤害事件，它必须使用此信号。

禁止模式：
  ✗ autoload_singleton_coupling (ADR-0001)
  ✗ direct_cross_system_state_write (ADR-0000)
  → 提议的方法不得使用这些模式。
```

如果用户提议的决策与任何已注册的立场矛盾，立即呈现冲突：

> "⚠️ 冲突：此 ADR 提议 [X]，但 ADR-[NNNN] 确立了 [Y] 是此目的的接受模式。如果不解决此冲突，将产生矛盾的 ADR 和不一致的用户故事。
> 选项：(1) 与现有立场一致，(2) 用显式替换取代 ADR-[NNNN]，(3) 解释为什么此情况是例外。"

在任何冲突解决或明确接受为有意例外之前，不要继续到步骤 4（协作设计）。

---

## 4. 协作指导决策

在询问任何内容之前，从已收集的上下文（已读取的 GDD、已加载的引擎参考、已扫描的现有 ADR）中推导技能的最佳猜测。然后使用 `AskUserQuestion` 呈现**确认/调整**提示 — 而不是开放式问题。

**首先推导假设：**
- **问题**：从标题 + GDD 上下文推断需要做出什么决策
- **替代方案**：从引擎参考 + GDD 需求提出 2-3 个具体选项
- **依赖关系**：扫描现有 ADR 以找到上游依赖；如果不清楚则假设无
- **GDD 联系**：提取标题直接相关的 GDD 系统
- **Status**：对于新 ADR 始终为 `Proposed` — 永远不要询问用户状态是什么

**假设范围选项卡**：假设仅涵盖：问题框架、替代方法、上游依赖、GDD 联系和状态。架构设计问题（例如，"生成时间应如何工作？"、"数据应该是内联还是外部？"）不是假设 — 它们是属于假设确认后单独步骤的设计决策。不要在假设 AskUserQuestion 小组件中包含架构设计问题。

**假设确认后**，如果 ADR 涉及架构或数据设计选择，请使用单独的多选项卡 `AskUserQuestion` 在起草之前独立询问每个设计问题。

**使用 `AskUserQuestion` 呈现假设：**

```
这是我起草前的假设：

问题：[从上下文推导的一句话问题陈述]
我将考虑的替代方案：
  A) [从引擎参考推导的选项]
  B) [从 GDD 需求推导的选项]
  C) [来自常见模式的选项]
驱动此决策的 GDD 系统：[从上下文推导的列表]
依赖关系：[上游 ADR（如果有），否则为"无"]
Status：Proposed

[A] 继续 — 使用这些假设起草
[B] 更改替代方案列表
[C] 调整 GDD 联系
[D] 添加性能预算约束
[E] 其他内容需要首先更改
```

在用户确认假设或提供更正之前，不要生成 ADR。

**引擎专家和 TD 审查返回后**（步骤 5.5/5.6），如果仍有未解决的决策，将每个决策作为单独的 `AskUserQuestion` 呈现，提议的选项作为选择加上自由文本转义：

```
决策：[具体未解决的点]
[A] [来自专家审查的选项]
[B] [替代选项]
[C] 不同的方法 — 我将描述它
```

**ADR 依赖关系** — 从现有 ADR 推导，然后确认：
- 此决策是否依赖于任何尚未接受的其他 ADR？
- 它是否解锁或解除对任何其他 ADR 或史诗的阻塞？
- 它是否阻止任何特定史诗开始？

将答案记录在 **ADR Dependencies** 部分。如果没有适用的约束，每个字段写"None"。

---

## 5. 生成 ADR

遵循此格式：

```markdown
# ADR-[NNNN]: [标题]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-XXXX]

## Date
[决策日期]

## Engine Compatibility

| 字段 | 值 |
|-------|-------|
| **引擎** | [例如 Unity 2022.3] |
| **领域** | [Physics / Rendering / UI / Audio / Navigation / Animation / Networking / Core / Input] |
| **知识风险** | [低 / 中 / 高 — 来自 VERSION.md] |
| **已查阅参考** | [列出读取的引擎参考文档，例如 `docs/engine-reference/unity/modules/physics.md`] |
| **使用的截止后 API** | [此决策依赖的任何来自 LLM 截止后版本的 API，或"无"] |
| **需要验证** | [发布前需要测试的具体行为，或"无"] |

## ADR Dependencies

| 字段 | 值 |
|-------|-------|
| **依赖于** | [ADR-NNNN（必须接受后才能实现），或"无"] |
| **启用** | [ADR-NNNN（此 ADR 解锁该决策），或"无"] |
| **阻止** | [史诗/用户故事名称 — 在此 ADR 接受之前不能开始，或"无"] |
| **排序说明** | [上面未捕获的任何排序约束] |

## Context

### 问题陈述
[我们正在解决什么问题？为什么需要现在做出此决策？]

### 约束
- [技术约束]
- [时间线约束]
- [资源约束]
- [兼容性要求]

### 需求
- [必须支持 X]
- [必须在 Y 预算内运行]
- [必须与 Z 集成]

## Decision

[做出的具体技术决策，描述足够详细以便某人实现它。]

### 架构图
[此创建的系统架构的 ASCII 图或描述]

### 关键接口
[此决策创建的 API 契约或接口定义]

## Alternatives Considered

### 替代方案 1：[名称]
- **描述**：[这将如何工作]
- **优点**：[优势]
- **缺点**：[劣势]
- **拒绝原因**：[为什么未选择此方案]

### 替代方案 2：[名称]
- **描述**：[这将如何工作]
- **优点**：[优势]
- **缺点**：[劣势]
- **拒绝原因**：[为什么未选择此方案]

## Consequences

### 正面
- [此决策的良好结果]

### 负面
- [接受的权衡和成本]

### 风险
- [可能出错的事情]
- [每个风险的缓解措施]

## GDD Requirements Addressed

| GDD 系统 | 需求 | 此 ADR 如何解决 |
|------------|-------------|--------------------------|
| [system-name].md | [来自该 GDD 的具体规则、公式或性能约束] | [此决策如何满足它] |

## Performance Implications
- **CPU**：[预期影响]
- **内存**：[预期影响]
- **加载时间**：[预期影响]
- **网络**：[预期影响（如果适用）]

## Migration Plan
[如果这改变了现有代码，我们如何从这里到那里？]

## Validation Criteria
[我们如何知道此决策是正确的？什么指标或测试？]

## Related Decisions
- [相关 ADR 的链接]
- [相关设计文档的链接]
```

5.5. **引擎专家验证** — 在保存之前，通过 Task 生成**主引擎专家**以验证起草的 ADR：
   - 读取 `.opencode/docs/technical-preferences.md` `Engine Specialists` 部分以获取主专家
   - 如果未配置引擎（`[TO BE CONFIGURED]`），跳过此步骤
   - 生成 `subagent_type: [主专家]`，包含：ADR 的 Engine Compatibility 部分、Decision 部分、Key Interfaces 和引擎参考文档路径。要求他们：
     1. 确认提议的方法对于固定的引擎版本是惯用的
     2. 标记任何已弃用或在训练截止后更改的 API 或模式
     3. 识别当前 ADR 草稿中未捕获的引擎特定风险或注意事项
   - 如果专家识别出**阻塞问题**（错误的 API、已弃用的方法、引擎版本不兼容）：相应地修改 Decision 和 Engine Compatibility 部分，然后在继续之前与用户确认更改
   - 如果专家仅发现**次要说明**：将它们纳入 ADR 的 Risks 子部分

**审查模式检查** — 在生成 TD-ADR 之前应用：
- `solo` → 跳过。注释："TD-ADR 跳过 — 独立模式。" 继续到步骤 5.7（GDD 同步检查）。
- `lean` → 跳过（不是阶段门）。注释："TD-ADR 跳过 — 精简模式。" 继续到步骤 5.7（GDD 同步检查）。
- `full` → 正常生成。

5.6. **技术总监战略审查** — 引擎专家验证后，通过 Task 使用门 **TD-ADR**（`.opencode/docs/director-gates.md`）生成 `technical-director`：
   - 传递：ADR 文件路径（或草稿内容）、引擎版本、领域、同一领域的现有 ADR
   - TD 验证架构一致性（此决策是否与整个系统一致？）— 与引擎专家的 API 级别检查不同
   - 如果 CONCERNS 或 REJECT：在继续之前相应地修改 Decision 或 Alternatives 部分

5.7. **GDD 同步检查** — 在呈现写入批准之前，扫描"GDD Requirements Addressed"部分中引用的所有 GDD，检查与 ADR 的 Key Interfaces 和 Decision 部分的命名不一致（重命名的信号、API 方法或数据类型）。如果发现任何不一致，将其作为**显眼的警告块**立即呈现在写入批准之前 — 而不是作为脚注：

```
⚠️ 需要 GDD 同步
[gdd-filename].md 使用了此 ADR 已重命名的名称：
  [old_name] → [new_name_from_adr]
  [old_name_2] → [new_name_2_from_adr]
必须在写入此 ADR 之前或同时更新 GDD，以防止开发人员阅读 GDD 时实现错误的接口。
```

如果没有不一致：静默跳过此块。

5. **写入批准** — 使用 `AskUserQuestion`：

如果发现 GDD 同步问题：
- "ADR 草稿已完成。您希望如何继续？"
  - [A] 在同一通道中写入 ADR + 更新 GDD
  - [B] 仅写入 ADR — 我将手动更新 GDD
  - [C] 还不行 — 我需要进一步审查

如果没有 GDD 同步问题：
- "ADR 草稿已完成。我可以写入吗？"
  - [A] 将 ADR 写入 `docs/architecture/adr-[NNNN]-[slug].md`
  - [B] 还不行 — 我需要进一步审查

如果对任何写入选项回答是，写入文件，如果需要则创建目录。
对于带有 GDD 更新的选项 [A]：还要更新 GDD 文件以使用新名称。

6. **更新架构注册表**

扫描写入的 ADR 以找到应注册的新架构立场：
- 它声称拥有的状态
- 它定义的接口契约（信号签名、方法 API）
- 它声称的性能预算
- 它明确做出的 API 选择
- 它禁止的模式（Consequences → Negative 或显式"不要使用 X"）

呈现候选项：
```
来自此 ADR 的注册表候选项：
  新状态所有权：      player_stamina → stamina-system
  新接口契约：       stamina_depleted 信号
  新性能预算：       stamina-system：0.5ms/帧
  新禁止模式：       每帧轮询 stamina（改用信号）
  现有（仅更新 referenced_by）：player_health → 已注册 ✅
```

**注册表追加逻辑**：写入 `docs/architecture/architecture.yaml` 时，不要假设部分为空。文件可能已经有此会话中先前写入的 ADR 条目。在每次 Edit 调用之前：
1. 读取 `docs/registry/architecture.yaml` 的当前状态
2. 找到正确的部分（state_ownership、interfaces、forbidden_patterns、api_decisions）
3. 在该部分的最后一个现有条目之后追加新条目 — 不要尝试替换可能不再存在的 `[]` 占位符
4. 如果该部分已有条目，请使用最后一个条目的结束内容作为 `old_string` 锚点，并在其后追加新条目

**阻塞 — 没有用户明确批准，不要写入 `docs/registry/architecture.yaml`。**

使用 `AskUserQuestion` 询问：
- "我可以使用这 [N] 个新立场更新 `docs/registry/architecture.yaml` 吗？"
  - 选项："是 — 更新注册表"、"还不行 — 我想查看候选项"、"跳过注册表更新"

仅在用户选择是时继续。如果是：追加新条目。永远不要修改现有条目 — 如果立场正在更改，请将旧条目设置为 `status: superseded_by: ADR-[NNNN]` 并添加新条目。

---

## 6. 结束下一步

ADR 写入后（注册表可选更新），使用 `AskUserQuestion` 结束。

在生成小组件之前：
1. 读取 `docs/registry/architecture.yaml` — 检查是否有任何优先 ADR 尚未写入（查找在 technical-preferences.md 或 systems-index.md 中标记为先决条件的 ADR）
2. 检查所有先决条件 ADR 是否已写入。如果是，包括"开始写入 GDD"选项。
3. 列出所有剩余的优先 ADR 作为单独选项 — 不仅仅是一两个。

小组件格式：
```
ADR-[NNNN] 已写入且注册表已更新。您接下来想做什么？
[1] 写入 [下一个优先级 ADR 名称] — [来自先决条件列表的简要描述]
[2] 写入 [另一个优先级 ADR] — [简要描述]（包括所有剩余的）
[N] 开始写入 GDD — 运行 `/design-system [第一个未设计的系统]`（仅在所有先决条件 ADR 已写入时显示）
[N+1] 本次会话到此为止
```

如果没有剩余的优先 ADR 和未设计的 GDD 系统，仅提供"到此为止"并建议在新会话中运行 `/architecture-review`。

**在结束输出中始终包含此固定通知（不要省略）：**

> 要根据 GDD 验证 ADR 覆盖范围，请打开**新的 Claude Code 会话**
> 并运行 `/architecture-review`。
>
> **永远不要在同一个会话中运行 `/architecture-review` 和 `/architecture-decision`。**
> 审查代理必须独立于编写上下文，以给出公正的评估。在此处运行将使审查无效。

将任何 `Status: Blocked` 等待此 ADR 的用户故事更新为 `Status: Ready`。