# LLKFrame Unity 框架

## 目录结构

`
Assets/LLKFrame/
├── Plugin/             # 第三方插件（Odin Inspector 等）
├── Prefab/             # 框架预制体
├── Scripts/            # 框架核心脚本
│   ├── 0.Base/         # 基类
│   ├── 1.System/       # 系统模块
│   ├── 2.Component/    # 组件
│   ├── 3.Extension/    # 扩展方法
│   ├── 4.Setting/      # 配置
│   └── GameRoot.cs     # 框架入口
└── Setting/            # 框架配置资产
`

---

## 0.Base — 基类

| 文件 | 说明 |
|------|------|
| SingletonBase.cs | 泛型单例基类（非 MonoBehaviour）。子类需继承 SingletonBase<T> 并提供无参构造，Instance 属性首次访问时按需创建。 |
| SingletonMonoBase.cs | 泛型单例基类（MonoBehaviour）。Awake 时将自身赋值给静态实例，子类需继承 SingletonMonoBase<T>。 |
| ManagerBase.cs | 管理器基类。 |

---

## 1.System — 系统模块

### 0.Pool — 对象池系统

集中管理 C# 对象和 GameObject 的复用，减少频繁创建和销毁的开销。

| 文件 | 说明 |
|------|------|
| PoolSystem.cs | 静态入口，统一对外接口。分 Object 池（按类型）和 GameObject 池（按字符串 key）两套。 |
| ObjectPoolModule.cs | Object 池化管理模块，Dictionary<Type, ObjectPoolData> 按类型隔离。 |
| ObjectPoolData.cs | 单个类型的对象栈（Stack<object>），LIFO。 |
| GameObjectPoolModule.cs | GameObject 池化管理模块，Dictionary<string, GameObjectPoolData> 按 key 隔离。 |
| GameObjectPoolData.cs | 单个 key 的 GameObject 栈，Push 时失活挂到池根节点下，Get 时激活并可选设置父对象。 |

### 1.Event — 事件系统

提供基于字符串 key 的观察者模式事件中心和 UI 指针事件监听组件。

| 文件 | 说明 |
|------|------|
| EventCenter.cs | 事件中心，全局静态类。AddEventListener / RemoveEventListener / TriggerEventListener 支持 0~3 个参数的泛型事件。 |
| LLKEventType.cs | 事件类型枚举，定义 OnClick、OnDrag、OnMouseDown 等事件类型。 |

### 2.Res — 资源加载系统

基于 Addressables 封装，提供同步加载和资源释放接口。

| 文件 | 说明 |
|------|------|
| ResSystem.cs | 静态资源加载接口。LoadAsset 同步加载资源（GameObject 自动 Instantiate 新实例），Release 释放引用计数。 |

### 3.UI — UI 窗口系统

按层级管理 UI 窗口的显示、关闭、缓存与销毁。

| 文件 | 说明 |
|------|------|
| UISystem.cs | UI 窗口管理核心，Show<T>() / Close<T>() 支持 keyName 或泛型类型。按 UI_Layer 层级管理窗口排序与遮罩。 |
| UI_WindowBase.cs | 窗口抽象基类，提供 Init() / Show() / Close() 虚方法。 |
| UIWindowAttribute.cs | 标记窗口类的特性，被 LLKFrameSetting 通过反射自动扫描收集。 |
| UIWindowData.cs | 单个窗口的配置数据（预制体路径、是否缓存、所属层级）。 |
| UI_LoadingSceneWindow.cs | 加载场景窗口，监听 LOAD_SCENE_PROGRESS 事件更新进度条和百分比文本。 |

### 4.Save — 存档系统

基于 MessagePack 的二进制存档系统，支持多存档槽管理。

| 文件 | 说明 |
|------|------|
| SaveSystem.cs | 静态存档接口。SaveFile / DeleteSave / DeleteAllSave 等。内部包含 SaveInfo 和 SaveItem 数据结构，通过 Init() 从持久化路径加载存档信息。 |

