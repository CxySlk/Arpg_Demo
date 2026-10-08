---
name: reverse-document
description: "从现有实现生成设计或架构文档。从代码/原型反向工作以创建缺失的规划文档。"
argument-hint: "<type> <path> (例如, 'design src/gameplay/combat' 或 'architecture src/core')"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
# 只读诊断技能——不需要专家代理委派
---

# 反向文档化

此技能分析现有实现（代码、原型、系统）并生成适当的设计或架构文档。在以下情况使用：
- 在没有编写设计文档的情况下构建了功能
- 继承了没有文档的代码库
- 创建了机制原型并需要正式化
- 需要记录现有代码背后的"为什么"

---

## 工作流程

## 阶段1：解析参数

**格式**: `/reverse-document <type> <path>`

**类型选项**:
- `design` → 生成游戏设计文档（GDD部分）
- `architecture` → 生成架构决策记录（ADR）
- `concept` → 从原型生成概念文档

**路径**: 要分析的目录或文件
- `src/gameplay/combat/` → 所有战斗相关代码
- `src/core/event-system.cpp` → 特定文件
- `prototypes/stealth-mech/` → 原型目录

**示例**:
```bash
/reverse-document design src/gameplay/magic-system
/reverse-document architecture src/core/entity-component
/reverse-document concept prototypes/vehicle-combat
```

## 阶段2：分析实现

**阅读并理解代码/原型**：

**对于设计文档（GDD）：**
- 识别机制、规则、公式
- 提取游戏值（伤害、冷却时间、范围）
- 查找状态机、能力系统、进度
- 检测代码中处理的边缘情况
- 映射依赖关系（哪些系统交互？）

**对于架构文档（ADR）：**
- 识别模式（ECS、单例、观察者等）
- 理解技术决策（线程、序列化等）
- 映射依赖和耦合
- 评估性能特征
- 查找约束和权衡

**对于概念文档（原型分析）：**
- 识别核心机制
- 提取涌现的游戏模式
- 记录什么有效与什么无效
- 查找技术可行性见解
- 记录玩家幻想/手感

## 阶段3：询问澄清问题

**不要**只描述代码。**询问**意图：

**设计问题**：
- "我看到一个在[活动]期间消耗的[资源]系统。这是为了：
  - 节奏控制（防止滥用）？
  - 资源管理（战略深度）？
  - 还是其他目的？"
- "[机制]似乎是核心。这是核心支柱还是支持功能？"
- "[值]随[因素]指数增长。是有意设计，还是需要重新平衡？"

**架构问题**：
- "你使用了服务定位器模式。选择它是因为：
  - 可测试性（模拟依赖）？
  - 解耦（减少硬引用）？
  - 还是从现有代码继承的？"
- "我看到手动内存管理而不是智能指针。性能要求还是遗留代码？"

**概念问题**：
- "原型强调潜行而非战斗。这是预期的支柱吗？"
- "玩家似乎利用抓钩来加速。这是功能还是缺陷？"

## 阶段4：展示发现

在起草之前，展示你的发现：

```
我已分析了[path]/。以下是我的发现：

已实现的机制：
- [mechanic-a]，具有[property]（例如时序窗口、冷却时间）
- [mechanic-b]（例如两个状态之间的交互）
- [resource]系统（在[action]时消耗，在[condition]时恢复）
- [state]系统（积累，触发[effect]）

发现的公式：
- [Output] = [使用发现变量的公式]
- [Secondary output] = [formula]

不明确的意图领域：
1. [Resource]系统 — 节奏控制还是资源管理？
2. [Mechanic] — 核心支柱还是支持功能？
3. [Value]缩放 — 有意设计还是需要调整？

在我起草设计文档之前，你能澄清这些点吗？
```

等待用户澄清意图后再起草。

## 阶段5：使用模板起草文档

根据类型，使用适当的模板：

| 类型 | 模板 | 输出路径 |
|------|----------|-------------|
| `design` | `templates/design-doc-from-implementation.md` | `design/gdd/[system-name].md` |
| `architecture` | `templates/architecture-doc-from-code.md` | `docs/architecture/[decision-name].md` |
| `concept` | `templates/concept-doc-from-prototype.md` | `prototypes/[name]/CONCEPT.md` 或 `design/concepts/[name].md` |

