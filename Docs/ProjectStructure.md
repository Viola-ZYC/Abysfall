# 无尽深渊 - 项目结构与脚本说明

更新时间：2026-03-23

## 一、目录结构（`Assets/Game`）
- `Art`：美术资源（Sprite、贴图、动画相关资源）
- `Audio`：音效与音乐
- `Materials`：材质资源
- `Prefabs`：玩家、敌人、障碍、赛道段、宝箱等预制体
- `Configs`：ScriptableObject 配置与实例
- `Abilities`：能力定义与效果资源
- `Physics`：物理材质
- `Resources/UI`：UI Toolkit 的 UXML/USS/PanelSettings
- `Scripts`：运行时代码
- `Editor`：编辑器辅助工具（动画构建器等）

## 二、脚本结构（`Assets/Game/Scripts`）

### 1) Core
- `GameState.cs`：状态枚举（Menu/Running/Paused/GameOver）
- `GameManager.cs`：全局流程控制、场景切换、状态广播

### 2) Systems
- `InputRouter.cs`：输入路由（键盘/触屏）
- `ScoreManager.cs`：计分与分数事件
- `ObjectPool.cs` / `Poolable.cs`：对象池复用
- `HitStopper.cs`：命中停顿
- `RunProgressStore.cs`：本地存档（`run_progress.json`）
- `ChestMilestoneSpawner.cs`：宝箱与里程碑收藏刷新
- `InfiniteVerticalTilemap.cs`：竖向背景循环与回收
- `Abilities/*`：能力定义、效果、管理器

### 3) Gameplay
- `RunnerController.cs`：玩家移动、碰撞、伤害与反冲
- `PlayerAnimatorStateDriver.cs`：根据速度驱动动画状态（`IsJumping/IsFalling`）
- `Enemy.cs`：敌人行为与回收
- `Obstacle.cs` / `MovingObstacle.cs`：障碍逻辑
- `Collectible.cs`：收藏物触发与记账
- `AbilityChest.cs`：宝箱触发能力选择

### 4) Track
- `TrackManager.cs`：赛道段拼接、生成、回收
- `TrackSegment.cs`：段信息与拼接点
- `SegmentContent.cs`：段内敌人/障碍/收藏/宝箱生成

### 5) UI
- `MainMenuSceneController.cs`：主菜单多页面与设置逻辑
- `MainMenuSceneBootstrap.cs`：主菜单场景初始化
- `HUDController.cs`：游戏内HUD、暂停/结算面板、角色选择
- `AbilitySelectionUI.cs`：能力三选一 UI
- `AbilityAcquiredUI.cs`：首次获得能力/收藏品弹窗

### 6) Characters
- `CharacterDefinition.cs`：角色定义资源
- `CharacterManager.cs`：角色配置与切换
- `CharacterAbilityController.cs`：角色能力执行（含空跳能力）

## 三、关键资源映射
- 主菜单场景：`Assets/Scenes/MainMenuScene.unity`
- 游戏场景：`Assets/Scenes/SampleScene.unity`
- 主菜单 UI：`Assets/Game/Resources/UI/MainMenuUI.uxml` / `.uss`
- 游戏 HUD UI：`Assets/Game/Resources/UI/GameUI.uxml` / `.uss`
- 玩家参数：`Assets/Game/Configs/Runner Config.asset`
- 赛道参数：`Assets/Game/Configs/Track Config.asset`

## 四、当前维护约定
- 优先通过配置与预制体扩展内容，尽量减少对核心循环代码的侵入式改动。
- 所有新增玩法改动都要回归：开始 -> 游玩 -> 结算 -> 重开 -> 返回主菜单。
- 文档与代码保持同一时间口径，避免中期材料与工程状态不一致。
