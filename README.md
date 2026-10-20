# RebornFGUI

`com.reborn.fgui` — Reborn 框架的 **UI 管理层**，在 FairyGUI 之上提供面板生命周期、
层级栈导航、包引用计数、多屏分屏与全局控制器等能力，让业务代码只关心面板本身。

核心是 `FGUIPanelManager` 与 `FguiPanelBase` 这套面板管理体系；
底层 UI 运行时基于 FairyGUI-unity 5.2.0，并为分屏与多点触控做了必要改动。

---

## ⚠️ 基于 FairyGUI-unity 5.2.0 · MIT License

> 本包的 UI 运行时源自 **[FairyGUI-unity 5.2.0](https://github.com/fairygui/FairyGUI-unity/releases/tag/5.2.0)**（2025-05-11 发布）。
>
> **FairyGUI 版权归原作者所有：`Copyright (c) 2015 fairygui.com`，许可为 `MIT License`。**
>
> | 项 | 值 |
> |---|---|
> | 上游项目 | [fairygui/FairyGUI-unity](https://github.com/fairygui/FairyGUI-unity) |
> | 上游版本 | **5.2.0** |
> | 上游版权 | Copyright (c) 2015 fairygui.com |
> | 上游许可 | [MIT](https://github.com/fairygui/FairyGUI-unity/blob/master/LICENSE) |
> | 本包许可 | MIT，见 [LICENSE.md](LICENSE.md) |
> | 完整声明 | [Third Party Notices.md](Third%20Party%20Notices.md) |
>
> `Runtime/`、`Editor/`、`Resources/Shaders/` 下共 **167 个源文件**（`.cs` + `.shader`），
> 其中 **156 个与官方 5.2.0 逐字节一致**、**9 个为适配分屏与多点触控而修改**、
> **2 个为 Reborn 新增**（面板管理层）。改动清单见 [第 4 节](#4-对-fairygui-运行时的改动)。
>
> 官方 `Extensions`（TextMeshPro / Spine / DragonBones / WebGLTextInput）**未包含**在本包中，
> 以避免强制依赖 TextMeshPro。需要时请从官方 5.2.0 自行合并并追加 asmdef 引用。

---

## 1. 安装

Unity Package Manager → *Add package from git URL*：

```
git@github.com:LinZhiWeiNB/RebornFrameFGUI.git
```

或在 `Packages/manifest.json` 中添加：

```json
"com.reborn.fgui": "git+ssh://git@github.com/LinZhiWeiNB/RebornFrameFGUI.git#v1.0.0"
```

### 环境要求

| 项 | 要求 |
|---|---|
| Unity | **2021.3** 及以上（已在 2021.3 / 2022 / 6000 验证） |
| 依赖 | `com.reborn.utils` `1.0.0`、`com.reborn.reborncore` `1.0.0`、`com.unity.inputsystem` |

> **两个 Reborn 依赖都是硬依赖**：
> 面板管理依赖 `RebornFrame.Core.AppInstance`（Update 驱动）与 `ArrayListT`；
> 触控输入依赖 `com.reborn.utils` 的 `MultiTouchScreenUtils`，分屏依赖 `RebornFrame.Utils.Singleton`。

### 程序集

| asmdef | 平台 | 内容 |
|---|---|---|
| `reborn.fgui` | 全平台 | `Runtime/` |
| `reborn.fgui.Editor` | 仅 Editor | `Editor/` |

---

## 2. Reborn 面板管理（核心能力）

业务侧只需继承 `FguiPanelBase` 写面板逻辑，其余交给 `FGUIPanelManager`：
包的加载与卸载、UI 层级、面板栈、分屏尺寸、相机绑定，全部自动处理。

### 2.1 一个最小面板

```csharp
public class MainPanel : FguiPanelBase
{
    // 指定 FGUI 包名与组件名，ShowPanel 时可省略参数
    public override string GetPackageName() => "Main";
    public override string GetResName()     => "MainPanel";

    public override void Start(object[] data)
    {
        // 面板已创建，FGUIComponent 可用。取 ShowPanel 传入的 data
        FGUIComponent.GetChild("btnStart").onClick.Add(OnStart);
    }

    public override void OnShow()  { }   // 显示（含从栈中恢复显示）
    public override void OnHide()  { }   // 被新面板遮挡而隐藏
    public override void OnClose() { }   // 关闭前清理
}
```

```csharp
// 包名与组件名由 GetPackageName / GetResName 提供，这里传 null 即可
FGUIPanelManager.Instance.ShowPanel<MainPanel>(null, null);
```

### 2.2 面板生命周期

```
ShowPanel<T>()
  ├─ LoadPackage(packageName)          包引用计数 +1，递归加载依赖包
  ├─ 按 orderIndex 取/建 UI 根节点      层级隔离
  ├─ UIPackage.CreateObject(...)       创建 GComponent
  ├─ SetContentScaleFactor(...)        按面板设计尺寸自适应
  ├─ layer = 5 + DisplayID             分屏命中测试用
  ├─ AddRelation(GRoot, Size)          跟随根节点缩放
  ├─ panel.Start(data) ────────────────► 业务初始化
  ├─ ChangePanelSizeByCurrentSpliteMode 应用当前分屏尺寸
  └─ panel.OnShow() ───────────────────► 业务显示逻辑

Back() / Close()
  ├─ panel.BaseClose() → OnClose() ────► 业务清理
  ├─ FGUIComponent.Dispose()
  └─ RemovePackage(packageName)        引用计数 -1，归零则卸载包与依赖
```

`Update` / `LateUpdate` 默认**不**调用，按需开启，避免空回调开销：

```csharp
public override void Start(object[] data)
{
    NeedUpdate = true;      // 开始每帧回调 Update()
    NeedLateUpdate = true;  // 开始每帧回调 LateUpdate()
}

public override void Update() { /* 每帧 */ }
```

> 开启后的第一帧会被跳过（`UpdateThisFrame` 门控），
> 避免面板创建的当帧就执行 `Update` 导致状态未就绪。

### 2.3 显示方式 `ShowTypeEnum`

| 值 | 行为 |
|---|---|
| `HideCurrent`（默认） | 隐藏当前面板并压入栈，`Back()` 可返回 |
| `OverCurrent` | 叠加显示，当前面板保持可见 |
| `CloseCurrent` | 关闭当前面板再显示新面板 |

```csharp
// 弹窗：盖在当前面板之上，不隐藏它
FGUIPanelManager.Instance.ShowPanel<ConfirmDialog>(null, null,
    data: new object[] { "确定要退出吗？" },
    showType: ShowTypeEnum.OverCurrent);
```

### 2.4 面板栈与返回

`HideCurrent` 方式打开的面板会自动入栈，`Back()` 关闭当前面板并恢复上一个：

```csharp
FGUIPanelManager.Instance.Back();              // 主屏返回
FGUIPanelManager.Instance.Back(DisplayID: 2);  // 分屏：玩家 2 返回
```

栈内已被 `Close` 掉的面板会在返回时自动跳过，不会恢复到已销毁的面板上。

> `Back()` 是安全的返回方式。`Close(panel)` / `Close(包名, 组件名)` 会绕过栈，
> 关闭当前面板时请优先用 `Back()`；`Close(包名, 组件名)` 遇到重名会误关。

### 2.5 UI 层级 `OrderIndex`

每个 `orderIndex` 对应 `GRoot` 下一个独立的根节点，按序号自动排序插入。
不同层之间互不干扰，`Normal` 层的面板栈不会影响常驻的 HUD 或顶层提示。

| 常量 | 值 | 典型用途 |
|---|---:|---|
| `OrderIndex.BackGround` | 0 | 背景层 |
| `OrderIndex.Normal` | 1 | 普通面板（**仅此层参与面板栈与 `ShowTypeEnum`**） |
| `OrderIndex.Front` | 2 | HUD |
| `OrderIndex.MostFront` | 3 | 弹窗 |
| `OrderIndex.MostMostFront` | 4 | 飘字 / 引导 |
| `OrderIndex.MostMostMostFront` | 5 | Loading / 断线重连 |

```csharp
// 常驻 HUD：不入栈，不被普通面板隐藏
FGUIPanelManager.Instance.ShowPanel<HudPanel>(null, null,
    orderIndex: OrderIndex.Front);
```

### 2.6 包的自动加载与引用计数

包的生命周期由管理器托管，业务侧不需要手动 `AddPackage` / `RemovePackage`：

- `ShowPanel` 自动加载包，并**递归加载依赖包**（`package.dependencies`）。
- 同一个包被多个面板使用时引用计数累加，最后一个面板关闭才真正卸载。
- 卸载时同样递归释放依赖包。

加载路径按编译宏切换：

| 宏 | 加载方式 |
|---|---|
| 未定义 `USE_AB` | `Resources`：`UI/{包名}/{包名}` |
| `USE_AB` + Editor 且未定义 `SIMULATE_PHONE` | `AssetDatabase`：`Assets/Arts/UI/{包名}/{包名}` |
| `USE_AB` 真机 | `ABPackageManager.Instance.LoadAssetBundle("arts_ui_{包名}")` |

也可以手动预加载常驻包，避免反复加载卸载：

```csharp
FGUIPanelManager.Instance.LoadPackage("Common", times: 1);
```

### 2.7 分屏与多显示器

支持同机多人分屏，最多四块。每块屏幕有独立的面板列表、当前面板与面板栈，
`DisplayID` 即玩家索引（`0` 为公共层）。

```csharp
FGUIPanelManager.Instance.ChangeSplitMode(SplitModeEnum.Quadruple);

// 为玩家 2 在自己那块屏上开面板
FGUIPanelManager.Instance.ShowPanel<PlayerPanel>(null, null, DisplayID: 2);
```

| `SplitModeEnum` | 布局 |
|---|---|
| `Single` | 单屏 |
| `Double` | 左右双屏 |
| `VerticalDouble` | 上下双屏 |
| `UpTriple` | 上方全屏 + 下方左右两屏 |
| `DownTriple` | 上方左右两屏 + 下方全屏 |
| `Quadruple` | 四分屏 |

切换分屏时所有面板尺寸自动重算；屏幕方向变化（`Screen.orientation`）也会自动重算。
需要无视分屏、始终铺满整屏的面板（如全屏遮罩、Loading）设置：

```csharp
panel.KeepFullScrenn = true;   // 注意：字段名拼写以代码为准
```

分屏的输入命中由 `RedirectScreenPosition` 把全局屏幕坐标转换为分屏局部坐标，
并给出命中的分屏索引；面板 `layer = 5 + DisplayID`，运行时据此保证
**玩家只能点到自己那一层的 UI**（`DisplayID = 0` 的公共层所有人可点）。
业务侧拿到的 `evt.x / evt.y` 已经是本屏局部坐标，无需自行换算。

真正的多物理显示器可以绑定独立相机：

```csharp
var cam = FGUIPanelManager.Instance.CreateCamera("Display2Camera", cullingMask);
cam.targetDisplay = 1;
FGUIPanelManager.Instance.ShowPanel<PlayerPanel>(null, null,
    DisplayID: 2, displayCamera: cam);
```

`CreateCamera` 产出正交、`Depth` 清除、`DontDestroyOnLoad` 并挂好 `StageCamera` 的相机；
传入 `displayCamera` 的面板会切到 `RenderMode.ScreenSpaceCamera`。

### 2.8 全局控制器

一次调用把某个控制器的值递归设置到**所有已打开面板及其全部子组件**上，
适合全局皮肤、语言、横竖屏等整体切换。设置后新打开的面板会自动套用当前值。

```csharp
FGUIPanelManager.Instance.SetGlobalController("Language", 1);
```

### 2.9 面板查询与批量关闭

```csharp
var hud  = FGUIPanelManager.Instance.GetPanel<HudPanel>();          // 按类型找
var p2   = FGUIPanelManager.Instance.GetPanel<HudPanel>(2);         // 指定分屏
var any  = FGUIPanelManager.Instance.GetPanel(0, "Main", "MainPanel");
var cur  = FGUIPanelManager.Instance.GetCurrentPanel();             // 当前面板

FGUIPanelManager.Instance.CloseAll(displayID: 2);  // 关掉某块屏的全部面板
FGUIPanelManager.Instance.RefreshSize();           // 按 ScreenSize 重算尺寸
FGUIPanelManager.Instance.Clear();                 // 清空管理器状态（切场景等）
```

### 2.10 API 速查

**`FGUIPanelManager`**

| 成员 | 说明 |
|---|---|
| `ShowPanel<T>(包名, 组件名, data, showType, DisplayID, displayCamera, orderIndex)` | 打开面板，返回面板实例 |
| `Back(DisplayID = 0)` | 关闭当前面板并恢复栈顶，安全的返回方式 |
| `Close(FguiPanelBase)` / `Close(包名, 组件名)` | 直接关闭，绕过面板栈 |
| `CloseAll(displayID)` | 关闭指定屏的所有面板 |
| `GetCurrentPanel(displayId = 0)` | 当前面板 |
| `GetPanel<T>()` / `GetPanel<T>(displayId)` / `GetPanel(displayId, 包名, 组件名)` | 查询面板 |
| `LoadPackage(包名, times = 1)` | 手动加载包（引用计数） |
| `ChangeSplitMode(SplitModeEnum)` | 切换分屏模式并重算所有面板尺寸 |
| `ChangePanelSizeByCurrentSpliteMode(panel)` | 按当前分屏模式重算单个面板尺寸 |
| `RedirectScreenPosition(x, y, reverseY)` / `(Vector2, reverseY)` | 全局坐标 → (分屏索引, 分屏局部坐标) |
| `CreateCamera(name, cullingMask)` | 创建并配置用于多显示器的 `StageCamera` |
| `SetGlobalController(控制器名, 值)` | 递归设置全局控制器 |
| `RefreshSize()` / `Clear()` | 重算尺寸 / 清空状态 |
| `CurrentSplitMode` / `DisplayCount` / `ScreenSize` / `DefaultCameraSize` | 状态与配置 |

**`FguiPanelBase`**

| 成员 | 说明 |
|---|---|
| `Start(object[] data)` | 面板创建完成，业务初始化 |
| `OnShow()` / `OnHide()` / `OnClose()` | 显示 / 被遮挡隐藏 / 关闭前清理 |
| `Update()` / `LateUpdate()` | 需先置 `NeedUpdate` / `NeedLateUpdate` 为 `true` |
| `GetPackageName()` / `GetResName()` | 覆写以提供默认包名与组件名 |
| `FGUIComponent` | 面板根 `GComponent` |
| `DisplayID` | 所属分屏 / 玩家索引 |
| `KeepFullScrenn` | 无视分屏，始终铺满整屏 |
| `isClosed` | 是否已关闭 |

---

## 3. 多点触控互斥

官方实现中一个按钮可以被多根手指同时按下，各自触发一次完整的
`onTouchBegin` / `onClick`。在多人同屏或快速连点时会造成重复响应——
一次扣两份道具、弹两个窗口。`EventDispatcher` 因此新增了互斥开关：

```csharp
// 默认 false：同一目标同一时刻只响应一个触点
someObject.AllowMultipleTouch = true;   // 需要同时响应多指时显式打开
```

默认值 `false` 是**破坏性默认值**：接入自绘画板、虚拟摇杆等确实需要多指的组件时，
若遇到「多指同时操作失效」，先检查这个开关。
捏合（`PinchGesture`）与旋转（`RotationGesture`）手势本身需要双指，已内置放开。

容器（`Container` 及其子类）在 `Init()` 中统一设为 `true`，自身不拦截触点，
保证事件能继续向子节点透传；互斥只作用在最终的事件目标上。

---

## 4. 对 FairyGUI 运行时的改动

以下是本包与官方 5.2.0 的**全部**差异（忽略 CRLF/LF 行尾差异后的实际内容差异）。
其余 156 个文件与官方完全一致，升级官方版本时可直接覆盖。

| 文件 | 改动行数 | 所属改动 |
|---|---:|---|
| `Runtime/Core/Stage.cs` | ~262 | 多点触控互斥、分屏输入、粘滞触控修复、坐标修正 |
| `Runtime/Core/Container.cs` | ~41 | 分屏命中测试、坐标修正、多点触控互斥 |
| `Runtime/Event/EventDispatcher.cs` | 5 | `AllowMultipleTouch` |
| `Runtime/Core/HitTest/HitTestContext.cs` | 2 | 分屏命中上下文 |
| `Runtime/Gesture/PinchGesture.cs` | 2 | 手势放开多点触控 |
| `Runtime/Gesture/RotationGesture.cs` | 2 | 手势放开多点触控 |
| `Runtime/Core/DisplayObject.cs` | 1 | 坐标修正 |
| `Runtime/Core/Shape.cs` | 1 | 坐标修正 |
| `Runtime/Core/Text/SelectionShape.cs` | 1 | 坐标修正 |
| `Runtime/Manager/FGUIPanelManager.cs` | 新增 631 行 | 面板管理（第 2 节） |
| `Runtime/Manager/FguiPanelBase.cs` | 新增 127 行 | 面板基类（第 2 节） |

### 4.1 多点触控互斥

**为什么改**：官方没有「单目标互斥」的开关，详见第 3 节。

| 文件 | 改动 |
|---|---|
| `EventDispatcher` | 新增属性 `AllowMultipleTouch`，默认 `false` |
| `Container.Init()` | 设 `true`，容器自身不拦截，保证事件向子节点透传（三个构造重载都经由 `Init`） |
| `PinchGesture` / `RotationGesture` 构造 | 设 `true`，手势本就需要双指 |
| `Stage.HandleCustomInput` / `HandleMouseEvents` / `HandleTouchEvents` | 共 3 处：`touch.Begin()` 前，若目标已出现在其他活跃触点的 `downTargets[0]`，把本次 target 回退为 `Stage` |
| `TouchInfo.End` | 结束时若 target 不允许多点触控且不在 `downTargets` 中，target 回滚到 `Stage.inst` |

### 4.2 分屏 / 多显示器输入

**为什么改**：官方的多显示器支持依赖 `Display.RelativeMouseAt()`，只处理「多个物理显示器」，
无法表达「一块屏幕切成四块、每块属于不同玩家」这种逻辑分屏；
且官方触控路径（`Input.GetTouch` / EnhancedTouch）拿不到触点所属的显示器索引，
多个 Touchscreen 设备的触点会被混在一起。

| 文件 | 改动 |
|---|---|
| `HitTestContext` | 新增 `splitScreenIndex`（命中落在哪个分屏）与 `splitScreenPoint`（分屏局部坐标） |
| `Container.HitTest(stagePoint, forTouch, displayId = 0)` | 新增可选参数 `displayId`；坐标经 `FGUIPanelManager.RedirectScreenPosition()` 重定向；移除官方 `Display.RelativeMouseAt` 分支 |
| `Container.HitTest()`（override） | 用 `gameObject.layer - 5` 推出 `playerIndex`；非单屏模式下 `playerIndex != 0 && splitScreenIndex != playerIndex` 直接返回 `null`；`ScreenSpaceOverlay` 的相机判定改为比对 `displayId`，`WorldSpace` 分支改用分屏坐标（去掉官方用 `WorldToScreenPoint` 查 z 值那段） |
| `Stage.SetCustomInput`（4 个重载） | 新增可选参数 `displayId = 0` |
| `Stage.GetHitTarget` / `HandleTouchEvents` | 改用 `MultiTouchScreenUtils.Instance.GetTouches()`（带回 display 索引），替代 `Input.GetTouch` / EnhancedTouch |
| `Stage`（鼠标路径） | `FAIRYGUI_INPUT_SYSTEM` 下从 `mouse.displayIndex` 取 displayId |
| `TouchInfo.UpdateEvent` | 事件坐标经 `RedirectScreenPosition` 转为分屏局部坐标 |
| `Stage.TouchDisplayID` / `SetCustomTouchMode` / `ClearCustomTouchMode` | 预留接口，用于把模拟触摸固定到指定屏；**当前只写不读，尚未接线** |

**代价**：触控路径不再走官方 `Input.GetTouch` / EnhancedTouch，固定走 `MultiTouchScreenUtils`。
这是为拿到 display 索引做的取舍，官方后续触控相关修复需人工 review 后合入。

### 4.3 粘滞触控修复

**为什么改**：官方 `HandleTouchEvents` 按「当前设备上报的触点」循环。
一旦某个触点因设备断开、应用切出、分屏切换等原因**没有上报 Ended/Canceled
就从列表里消失**，`_touches[]` 对应槽位永远不会被回收——`touchId` 停留在旧值、
`_touchCount` 不减，表现为「有一根看不见的手指一直按着」，后续触控错位甚至完全失效。

**怎么改**：循环方向反转，改为遍历 `_touches[0..4]` 五个槽位，再去设备列表找对应触点。
找不到时若该槽位已 `began`，补发 `End()` 与 `HandleRollOver()` 并递减 `_touchCount`，
最后把 `touchId` 置回 `-1` 释放槽位。这也是 `Stage.cs` 差异行数偏大的主要原因之一。

### 4.4 点击坐标修正 `WidthFix` / `HeightFix`

**为什么改**：特定布局（分屏缩放、带偏移的 `UIPainter`、非标准 `UIContentScaler` 配置）下，
HitTest 的本地坐标与实际渲染位置存在固定偏移，表现为「点击落点偏了一段」。
与其逐个场景调相机和 Root，不如开一个全局补偿量，由宿主初始化时标定一次。

`Stage` 新增 `public float WidthFix` / `HeightFix`（默认 `0`，不设置时与官方行为完全一致），
在 `Container.HitTest_Container`、`DisplayObject.HitTest`、`Shape.HitTest`、
`SelectionShape.HitTest` 四处叠加到本地坐标上。

---

## 5. 已知取舍与后续方向

1. **触控路径与官方分叉**。触摸始终走 `MultiTouchScreenUtils` 而非官方 EnhancedTouch，
   这是分屏下拿 display 索引的必要代价。若官方后续完善逻辑分屏支持，可评估收敛。
2. **`Stage.TouchDisplayID` 与 `SetCustomTouchMode` / `ClearCustomTouchMode` 尚未接线**，
   当前调用不产生效果，属预留接口。
3. **`AllowMultipleTouch` 默认 `false` 是破坏性默认值**，接入新组件时留意。
4. **Extensions 未包含**。若要用 TMP / Spine / DragonBones Loader，
   需从官方 5.2.0 的 `Assets/Scripts/Extensions` 单独合并并追加 asmdef 引用。
5. **升级官方版本的流程**：保留 `Runtime/Manager/`、asmdef 与 `package.json`，
   其余目录用官方新版覆盖，再把第 4 节的改动逐条重打。

---

## 6. 许可

本包以 **MIT** 许可发布，见 [LICENSE.md](LICENSE.md)。

其中源自 FairyGUI-unity 的部分，版权归 **fairygui.com** 所有
（`Copyright (c) 2015 fairygui.com`，MIT License），
完整声明见 [Third Party Notices.md](Third%20Party%20Notices.md)。

使用、分发本包时请一并保留上述两份声明。

---

任何使用问题请联系 **林志威**。
