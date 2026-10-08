# Odin Inspector 参考手册

> 适用于本项目 `Assets/LLKFrame/Plugin/Sirenix`（Odin Inspector **3.1+**，含 `SdfIconType`）。
> 数据来源：官方文档 [odininspector.com/attributes](https://odininspector.com/attributes) 与
> [API Documentation](https://odininspector.com/documentation)，2026-08 抓取核实。
> 命名空间约定（本仓库 AGENTS.md）：私有字段 `_xxx`、public 字段小写、属性大写、禁止 `var`。

---

## 目录

1. [核心基类](#1-核心基类)
2. [分组类（Groups）](#2-分组类groups)
3. [按钮类（Buttons）](#3-按钮类buttons)
4. [基础美化（Essentials）](#4-基础美化essentials)
5. [条件控制（Conditionals）](#5-条件控制conditionals)
6. [集合类（Collections）](#6-集合类collections)
7. [数值类（Numbers）](#7-数值类numbers)
8. [类型专属（Type Specifics）](#8-类型专属type-specifics)
9. [回调与消息（Misc）](#9-回调与消息misc)
10. [校验（Validation）](#10-校验validation)
11. [调试与元（Debug & Meta）](#11-调试与元debug--meta)
12. [常用枚举速查](#12-常用枚举速查)
13. [本项目实践对照](#13-本项目实践对照)

---

## 1. 核心基类

| 类 | 命名空间 | 用途 |
|---|---|---|
| `SerializedScriptableObject` | `Sirenix.OdinInspector` | 由 Sirenix 序列化的 SO，支持 Dictionary、多态 `[SerializeReference]` 等。继承 `ScriptableObject`。本仓库 `AbilityAsset : ConfigBase : SerializedScriptableObject` |
| `SerializedMonoBehaviour` | `Sirenix.OdinInspector` | 同上，MonoBehaviour 版。继承 `MonoBehaviour` |
| `OdinEditorWindow` | `Sirenix.OdinInspector.Editor` | 自定义编辑器窗口基类（继承 `EditorWindow`）。**窗口自身即检视对象**：字段加 `[ShowInInspector]`、方法加 `[OnInspectorGUI]`/`[Button]` 即被 Odin 自动绘制。本仓库 `GASEditorWindow` 即此类 |
| `OdinMenuEditorWindow` | `Sirenix.OdinInspector.Editor` | 带左侧菜单树（`OdinMenuTree`）的窗口，继承 `OdinEditorWindow`。需实现 `BuildMenuTree()` |
| `OdinEditor` | `Sirenix.OdinInspector.Editor` | 自定义 Inspector 基类（继承 `UnityEditor.Editor`），配合 `[CustomEditor]`。`DrawDefaultInspector()` 画 Odin 检视器，`DrawUnityInspector()` 画原生检视器 |
| `OdinMenuTree` | `Sirenix.OdinInspector.Editor` | `OdinMenuItem` 树结构，负责选中/插入/键盘导航，可独立于窗口使用 |
| `OdinSelector<T>` | `Sirenix.OdinInspector.Editor` | 选择器抽象基类，派生 `EnumSelector<T>`/`GenericSelector<T>`/`TypeSelector`；`ShowInPopup(...)` 弹窗选择 |

### OdinEditorWindow 用法示例

```csharp
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

public class MyWindow : OdinEditorWindow
{
    [MenuItem("Tools/My Window")]
    private static void Open()
    {
        GetWindow<MyWindow>().Show();
    }

    [ShowInInspector]
    private string someText;

    [Button("点我")]
    private void Click() { }

    [OnInspectorGUI]
    private void DrawCustom() { }          // 手绘追加内容

    protected override void OnEnable() { base.OnEnable(); }    // 生命周期挂钩（如 SceneGizmos 注册）
    protected override void OnDestroy() { base.OnDestroy(); }  // 注销
}
```

---

## 2. 分组类（Groups）

> **分组路径语法**：`"父组/子组"` 用 `/` 嵌套，可组合（如 `[HorizontalGroup("A/B")]` + `[BoxGroup("A/B/C")]`）。

### BoxGroup — 盒子分组
将相关属性组织进一个**带标题的圆角方框**。可空标题、居中标题、动态标题（`$` 引用成员）。

```csharp
[BoxGroup("Some Title")]
public string A;
[BoxGroup("Some Title")]
public string B;

[BoxGroup("Centered Title", centerLabel: true)]
public string C;

[BoxGroup("$G")]                       // 动态标题，取成员 G 的值
public string E;
public string G = "Dynamic box title 2";

[BoxGroup]                             // 无标题框
public string H;

[BoxGroup("NoTitle", false)]           // 具名但隐藏标题
public string I;
```

### FoldoutGroup — 折叠分组
将属性组织进可**折叠/展开**的组，适合收起不常用配置。`expanded` 控制初始展开状态。

```csharp
[FoldoutGroup("Group 1")]
public int A;
[FoldoutGroup("Collapsed group", expanded: false)]
public int D;
```

### HorizontalGroup — 水平分组
多个属性**水平并排**。宽度可为百分比（0~1）或像素，0 自动分配。

```csharp
[HorizontalGroup("row1", Width = 0.25f)] // 25% 宽
public float Left3;
[HorizontalGroup("row1", Width = 150)]   // 150px 宽
public float Center3;
[HorizontalGroup("row1")]                // 剩余自动
public float Right3;

[HorizontalGroup("row2", MarginRight = 0.4f)]
public float Left2;
[HorizontalGroup("row2")]
public float Right2;

[HorizontalGroup("row3", Gap = 3)]
public float Left4;
```

### VerticalGroup — 垂直分组
把属性垂直聚集，常与 `HorizontalGroup` 组合做分栏布局（单独用效果弱）。

```csharp
[HorizontalGroup("Split")]
[VerticalGroup("Split/Left")]
public float First;
[VerticalGroup("Split/Right")]
[BoxGroup("Split/Right/Box C")]
public int BoxC;
```

### TabGroup — 页签分组
把属性组织进**多个 Tab 页签**。支持图标（`SdfIconType`）、文字颜色、`TabLayouting`（多行/收缩）。

```csharp
[TabGroup("General")]
public string playerName1;
[TabGroup("Stats")]
public int strength1;

// 带图标 + 颜色 + 多行布局
[TabGroup("shrink tabs", "World Map", SdfIconType.Map, TextColor = "orange", TabLayouting = TabLayouting.Shrink)]
[TabGroup("shrink tabs", "Character", SdfIconType.PersonFill, TextColor = "orange")]
public float a, b;
```

### TitleGroup — 标题分组
按标题垂直分组，可带副标题、水平分隔线、粗体、居中。

```csharp
[TitleGroup("Ints")]
public int SomeInt1;

[TitleGroup("Vectors", "Optional subtitle", alignment: TitleAlignments.Centered,
     horizontalLine: true, boldTitle: true, indent: false)]
public Vector2 SomeVector1;
```

### ToggleGroup — 开关分组
用 bool 成员控制一组属性的显隐（组标题可动态）。

```csharp
[ToggleGroup("MyToggle")]
public bool MyToggle;
[ToggleGroup("MyToggle")]
public float A;
```

### ShowIfGroup / HideIfGroup — 条件分组
整组按条件显示/隐藏，可嵌在其它分组内。

```csharp
[ShowIfGroup("Toggle")]
[BoxGroup("Toggle/Shown Box")]
public int A;

[ShowIfGroup("Box/Toggle/EnumField", Value = InfoMessageType.Info)]
public string Name;

[HideIfGroup("Toggle")]
[BoxGroup("Toggle/Shown Box")]
public int B;
```

---

## 3. 按钮类（Buttons）

### Button — 方法按钮
在 Inspector 中为方法生成按钮。支持带参/ref/out 方法、图标、对齐、拉伸。

```csharp
[Button]                              // 默认按钮，显示方法名
private void DefaultButton() { }

[Button("自定义名")]                    // 自定义文字
private void NamedButton() { }

[Button("$ButtonName")]               // 动态名称
private void Dynamic() { }
public string ButtonName = "Run";

[Button(ButtonSizes.Gigantic)]        // 尺寸：Small/Medium/Large/Gigantic 或自定义像素
private void Gigantic() { }

[Button(ButtonSizes.Large, Icon = SdfIconType.Plus)]   // 图标按钮
private void WithIcon() { }

[Button(ButtonSizes.Large, Icon = SdfIconType.Plus,
        Style = ButtonStyle.FoldoutButton)]            // 参数折叠进按钮（点箭头展开输入框）
private void AddTag(string tag) { }

[Button(ButtonSizes.Large, Icon = SdfIconType.Plus,
        Name = "添加标签", Style = ButtonStyle.Box)]    // Box 大按钮
private void BoxButton(string tag) { }
```

> `ButtonStyle`：`Default`（参数显示在按钮上方）/ `FoldoutButton`（参数折叠）/ `Box`（方框）/ `CompactBox`（紧凑方框）。

### ButtonGroup — 按钮组
多个按钮水平排成一组。

```csharp
[ButtonGroup]           // 默认组
private void A() { }
[ButtonGroup]
private void B() { }

[ButtonGroup("My Group")]               // 具名组
private void E() { }

[ButtonGroup(ButtonHeight = 25), Button(SdfIconType.Crop, "")]   // 图标按钮+高度
private void Crop() { }
```

### ResponsiveButtonGroup — 自适应按钮组
按可用空间自动换行/对齐，适合工具类窗口。

```csharp
[ResponsiveButtonGroup("UniformGroup", UniformLayout = true)]
public void Foo1() { }
[ResponsiveButtonGroup("UniformGroup")]
public void Foo2() { }

[ResponsiveButtonGroup("DefaultButtonSize", DefaultButtonSize = ButtonSizes.Small)]
public void Bar1() { }
[Button(ButtonSizes.Large), ResponsiveButtonGroup("DefaultButtonSize")]
public void Bar4() { }
```

### InlineButton — 行内按钮
在属性**末尾**加小按钮，点击调指定方法。

```csharp
[InlineButton("A")]                    // 属性末尾一个按钮
public int InlineButton;

[InlineButton("A")]
[InlineButton("B", "Custom Button Name")]  // 多个链式按钮
public int ChainedButtons;

[InlineButton("C", SdfIconType.Dice6Fill, "Random")]   // 图标按钮
public int IconButton;
```

### EnumToggleButtons — 枚举按钮组
枚举以水平按钮组绘制（替代下拉框），支持 `[Flags]` 与带图标枚举。

```csharp
[EnumToggleButtons]
public SomeEnum SomeEnumField;
public enum SomeEnum { First, Second, Third, Fourth }
```

### EnumPaging — 枚举翻页
用 ←/→ 按钮循环切换枚举值。

```csharp
[EnumPaging]
public SomeEnum SomeEnumField;
```

---

## 4. 基础美化（Essentials）

### ShowInInspector — 显示在检视器
让任意成员（字段/属性/方法）显示在 Inspector。**不序列化**，改动不保存。常用于只读计算属性、static 成员。

```csharp
[ShowInInspector] private int myPrivateInt;
[ShowInInspector] public string SomeText { get; set; }
```

### Title — 标题
在属性上方画粗体大标题。

```csharp
[Title("Static title", "Static subtitle")]   // 标题 + 副标题
public int E;

[Title("标签管理", TitleAlignment = TitleAlignments.Centered)]
public int F;
```

### PropertyOrder — 排序
控制 Inspector 显示顺序（数字越小越靠前，可为负）。

```csharp
[PropertyOrder(-1)] public int First;
[PropertyOrder(1)] public int Second;
```

### PropertySpace — 留白
属性上下留白（与 `[Space]` 类似但可用于属性）。

```csharp
[PropertySpace(SpaceBefore = 30, SpaceAfter = 60)]
public int BeforeAndAfter;
```

### ReadOnly — 只读
禁止在 Inspector 中修改，仅显示。

```csharp
[ReadOnly] public string MyString = "display only";
```

### GUIColor — 颜色
改变绘制属性时的 GUI 颜色。支持 RGBA、十六进制、命名颜色、动态（`@` 表达式/成员引用）。

```csharp
[GUIColor(0.3f, 0.8f, 0.8f, 1f)]
public int ColoredInt1;
[GUIColor("#FF0000")]
public int Red;
[GUIColor("orange")]
public int Orange;
```

### HideLabel — 隐藏标签
隐藏属性标签，值占满整行。

```csharp
[HideLabel] public Vector3 WideVector1;
```

### LabelText — 改标签文字
自定义显示标签，支持图标。

```csharp
[LabelText("Test", SdfIconType.HeartFill)]
public int LabelIcon1 = 123;
```

### LabelWidth — 标签宽度
```csharp
[LabelWidth(250)] public int Wide;
```

### SuffixLabel — 后缀标签
属性末尾显示单位说明（`Overlay` 叠加在属性上）。

```csharp
[SuffixLabel("ms", Overlay = true)] public float Speed;
```

### PropertyTooltip — 悬停提示
```csharp
[PropertyTooltip("This is tooltip on an int property.")] public int MyInt;
```

### DisplayAsString — 纯文本显示
以纯文本显示（不可编辑），常用于调试信息。

```csharp
[DisplayAsString(false, 20, TextAlignment.Center, true)]
public string DebugInfo;
```

### DelayedProperty — 延迟应用
编辑时回车/失焦后才生效（与 Unity `[Delayed]` 类似，可用于属性）。

```csharp
[ShowInInspector, DelayedProperty]
public string DelayedProperty { get; set; }
```

### Indent — 缩进
```csharp
[Indent] public int A;
[Indent(4)] public int D;
```

---

## 5. 条件控制（Conditionals）

### ShowIf / HideIf — 按条件显示/隐藏
引用字段/方法/属性或 `@` 表达式；可比较指定值。

```csharp
public bool IsToggled;

[ShowIf("IsToggled")]
public Vector3 ShownWhenToggled;

[HideIf("IsToggled")]
public Vector3 HiddenWhenToggled;

[ShowIf("SomeEnum", InfoMessageType.Info)]   // 枚举等于某值时显示
public Vector3 Info;
public SomeEnum SomeEnum;
```

### DisableIf / EnableIf — 按条件禁用/启用
```csharp
public bool IsToggled;

[EnableIf("IsToggled")] public int EnableIfToggled;
[DisableIf("SomeEnum", InfoMessageType.Info)] public Vector2 Info;
```

### 模式类条件（PrefabKind / 编辑器-播放模式）
| 属性 | 效果 |
|---|---|
| `[ShowIn(PrefabKind.InstanceInScene)]` | 仅当所在 prefab 类型匹配时显示 |
| `[HideIn(PrefabKind.Regular)]` | 匹配时隐藏 |
| `[DisableIn(PrefabKind.PrefabAsset)]` | 匹配时禁用 |
| `[HideInEditorMode]` / `[DisableInEditorMode]` | 编辑器模式隐藏/禁用 |
| `[HideInPlayMode]` / `[DisableInPlayMode]` | 播放模式隐藏/禁用 |
| `[HideInInlineEditors]` / `[DisableInInlineEditors]` / `[ShowInInlineEditors]` | 在 InlineEditor 内部时隐藏/禁用/仅显示 |
| `[HideInTables]` | 不作为表格列显示 |
| `[HideMonoScript]`（类级） | 隐藏 Inspector 顶部的 Script 引用 |
| `[HideReferenceObjectPicker]` | 隐藏 `[SerializeReference]` 多态选择器 |

---

## 6. 集合类（Collections）

### ListDrawerSettings — 列表绘制定制
**本项目 GASEditorData 的 `_tags` 列表即用此美化**。Odin 列表默认支持拖拽排序、逐项删除、分页、多资源拖入。

```csharp
[SerializeField]
[ListDrawerSettings(HideAddButton = true, ShowItemCount = true, DraggableItems = false)]
private List<string> _tags = new List<string>();
```

可用参数：
| 参数 | 作用 |
|---|---|
| `HideAddButton` | 隐藏 `+` 添加按钮（用自定义 `[Button]` 添加） |
| `ShowItemCount` | 显示"共 N 项"计数 |
| `DraggableItems` | 是否可拖拽排序 |
| `HideRemoveButton` | 隐藏每项 `×` 删除 |
| `IsReadOnly` | 只读列表 |
| `ShowIndexLabels` | 显示索引 |
| `ShowFoldout` / `ShowPaging` | 折叠 / 分页 |
| `NumberOfItemsPerPage` | 每页条数 |
| `ListElementLabelName` | 元素自定义标签字段名 |
| `CustomAddFunction` | 自定义添加方法（返回新值） |
| `OnBeginListElementGUI` / `OnEndListElementGUI` | 元素绘制包装回调 |
| `OnTitleBarGUI` | 标题栏额外按钮回调 |

### DictionaryDrawerSettings — 字典绘制定制
```csharp
[DictionaryDrawerSettings(KeyLabel = "键", ValueLabel = "值", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
public Dictionary<string, int> MyDict;
```

### TableList — 表格列表
把 List/数组渲染成表格。

```csharp
[TableList(ShowIndexLabels = true)]
public List<MyRow> Rows;
[TableList(AlwaysExpanded = true, DrawScrollView = false)]
public List<MyRow> AlwaysExpandedTable;
```

### TableMatrix — 二维数组表格
```csharp
[TableMatrix(HorizontalTitle = "Square Celled Matrix", SquareCells = true)]
public Texture2D[,] SquareCelledMatrix;
```

### TableColumnWidth — 表格列宽
```csharp
[TableColumnWidth(60)] public int ID;
[TableColumnWidth(30, Resizable = false)] public Texture2D Icon;
```

---

## 7. 数值类（Numbers）

| 属性 | 作用 | 示例 |
|---|---|---|
| `[MinValue(0)]` | 下限封顶 | `[MinValue(0)] public int M;` |
| `[MaxValue(100)]` | 上限封顶 | `[MaxValue(100)] public int M;` |
| `[PropertyRange(0, 10)]` | 滑块限值（可作用于属性） | `[ShowInInspector, PropertyRange(0, 10)] public int P { get; set; }` |
| `[MinMaxSlider(-10, 10)]` | 区间滑块，Vector2（x=min,y=max） | `[MinMaxSlider(-10, 10)] public Vector2 Range;` |
| `[ProgressBar(0, 100)]` | 水平进度条 | `[ProgressBar(0, 100)] public int Hp;` |
| `[Wrap(0f, 360)]` | 值超界环绕（角度等） | `[Wrap(0f, 360)] public float Angle;` |
| `[Unit(Units.Kilogram)]` | 单位换算显示 | `[Unit(Units.MetersPerSecond, Units.KilometersPerHour)] public float Speed;` |

---

## 8. 类型专属（Type Specifics）

### InlineEditor — 内联编辑器
把 Object 引用内联展开编辑（组件/资产预览+编辑），模式见 `InlineEditorModes`。

```csharp
[InlineEditor(InlineEditorModes.FullEditor)]
public Material FullInlineEditor;
[InlineEditor(InlineEditorModes.SmallPreview)]
public Material SmallPreview;
[InlineEditor(InlineEditorObjectFieldModes.Boxed)]
public Transform Boxed;
```

### PreviewField — 预览方块
Object 字段带预览图，支持拖拽赋值/替换。

```csharp
[PreviewField(50, ObjectFieldAlignment.Right)]
public Object D;
```

### AssetList — 资产列表
按过滤条件列出项目资产，可直接下拉选择。

```csharp
[AssetList(Path = "Assets/Config/")]
public List<ScriptableObject> AssetList;
[AssetList(AutoPopulate = true, Path = "Assets/Config/")]
public List<ScriptableObject> AutoPopulated;
```

### AssetSelector — 资产选择器
对象字段旁加按钮，弹可搜索资产选择器。

```csharp
[AssetSelector(Paths = "Assets/MyScriptableObjects")]
public ScriptableObject FromFolder;
```

### FilePath / FolderPath — 文件/文件夹路径
```csharp
[FilePath(Extensions = "cs")] public string ScriptFiles;
[FilePath(ParentFolder = "Assets/Resources")] public string ResourcePath;
[FolderPath(ParentFolder = "Assets/Resources")] public string Folder;
```

### ChildGameObjectsOnly — 仅子物体
```csharp
[ChildGameObjectsOnly] public Transform ChildOrSelfTransform;
[ChildGameObjectsOnly(IncludeSelf = false)] public Light[] Lights;
```

### SceneObjectsOnly / AssetsOnly — 场景对象/资产限定
```csharp
[SceneObjectsOnly] public List<GameObject> OnlySceneObjects;
[AssetsOnly] public Material MaterialAsset;
```

### ColorPalette — 调色板
```csharp
[ColorPalette("Underwater")] public Color UnderwaterColor;
```

### Toggle — 启用开关
在字段旁加勾选框控制启用/禁用。

```csharp
[Toggle("Enabled")] public MyToggleable Toggler = new MyToggleable();
[Serializable, Toggle("Enabled")] public class MyToggleable { public bool Enabled; public string Text; }
```

### ToggleLeft — 勾选框靠左
```csharp
[ToggleLeft] public bool LeftToggled;
```

### MultiLineProperty — 多行文本
Odin 版 TextArea，可作用于属性/方法参数。

```csharp
[MultiLineProperty(10)] public string Description = "";
```

### InlineProperty — 内联平铺
把类型内容平铺在标签旁（不折叠）。

```csharp
[InlineProperty(LabelWidth = 13)] public Vector2Int MyVector2Int;
```

### TypeFilter / TypeInfoBox / TypeSelectorSettings
- `[TypeFilter("GetFilteredTypeList")]` — 多态字段类型过滤（方法返回 `IEnumerable<Type>`）
- `[TypeInfoBox("...")]`（类级）— 类型顶部信息框
- `[TypeSelectorSettings(ShowCategories = true, PreferNamespaces = true)]` — 类型选择器设置

---

## 9. 回调与消息（Misc）

### OnValueChanged — 值变更回调
值在 Inspector 被修改时调用方法。**本项目用字符串形式**（如 `"OnTagChanged"`）而非 `nameof`，因为方法在 `#if UNITY_EDITOR` 里。

```csharp
[SerializeField, OnValueChanged("OnTagChanged")]
private string _tagName;
private void OnTagChanged() { /* 更新 hash */ }
```

### OnInspectorGUI — 自定义绘制
在属性旁/后追加自定义 GUI 绘制。

```csharp
[OnInspectorGUI("DrawPreview", append: true)]
public Texture2D Texture;
```

### OnInspectorInit / OnInspectorDispose — 初始化/销毁回调
属性 drawer 初始化/释放时执行，可传方法名或 `@` 表达式。

```csharp
[OnInspectorInit("@TimeWhenExampleWasOpened = System.DateTime.Now.ToString()")]
public string TimeWhenExampleWasOpened;
[OnInspectorDispose("@UnityEngine.Debug.Log(\"Dispose event invoked!\")")]
public BaseClass PolymorphicField;
```

### OnStateUpdate — 状态更新
每帧至少一次回调，可用于自定义状态控制（如 `$property.State.Visible/Expanded`）。

```csharp
[OnStateUpdate("@$property.State.Expanded = ExpandList")]
public List<string> list;
```

### OnCollectionChanged — 集合变更回调
集合在 Inspector 修改前/后回调，回调签名 `void X(CollectionChangeInfo info, object value)`。

```csharp
[OnCollectionChanged("Before", "After")]
public List<string> list;
```

### InfoBox / DetailedInfoBox — 信息框
```csharp
[InfoBox("Warning info box.", InfoMessageType.Warning)]
public int B;
[InfoBox("$InfoMessage")] public string InfoMessage = "动态消息";
[InfoBox("提示", InfoMessageType.Info, "VisibleWhenTrue")] public int C;
public bool VisibleWhenTrue = true;

[DetailedInfoBox("Click to reveal", "...more details...")]
public int Field;
```

### CustomContextMenu / DisableContextMenu — 右键菜单
```csharp
[CustomContextMenu("Say Hello/Twice", "SayHello")]
public int MyProperty;
private void SayHello() { Debug.Log("Hello Twice"); }

[DisableContextMenu] public int[] NoRightClickList;
```

### CustomValueDrawer — 自定义值绘制器
用方法当一次性绘制器，自带 Undo/多选。

```csharp
[CustomValueDrawer("MyCustomDrawerStatic")]
public float CustomDrawerStatic;
private static float MyCustomDrawerStatic(float value, GUIContent label)
{
    return EditorGUILayout.Slider(label, value, 0f, 10f);
}
```

### DrawWithUnity — 用 Unity 原生绘制
```csharp
[DrawWithUnity] public GameObject ObjectDrawnWithUnity;
```

### HideDuplicateReferenceBox — 隐藏重复引用框
```csharp
[HideDuplicateReferenceBox] public MyClass WithoutReferenceBox;
```

---

## 10. 校验（Validation）

| 属性 | 作用 | 示例 |
|---|---|---|
| `[Required("custom msg")]` | 必填对象，为空时提示 | `[Required] public Rigidbody Rb;` |
| `[RequiredListLength(3, 10)]` | 列表长度校验 | `[RequiredListLength(3, 10)] public List<int> L;` |
| `[ValidateInput("CheckMethod")]` | 自定义校验方法 | `[ValidateInput("MustBeNull", "This field should be null.")] public ScriptableObject Obj;` |
| `[DisallowModificationsIn(PrefabKind.PrefabInstance)]` | 禁用成员修改并校验 | `[DisallowModificationsIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)] public string S;` |
| `[AssetsOnly]` / `[SceneObjectsOnly]` | 资产/场景对象限定 | 见第 8 节 |
| `[FilePath(RequireExistingPath = true)]` / `[FolderPath]` | 路径有效性校验 | 见第 8 节 |
| `[MinValue]` / `[MaxValue]` / `[PropertyRange]` | 数值范围 | 见第 7 节 |

ValidateInput 校验方法签名（可带动态错误消息）：
```csharp
private bool CheckMethod(string value, ref string errorMessage, ref InfoMessageType? messageType)
{
    if (string.IsNullOrEmpty(value)) { errorMessage = "不能为空"; messageType = InfoMessageType.Error; return false; }
    return true;
}
```

---

## 11. 调试与元（Debug & Meta）

| 属性 | 作用 |
|---|---|
| `[ShowDrawerChain]` | 列出该属性使用的所有 drawer（调试属性为什么这样绘制） |
| `[ShowPropertyResolver]` | 显示负责该成员的 property resolver（调试"为什么显示了/不显示"） |
| `[SuppressInvalidAttributeError]` | 抑制"属性应用到不适用类型"的错误提示（泛型场景） |
| `[EnableGUI]` | 启用本来会被禁用的属性 GUI（配合 `[ShowInInspector]` 计算属性） |

```csharp
[ShowDrawerChain] [HideIf("ToggleHideIf")]
public GameObject SomeObject;

[ShowInInspector, EnableGUI]
public int GUIEnabledProperty { get { return 10; } }
```

---

## 12. 常用枚举速查

| 枚举 | 命名空间 | 值与说明 |
|---|---|---|
| `ButtonSizes` | `Sirenix.OdinInspector` | `Small` / `Medium` / `Large` / `Gigantic`（或直接传像素 int） |
| `ButtonStyle` | `Sirenix.OdinInspector` | `Default`（参数在上方）/ `FoldoutButton`（参数折叠）/ `Box` / `CompactBox` |
| `SdfIconType` | `Sirenix.OdinInspector` | SDF 矢量图标枚举（数百个：`Plus`/`GearFill`/`Map`/`PersonFill`/`BarChartLineFill`/`Dice6Fill` 等），用于 Button/菜单/Toast |
| `IconAlignment` | `Sirenix.OdinInspector` | `LeftOfText` / `RightOfText` / `LeftEdge` / `RightEdge` |
| `TitleAlignments` | `Sirenix.OdinInspector` | `Left` / `Right` / `Centered` / `Split` |
| `InfoMessageType` | `Sirenix.OdinInspector` | `None` / `Info` / `Warning` / `Error` |
| `TabLayouting` | `Sirenix.OdinInspector` | `Default` / `Shrink` / `MultiRow` |
| `DictionaryDisplayOptions` | `Sirenix.OdinInspector` | `ExpandedFoldout` / `Foldout` |
| `InlineEditorModes` | `Sirenix.OdinInspector` | `FullEditor` / `GUIAndHeader` / `SmallPreview` / `LargePreview` |
| `InlineEditorObjectFieldModes` | `Sirenix.OdinInspector` | `Boxed` / `Foldout` / `Hidden` / `CompletelyHidden` |
| `ObjectFieldAlignment` | `Sirenix.OdinInspector` | `Left` / `Center` / `Right` |
| `PrefabKind` | `Sirenix.OdinInspector` | `Regular`/`Variant`/`PrefabAsset`/`PrefabInstance`/`InstanceInScene`/`NonPrefabInstance` 等（位标志可组合） |
| `Units` | `Sirenix.OdinInspector` | `Kilogram`/`MetersPerSecond`/`KilometersPerHour` 等（配合 `[Unit]`） |
| `SearchFilterOptions` | `Sirenix.OdinInspector` | `ISearchFilterableInterface` 等（配合 `[Searchable]`） |

---

## 13. 本项目实践对照

### GASEditorData.cs（标签管理）
```csharp
/// <summary>全量标签，由编辑器 UI 维护（添加时自动排序、判重）</summary>
[BoxGroup("标签管理")]
[SerializeField]
[PropertySpace(0, 12)]
[ListDrawerSettings(HideAddButton = true, ShowItemCount = true, DraggableItems = false)]
private List<string> _tags = new List<string>();

public List<string> Tags => _tags;

[BoxGroup("标签管理")]
[GUIColor(0, 1, 0)]
[Button("＋ 添加标签", ButtonSizes.Large, Icon = SdfIconType.Plus,
        Name = "添加标签", Style = ButtonStyle.FoldoutButton)]
private void AddTag(string tag)
{
    if (string.IsNullOrEmpty(tag) || _tags.Contains(tag)) return;
    _tags.Add(tag);
    _tags.Sort();
    EditorUtility.SetDirty(this);
    AssetDatabase.SaveAssets();
}
```

### 关键用法要点（结合本仓库经验）
1. **`[Button]` 带参方法**：Odin 自动为参数生成输入框并内部管理输入状态，不会像手写 IMGUI 局部变量那样丢输入。
2. **`[FoldoutButton]` vs `[Default]`**：参数折叠 vs 常显。若按钮和输入框贴太近，用 `[PropertySpace]` 或 BoxGroup 内边距隔开。
3. **字符串属性名**（`OnValueChanged`/`ValueDropdown`）：用字符串而非 `nameof`，因方法在 `#if UNITY_EDITOR` 块内。
4. **`[ValueDropdown("...")]`**：Odin 运行时反射解析字符串，方法签名变动（如 `GetTags(TagCategory.Effect)` → `GetTags()`）会导致下拉失效但**不报编译错**，改签名务必同步改引用。
5. **编辑器窗口**：继承 `OdinEditorWindow`，`[ShowInInspector]` 字段 + `[OnInspectorGUI]` 方法自动绘制；生命周期在 `OnEnable`/`OnDestroy` 挂钩（如 SceneGizmos 的 `SceneView.duringSceneGui` 注册/注销）。
6. **不要在拖动字段的回调里调 `AssetDatabase.SaveAssets()`**（每帧写盘卡死）；用 `EditorUtility.SetDirty` + 合适时机保存。

---

### 官方文档链接
- 属性总览：https://odininspector.com/attributes
- API 文档：https://odininspector.com/documentation
- 教程：https://odininspector.com/tutorials
- 更新日志：https://odininspector.com/patch-notes
