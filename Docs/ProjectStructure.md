# 无尽深渊 - 项目结构与脚本说明

本文用于梳理 `Assets/Game` 目录结构与脚本职责，便于快速理解与接手维护。

## 目录结构（Assets/Game）
- Art：美术资源（Sprite、Texture、动画、图集等）。
- Audio：音效与音乐资源。
- Materials：材质资源（2D/URP 用材质）。
- Prefabs：可复用预制体（Player、敌人、障碍、段落等）。
- Configs：ScriptableObject 配置与其实例资源（核心参数集中管理）。
- Abilities：肉鸽能力资源（AbilityDefinition + Effects）。
- Physics：物理材质资源（如 NoFriction）。
- Scripts：所有运行时代码。
- README.md：快速搭建清单（Quick Setup）。
- Context.md：项目目标与核心循环摘要。
- NextSteps.md：当前阶段的搭建清单与验证项。

## Configs（配置资源）
- `Assets/Game/Configs/RunnerConfig.cs`  
  定义角色控制参数：生命值上限、横向速度、重力递增、下落上限、刹车冲量与命中停顿等。
- `Assets/Game/Configs/TrackConfig.cs`  
  定义赛道段参数：段预制体列表、初始段数、回收/提前生成距离。
- `Assets/Game/Configs/Runner Config.asset`  
  RunnerConfig 实例资源（当前内容与 `RunnerConfig` 脚本匹配）。
- `Assets/Game/Configs/Track Config.asset`  
  赛道配置实例（需要在完成 TrackSegment 预制体后填写 `segmentPrefabs`）。

## Scripts（代码结构）

### Core（流程与状态）
- `Assets/Game/Scripts/Core/GameState.cs`  
  游戏状态枚举：Boot / Menu / Running / Paused / GameOver。
- `Assets/Game/Scripts/Core/GameManager.cs`  
  游戏主流程管理器：启动、开始运行、暂停/恢复、GameOver；负责驱动 Runner/Track/Score 的重置。

### Systems（通用系统）
- `Assets/Game/Scripts/Systems/InputRouter.cs`  
  输入路由：支持新输入系统（Input System）和键盘回退；实现横向滑动/按键输入。
- `Assets/Game/Scripts/Systems/ScoreManager.cs`  
  计分系统：按下落距离与收集物计算分数，触发 ScoreChanged 事件。
- `Assets/Game/Scripts/Systems/ObjectPool.cs`  
  简单对象池：按 prefab 回收与复用实例，支持单例模式。
- `Assets/Game/Scripts/Systems/Poolable.cs`  
  统一对象池生命周期接口（OnSpawned/OnDespawned）。
- `Assets/Game/Scripts/Systems/HitStopper.cs`  
  命中停顿控制：通过时间缩放实现短暂停顿效果。
- `Assets/Game/Scripts/Systems/Abilities/*`  
  肉鸽能力系统：能力定义、效果与管理器（权重抽取、叠加与扩展）。

### Gameplay（玩家、敌人、交互）
- `Assets/Game/Scripts/Gameplay/RunnerController.cs`  
  玩家控制器：重力递增、横向移动、下坠触碰敌人自动刹车反冲；障碍扣血直至 GameOver。
- `Assets/Game/Scripts/Gameplay/Enemy.cs`  
  敌人逻辑：被玩家触碰（下坠）后回收进对象池；继承 Poolable。
- `Assets/Game/Scripts/Gameplay/Obstacle.cs`  
  障碍标记脚本：与玩家碰撞会扣血、减速并消失（由 RunnerController 驱动）。
- `Assets/Game/Scripts/Gameplay/Collectible.cs`  
  收集物：被玩家触发后加分，并回收到对象池。
- `Assets/Game/Scripts/Gameplay/AbilityChest.cs`  
  宝箱逻辑：触碰后暂停时间并触发 3 选 1 能力选择。

### Track（赛道段生成）
- `Assets/Game/Scripts/Track/TrackManager.cs`  
  赛道管理：根据玩家位置生成/回收段落，支持对象池。
- `Assets/Game/Scripts/Track/TrackSegment.cs`  
  单段结构：记录段长度与 EndPoint 用于拼接与回收判断。
- `Assets/Game/Scripts/Track/SegmentContent.cs`  
  段内容生成：按 spawn 点与概率生成敌人/障碍/宝箱并回收。

### UI（界面）
- `Assets/Game/Scripts/UI/HUDController.cs`  
  HUD 控制：订阅分数/生命值变化与游戏状态，切换菜单/结算面板并刷新显示。
- `Assets/Game/Scripts/UI/AbilitySelectionUI.cs`  
  能力选择 UI：暂停后展示 3 选 1，选择后恢复。

## 场景与其它
- `Assets/Scenes/`：当前项目主场景（如 `SampleScene.unity`）。
- `Assets/Settings/Scenes/`：URP 2D 模板场景（引擎自带模板）。

## 依赖与约束
- 目前核心循环已搭好脚本框架，场景与引用需要按 `NextSteps.md` 完整接线。
- 工程约束：只在 `Assets` 内编辑与新增资源。