### 5.Audio — 音频系统

背景音乐和特效音效的播放与音量控制。

| 文件 | 说明 |
|------|------|
| AudioSystem.cs | 单例音频管理器。PlayBgMusic / PauseBgMusic / ResumeBgMusic / StopBgMusic 控制 BGM。PlayEffectMusic 从对象池获取音源播放特效音（支持 3D 空间音效和自动回收）。支持 BGM / SFX / Global 三级音量独立配置。 |

### 6.Localization — 本地化系统

多语言文本/图片本地化支持。

| 文件 | 说明 |
|------|------|
| LocalizationSystem.cs | 本地化管理器单例，提供 GetContent 获取本地化数据、RegisterLanguageEvent 注册语言变更回调。 |
| LocalizationConfig.cs | 本地化配置资产，二维索引：键名 × 语言类型 → 本地化数据。 |
| LocalizationData.cs | 本地化数据类型：LocalizationTextData（文本）、LocalizationImageData（图片）。 |
| LanguageType.cs | 支持的语言枚举（简体中文 / 繁體中文 / English）。 |
| UILocalizationCollector.cs | UI 本地化收集器，挂载到 UI 根节点上，OnEnable 时自动应用当前语言的本地化配置。先查局部配置再查全局配置，支持 Text / TMP_Text / Image。 |

### 7.Log — 日志系统

便捷的带颜色日志输出工具（仅 Editor 生效）。

| 文件 | 说明 |
|------|------|
| LogSystem.cs | 提供 Green / Red / Yellow / Cyan / White 颜色封装方法，以及 Log / LogWarning / LogError 快捷打印。默认 #if UNITY_EDITOR 保护，出包后颜色标签自动去除。 |

---

## 2.Component — 组件

### 0.Event — 事件监听组件

| 文件 | 说明 |
|------|------|
| LLKEventListener.cs | 实现 IMouseEvent 接口（继承 IPointer / IDrag 等 9 个接口），统一管理指针事件的分发。使用对象池管理事件数据。 |
| LLKEventListenerExtension.cs | 扩展方法，AddEventListener<T>() / RemoveEventListener<T>()，方便对任意 Component 一键注册事件（自动添加 LLKEventListener 组件）。 |

### 1.Animation — 动画系统

基于 Playables API 的动画控制组件，支持单段动画和混合动画播放。

| 文件 | 说明 |
|------|------|
| AnimationController.cs | 动画控制器组件。Init() 创建 PlayableGraph。PlaySingleAnimation 播放单段动画（支持过渡），PlayBlendAnimation 播放混合动画。集成了对象池复用 AnimationNode。 |
| AnimationNodeBase.cs | 动画节点抽象基类，定义 SetWeight / SetSpeed / Destroy 接口，Destroy 自动回收至对象池。 |
| SingleAnimationNode.cs | 单段动画节点，内部持有 AnimationClipPlayable。 |
| BlendAnimationNode.cs | 混合动画节点，内部使用 AnimationMixerPlayable 混合多个 clip。 |

---

## 3.Extension — 扩展方法

| 文件 | 说明 |
|------|------|
| LLKFrameExtension.cs | 框架工具扩展。特性反射便捷方法（委托 Sirenix.Utilities）、对象池快捷方法（PushObject / PushGameObject）。 |

---

## 4.Setting — 配置

| 文件 | 说明 |
|------|------|
| ConfigBase.cs | 配置基类。 |
| LLKFrameSetting.cs | 框架全局配置资产。通过反射扫描所有 UIWindowAttribute 自动收集窗口信息到 uiWindowDataDic。 |

---

## GameRoot.cs — 框架入口

单例入口，Awake 时依次初始化 PoolSystem → EventCenter → UISystem。持有 FrameSetting、UISystem 等核心模块引用。DontDestroyOnLoad 保证全局唯一。
