# Changelog

本文件记录 `com.reborn.fgui` 的所有重要变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [1.0.0] - 2026-10-20

首个公开版本。仓库已重新初始化，此前的内部提交历史不再保留。

### 核心能力

Reborn 框架的 UI 管理层：

- `FGUIPanelManager`：面板生命周期管理、`ShowTypeEnum` 三种显示方式
  （关闭当前 / 叠加 / 隐藏当前）、面板栈与 `Back()` 返回、
  `OrderIndex` 六级 UI 层级隔离、FGUI 包的自动加载卸载与引用计数（含递归处理依赖包）、
  分屏模式切换与尺寸自适应、多显示器相机创建、全局控制器递归设置、面板查询与批量关闭。
- `FguiPanelBase`：面板基类，提供 `Start` / `OnShow` / `OnHide` / `OnClose` 生命周期钩子，
  以及按需开启的 `Update` / `LateUpdate`。
- 分屏支持：单屏 / 左右双屏 / 上下双屏 / 上三屏 / 下三屏 / 四屏，
  每块屏独立的面板列表、当前面板与面板栈，玩家只能点到自己所属层的 UI。

### UI 运行时

- FairyGUI-unity **5.2.0**（2025-05-11），MIT License，Copyright (c) 2015 fairygui.com。
- 167 个源文件中 156 个与官方逐字节一致，9 个为适配分屏与多点触控而修改，2 个为 Reborn 新增。
- 未包含官方 `Extensions`（TextMeshPro / Spine / DragonBones / WebGLTextInput），避免强制依赖。

### 对 FairyGUI 运行时的改动

- `EventDispatcher.AllowMultipleTouch`：控制同一目标是否允许多个触点同时响应，默认 `false`。
  捏合与旋转手势内置放开。
- 分屏 / 多显示器输入：`HitTestContext` 新增分屏命中字段；
  `Container.HitTest` 与 `Stage.SetCustomInput` 新增可选 `displayId` 参数；
  触控改走 `MultiTouchScreenUtils.GetTouches()`（来自 `com.reborn.utils`）以获取 display 索引；
  `TouchInfo.UpdateEvent` 将事件坐标转为分屏局部坐标。
- `Stage.WidthFix` / `HeightFix`：HitTest 本地坐标偏移修正，默认 `0`。
- 修复触点从设备列表消失后槽位不释放导致的「粘滞触控」。

### 依赖

- `com.reborn.utils` `1.0.0`、`com.reborn.reborncore` `1.0.0`、`com.unity.inputsystem`。
- Unity 2021.3 及以上。

[1.0.0]: https://github.com/LinZhiWeiNB/RebornFrameFGUI/releases/tag/v1.0.0
