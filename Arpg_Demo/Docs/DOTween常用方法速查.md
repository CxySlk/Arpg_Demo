# DOTween 常用方法速查表

> 适用:DOTween(Demigiant),常见版本 1.2.x。
> 补间(Tween)基本结构:`目标.DO方法(目标值, 时长).链式配置().回调()`,最后可用变量持有以便控制/销毁。

---

## 一、常用补间方法(做什么)

| 方法 | 作用 | 参数说明 | 示例 |
|---|---|---|---|
| `transform.DOMove(end, duration)` | 移动到目标**世界坐标** | `end`:Vector3 或 float(三轴统一);`duration`:秒 | `DOMove(new Vector3(0,1,0), 1f)` |
| `transform.DOMoveX/Y/Z(end, duration)` | 单轴世界移动 | `end`:float 目标轴坐标 | `DOMoveY(1f, 0.5f)` |
| `transform.DOLocalMove(end, duration)` | 移动到目标**本地坐标** | 同上(相对父物体) | `DOLocalMove(Vector3.up * 2, 1f)` |
| `transform.DORotate(euler, duration)` | 旋转到目标欧拉角 | `euler`:Vector3(度) | `DORotate(new Vector3(0,90,0), 1f)` |
| `transform.DOLocalRotate(euler, duration)` | 本地欧拉角旋转 | 同上 | `DOLocalRotate(Vector3.forward * 90, 0.3f)` |
| `transform.DOScale(end, duration)` | 缩放到目标大小 | `end`:float 或 Vector3(三轴统一/独立) | `DOScale(1.1f, 0.15f)` |
| `transform.DOPunchScale(punch, duration, vibrato, elasticity)` | 冲击缩放(抖一下回弹) | `punch`:冲击幅度 Vector3;`vibrato`:振动次数;`elasticity`:回弹程度 0~1 | `DOPunchScale(Vector3.one*0.2f, 0.3f)` |
| `transform.DOShakePosition(duration, strength, vibrato, randomness, fadeOut)` | 位置抖动(受击/震动) | `strength`:float 或 Vector3;`vibrato`:振动次数;`randomness`:随机度;`fadeOut`:是否衰减 | `DOShakePosition(0.5f, 0.5f)` |
| `transform.DOShakeRotation(...)` / `DOShakeScale(...)` | 旋转/缩放抖动 | 同 DOShakePosition | `DOShakeScale(0.3f, 0.3f)` |
| `graphic.DOFade(end, duration)` | 透明度渐变(Image/Text 等 Graphic、CanvasGroup) | `end`:目标 alpha 0~1 | `img.DOFade(0f, 0.5f)` |
| `graphic.DOColor(color, duration)` | 颜色渐变(Graphic/Material/SpriteRenderer) | `color`:目标 Color | `img.DOColor(Color.red, 0.3f)` |
| `rectTransform.DOAnchorPos(pos, duration)` | UI 锚点位置移动(RectTransform) | `pos`:Vector2 目标锚点位置 | `rect.DOAnchorPos(new Vector2(0,0), 0.5f)` |
| `rectTransform.DOSizeDelta(size, duration)` | UI 尺寸变化(RectTransform) | `size`:Vector2 | `rect.DOSizeDelta(new Vector2(200,200), 0.3f)` |
| `DOTween.To(getter, setter, end, duration)` | **万能数值补间**(任意字段/属性) | `getter`:取当前值委托;`setter`:设值委托;`end`:目标值 | `DOTween.To(()=>value, v=>value=v, 100, 1f)` |

> 补间默认作用在**本地**变换(localScale/localPosition);数值补间一律是**绝对值**,想增量用 `.SetRelative()`。

---

## 二、链式配置方法(怎么动)

| 方法 | 作用 | 参数说明 |
|---|---|---|
| `.SetEase(Ease.xxx)` | 设置缓动曲线 | `Ease.OutBack`(回弹)、`OutQuad/OutCubic`(减速)、`InOutSine`(柔和)、`Linear`(匀速) |
| `.SetLoops(loops, loopType)` | 设置循环 | `loops`:次数(-1=无限);`loopType`:`LoopType.Restart`(重头)/`Yoyo`(往返)/`Incremental`(每次增量) |
| `.SetDelay(seconds)` | 延迟开始 | `seconds`:秒 |
| `.SetUpdate(updateType)` 或 `.SetUpdate(true)` | 设置时间类型 | `UpdateType.Normal`(受 timescale)/`Late`/`Fixed`;`true`= 不受 Time.timeScale 影响(暂停也动) |
| `.SetRelative(isRelative)` | 改为相对值(增量) | `isRelative`:默认 true;在目标值基础上加 |
| `.SetSpeedBased()` | 按速度解释时长 | duration 变为"单位/秒",适合固定速度移动 |
| `.SetAutoKill(autoKill)` | 完成后是否自动销毁 | 默认 true;设为 false 可复用该 Tween |
| `.SetId(id)` | 打标签 | 配合 `DOTween.Kill(id)` 批量控制 |
| `.From()` / `.From(value)` | 从指定值开始(常做入场动画) | 如从透明/缩小状态出现 |
| `.SetOptions(...)` | 各类型特有选项 | 如移动的 `snapping`(像素对齐)、`axisConstraint`(锁定轴) |

