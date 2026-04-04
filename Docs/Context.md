# 无尽深渊 - 项目上下文（持续更新）

更新时间：2026-03-23

## 项目概览
- Unity 版本：6000.3.0f1
- Unity Editor 路径：`E:\AppData\Unity\Unity 6000.3.0f1\Editor`
- 类型：2D 纵向下坠跑酷（竖屏）
- 核心循环：重力递增 -> 下坠前进 -> 触敌刹车反冲 -> 障碍扣血 -> 失败/重开
- 操作：仅左右移动（无主动攻击）

## 当前场景结构（最新）
- 主菜单场景：`Assets/Scenes/MainMenuScene.unity`
  - 作为纯主页场景使用，不显示游戏内 HUD/能力 UI。
  - 当前交互：任意键/点击/触摸进入游戏场景；`Esc` 退出游戏。
  - 游戏运行对象（Player/Track/Score/Ability/UI 等）在该场景中已关闭，不参与运行。
- 游戏场景：`Assets/Scenes/SampleScene.unity`
  - 作为纯游戏场景使用，`initialState` 已设置为 `Running`（进入后直接开局）。
- Build Settings 首场景已改为 `MainMenuScene`，`SampleScene` 排在其后。

## 关键已实现功能
- 玩家控制（`RunnerController`）
  - 重力递增、最大下落速度、横向移动
  - 下坠触敌自动刹车 + 反冲
  - 障碍扣血、减速并消失
  - 高速穿透保护：Continuous + Cast 扫掠
- 游戏流程（`GameManager`）
  - 支持 `Menu / Running / Paused / GameOver`
  - 支持主菜单场景与游戏场景切换
  - 提供统一 `QuitGame` 退出入口
- 暂停菜单（`HUDController` + `GameUI.uxml`）
  - 已新增：`Return to Main Menu`、`Exit Game`
  - 保留：`Continue Game`
- 主菜单多页面（`MainMenuSceneController` + `MainMenuUI`）
  - 已接入：`Leaderboard / Collection / Achievements / Settings`
  - 各页面均支持 `Back to Main Interface` 和 `Close`
  - `Esc` 关闭当前打开页面（兼容 Input System）
- 肉鸽系统
  - `AbilityDefinition + AbilityEffect + AbilityManager`
  - 宝箱触发暂停并 3 选 1
  - 当前能力池为 3 个（Speed Boost / Lightweight / Tough Skin）
- 宝箱刷新
  - `ChestMilestoneSpawner` 按分数里程碑刷新（默认每 100 分）
- 收藏系统（里程碑固定物品）
  - 固定深度刷新收藏物（120/280/450/650/900/1200）
  - 已停用随机黄色颗粒式收集物
  - 收藏页显示解锁状态、累计数量、文本介绍
- 本地存档（`RunProgressStore`）
  - 统一写入 `run_progress.json`
  - 记录分数、局数、排行榜、模式解锁、收藏解锁与收藏数量

## 需求与约束（已确认）
- 不保留主动攻击；下坠触敌触发自动刹车反冲
- 玩家保持垂直，碰撞不旋转
- 相机仅跟随 Y 轴
- 左右边界清晰可见并可阻挡
- 障碍需要减速效果并在碰撞后消失
- 宝箱触发能力选择：触碰后暂停时间，选择后恢复，触碰瞬间速度归零

## 当前状态总结
- 核心玩法闭环可运行，且已完成“主菜单独立场景化”。
- 主菜单页面功能已从占位升级为可用状态（排行榜、收藏、成就、设置）。
- 现阶段主要短板为内容量与视觉打磨，不再是流程阻断问题。

## 关键文件（入口）
- 场景流程：`Assets/Game/Scripts/Core/GameManager.cs`
- 主游戏 UI 控制：`Assets/Game/Scripts/UI/HUDController.cs`
- 游戏 UI 资源：`Assets/Game/Resources/UI/GameUI.uxml`
- 玩家逻辑：`Assets/Game/Scripts/Gameplay/RunnerController.cs`
- 赛道生成：`Assets/Game/Scripts/Track/TrackManager.cs`
- 赛道内容：`Assets/Game/Scripts/Track/SegmentContent.cs`
- 肉鸽系统：`Assets/Game/Scripts/Systems/Abilities/*`
- 宝箱里程碑：`Assets/Game/Scripts/Systems/ChestMilestoneSpawner.cs`
