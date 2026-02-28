# 无尽深渊 - 下一步执行清单（目标2：内容量补齐）

更新时间：2026-02-14

## 目标范围
- 在不新增复杂系统的前提下，补齐演示所需内容量。
- 达成项：
  - 至少 2 种敌人行为变体。
  - 至少 2 种障碍变体。
  - 至少 3 套赛道段组合（低压/中压/高压）。

## 执行顺序（建议 2-3 天）

### Step A：敌人内容扩充（优先）
- 产出：
  - `Enemy_Fast.prefab`（更快移动或更高碰撞威胁）。
  - `Enemy_Tank.prefab`（更大体积或更难规避路径）。
- 代码落点：
  - 如需行为差异，在 `Assets/Game/Scripts/Gameplay/Enemy.cs` 基础上新增轻量脚本（如 `EnemyMover`）。
- 场景接线：
  - 将新敌人预制体加入赛道段 `SegmentContent` 的 `enemyPrefabs` 数组。
- 完成定义：
  - 连续 5 局中两种敌人均出现，且对走位策略形成差异。

### Step B：障碍内容扩充
- 产出：
  - `Obstacle_Wide.prefab`（宽平台，压缩可移动空间）。
  - `Obstacle_Narrow.prefab`（窄平台，要求精确走位）。
- 代码落点：
  - 复用 `Assets/Game/Scripts/Gameplay/Obstacle.cs`，优先不改逻辑，仅做碰撞体和尺寸差异。
- 场景接线：
  - 将新障碍预制体加入 `SegmentContent.obstaclePrefabs`。
- 完成定义：
  - 连续 5 局中两种障碍均出现，且产生明显不同风险。

### Step C：赛道段组合扩充
- 产出（复制并重排现有段）：
  - `TrackSegment_Easy.prefab`
  - `TrackSegment_Mid.prefab`
  - `TrackSegment_Hard.prefab`
- 代码落点：
  - 无需改 `Assets/Game/Scripts/Track/TrackManager.cs` 逻辑，直接扩充 `Track Config.asset` 的 `segmentPrefabs` 列表。
  - 使用 `Assets/Game/Scripts/Track/SegmentContent.cs` 的 `spawnChance/enemySpawnChance/chestSpawnChance` 调整段内压力。
- 完成定义：
  - 3 种段在单局内均可刷出，难度曲线可感知（从松到紧）。

### Step D：刷新与节奏微调
- 目标参数：
  - 宝箱间隔先固定为每 100 分（必要时调整到 120/140）。
  - 敌人出现率先在 0.35-0.50 区间调试。
  - 总体生成率 `spawnChance` 先在 0.55-0.70 区间调试。
- 文件位置：
  - `Assets/Game/Scripts/Systems/ChestMilestoneSpawner.cs`
  - `Assets/Game/Configs/Runner Config.asset`
  - `Assets/Game/Configs/Track Config.asset`
- 完成定义：
  - 10 局中 8 局能稳定进入 3-5 分钟区间，无“空段太多”或“过载秒杀”。

## 今日可执行清单（直接照做）
- [ ] 在 `Assets/Game/Prefabs` 新增 2 个敌人变体预制体。
- [ ] 在 `Assets/Game/Prefabs` 新增 2 个障碍变体预制体。
- [ ] 在 `Assets/Game/Prefabs` 新增 3 个赛道段变体预制体。
- [ ] 在 `Assets/Game/Configs/Track Config.asset` 填入 3 个新赛道段。
- [ ] 在每个段的 `SegmentContent` 中配置新敌人/障碍数组与概率。
- [ ] 回归测试 10 局并记录：单局时长、死亡原因、是否出现内容重复。

## 验收记录模板
- 测试日期：2026-__-__
- 测试人：__
- 局数：10
- 平均时长：__ 分 __ 秒
- 最短/最长：__ / __
- 关键问题：
  - [ ] P0（阻断）
  - [ ] P1（高频影响体验）
  - [ ] 无
- 结论：
  - [ ] 通过（进入目标3：调参与反馈）
  - [ ] 未通过（返回 Step A-D 继续迭代）