---

## 三、播放控制方法(实例)

| 方法/属性 | 作用 |
|---|---|
| `tween.Play()` / `tween.PlayForward()` / `tween.PlayBackwards()` | 播放 / 正向播放 / 反向播放 |
| `tween.Pause()` / `tween.togglePause()` | 暂停 / 切换暂停 |
| `tween.Restart()` | 从头重新播放 |
| `tween.Rewind()` | 回到起点(不播放) |
| `tween.Complete()` | 立即跳到最后并完成 |
| `tween.Kill()` | 销毁补间(常用:对象销毁/重复触发前清理) |
| `tween.Flip()` | 正反切换 |
| `tween.Goto(time, andPlay)` | 跳到指定时间点 |
| `tween.timeScale` | 播放速度倍率(0=停,1=正常,2=两倍) |
| `tween.IsActive()` / `tween.IsPlaying()` / `tween.IsComplete()` | 状态查询 |

---

## 四、回调方法(什么时候触发)

| 方法 | 作用 |
|---|---|
| `.OnStart(Action)` | 开始播放时 |
| `.OnPlay(Action)` | 每次开始播放时 |
| `.OnUpdate(Action)` | 每一帧更新时 |
| `.OnComplete(Action)` | 播放完成时(最常用) |
| `.OnKill(Action)` | 被销毁时 |
| `.OnPause(Action)` / `.OnRewind(Action)` / `.OnStepComplete(Action)` | 暂停 / 回退 / 每步(循环)完成时 |

---

## 五、静态管理方法(DOTween 类)

| 方法 | 作用 |
|---|---|
| `DOTween.KillAll(complete)` | 销毁所有补间(`complete`=true 先完成再销毁) |
| `DOTween.PauseAll()` / `PlayAll()` / `RewindAll()` / `CompleteAll()` | 批量暂停/播放/回退/完成 |
| `DOTween.Kill(id)` | 按 `.SetId()` 的标签销毁一组补间 |
| `DOTween.To(...)` | 万能数值补间(见上表) |
| `DOTween.Sequence()` | 创建动画序列 |
| `DOTween.Init(...)` | 手动初始化(一般自动) |

### 序列 Sequence 常用方法

| 方法 | 作用 |
|---|---|
| `sequence.Append(tween)` | 顺序拼接(前一个完成后播下一个) |
| `sequence.Join(tween)` | 同时加入(和上一个一起播) |
| `sequence.AppendInterval(seconds)` | 顺序间隔 |
| `sequence.AppendCallback(Action)` | 顺序回调 |
| `sequence.Prepend(tween)` / `PrependInterval(...)` / `PrependCallback(...)` | 插到序列开头 |

---

## 六、组合示例

```csharp
// 1. 悬停放大(回弹) + 不受暂停影响(UI_BagSkillSlot 的用法)
transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);

// 2. 入场动画:从透明+缩小出现
img.DOFade(1f, 0.3f);
rectTransform.DOScale(1f, 0.3f).From(0.5f).SetEase(Ease.OutBack);

// 3. 受击抖动 + 完成后恢复
transform.DOShakePosition(0.4f, 0.4f).OnComplete(() => Debug.Log("抖动结束"));

// 4. 循环闪烁(透明度 Yoyo)
img.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);

// 5. 序列:上移 → 停顿 → 消失 → 销毁物体
Sequence seq = DOTween.Sequence();
seq.Append(rect.DOAnchorPos(new Vector2(0, 100), 0.5f).SetEase(Ease.OutQuad));
seq.AppendInterval(0.3f);
seq.Append(img.DOFade(0f, 0.3f));
seq.AppendCallback(() => Destroy(gameObject));

// 6. 批量控制:给 UI 加标签,一次性销毁
tween.SetId("BagSlotTween");
DOTween.Kill("BagSlotTween");
```

---

## 七、常见坑

1. **重复触发不清理**:同一目标连续开同类型补间会叠加,先 `Kill()` 旧 Tween 再开新的;
2. **对象销毁后补间报错**:在 `OnDestroy` 里 `Kill()`;或用 `.SetAutoKill(true)`(默认);
3. **暂停时动画失效**:UI 动画建议 `.SetUpdate(true)`;
4. **DOScale 是绝对值**:当前值已等于目标值时视觉上不动;
5. **DOTween Pro 差异**:部分方法(如某些 UI/路径高级功能)在 Pro 版才有,本表均为免费版常用 API。