**起草结构**：
- 捕获**现有内容**（机制、模式、实现）
- 记录**为什么存在**（与用户澄清的意图）
- 识别**缺失内容**（未处理的边缘情况、设计差距）
- 标记**后续工作**（平衡调整、缺失功能）

## 阶段6：展示草稿并请求批准

**协作协议**：
```
我已根据你的代码和澄清起草了[system-name]设计文档。

[展示关键部分：概述、机制、公式、设计意图]

我的补充：
- 根据你的澄清将[mechanic]记录为"[intent]"
- 添加了代码中没有的边缘情况（例如，如果[resource]在[action]中途达到0会怎样？）
- 标记了平衡关注：在[boundary condition]的[scaling type]缩放

标记为不完整的部分：
- "[System]与[other-system]的交互"（尚未完全实现）
- "[Variant或feature]"（目前仅实现[subset]）

我可以将其写入design/gdd/[system-name].md吗？
```

等待批准。用户可能在写入前请求更改。

## 阶段7：带元数据写入文档

批准后，带特殊标记写入文件：

```markdown
---
status: reverse-documented
source: [path/]
date: [today]
verified-by: [User name]
---

# [System Name] 设计

> **注意**：此文档是从现有实现反向工程的。
> 它捕获了当前行为和澄清的设计意图。在实现不完整或意图不明确的地方，
> 某些部分可能不完整。

[文档其余部分...]
```

## 阶段8：标记后续工作

写入后，建议下一步：

```
✅ 已写入design/gdd/combat-system.md

建议后续：
1. 对战斗公式运行/balance-check（指数缩放关注）
2. 为耐力系统架构决策创建ADR
3. 实现缺失的边缘情况：
  - 连招中途耐力耗尽的行为
  - 闪避时的踉跄状态
4. 当武器多样性实现时扩展设计文档

你希望我现在处理这些吗？
```

---

## 模板选择逻辑

| 如果分析... | 使用模板... | 因为... |
|----------------|-----------------|------------|
| `src/gameplay/*` | design-doc-from-implementation.md | 游戏机制 → GDD |
| `src/core/*`、`src/ai/*` | architecture-doc-from-code.md | 核心系统 → ADR |
| `prototypes/*` | concept-doc-from-prototype.md | 实验 → 概念文档 |
| `src/networking/*` | architecture-doc-from-code.md | 技术系统 → ADR |
| `src/ui/*` | design-doc-from-implementation.md | UI/UX → 设计规格 |

---

## 示例会话：反向文档化一个系统

```
用户: /reverse-document design src/gameplay/[system]

代理: 我将分析你的[system]实现以创建设计文档。
       [读取代码，发现机制、公式]

代理: 我已分析了src/gameplay/[system]/。以下是我的发现：
       [展示机制、公式、不明确的领域]

       在起草之前，你能澄清：
       1. [Resource]系统的意图？
       2. [mechanic]是核心支柱吗？
       3. [value]缩放 — 有意设计还是需要调整？

用户: 1. [Resource]用于节奏控制，防止[unwanted behavior]
      2. [mechanic]是核心 — 我想要[design intent]
      3. 是的，指数太多了，应该是线性的

代理: 完美，这澄清了愿景。让我起草设计文档。
       [展示带有澄清意图的草稿]

       我可以将其写入design/gdd/[system-name].md吗？

用户: 是的，写入。

代理: ✅ 已写入design/gdd/[system-name].md
       ✅ 已标记为[REVERSE-DOCUMENTED]
       ✅ 已标记[value]缩放用于重新平衡

       后续步骤：
       - 更新[formula]为[corrected scaling]
       - 运行/balance-check验证[curve]
       - 在game-pillars.md中将[mechanic]记录为核心支柱
```

---

## 协作协议

此技能遵循协作设计原则：

1. **先分析**：读取代码，理解实现
2. **询问意图**：询问"为什么"，而不仅仅是"是什么"
3. **展示发现**：展示发现，突出不明确的领域
4. **用户澄清**：区分意图与偶然
5. **起草文档**：基于现实+意图创建文档
6. **展示草稿**：展示关键部分，解释补充内容
7. **获得批准**："我可以写入[filepaths]吗？"批准时：结论：**完成** — 文档已生成。拒绝时：结论：**被阻止** — 用户拒绝写入。
8. **标记后续**：建议相关工作，不要自动执行

**永远不要假设意图。在记录"为什么"之前始终询问。**
