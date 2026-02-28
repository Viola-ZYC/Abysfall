# Spawn Progress Preset (Balanced)

这是一套已经落地到项目里的“按进度固定数量生成”参数。

## 1) 使用的数学公式

- 进度因子：`p = ln(1 + score / progressScoreScale)`
- 敌对总数：`hostileCount = floor(hostileBaseCount + hostileGrowth * p)`
- 敌人占比：`enemyRatio = clamp01(enemyRatioBase + enemyRatioGrowth * (1 - exp(-p)))`
- 敌人数：`enemyCount = round(hostileCount * enemyRatio)`
- 障碍数：`obstacleCount = hostileCount - enemyCount`
- 收藏品数：`collectibleCount = floor(collectibleBaseCount + collectibleGrowth * sqrt(1 + p))`
- 宝箱：按分数间隔固定刷新（`scoreInterval`）

## 2) 已应用参数

### SegmentContent（赛道内敌人/障碍/收藏品）
- `progressScoreScale = 180`
- `hostileBaseCount = 1`
- `hostileGrowth = 1.1`
- `enemyRatioBase = 0.25`
- `enemyRatioGrowth = 0.4`
- `chestBaseCount = 0`
- `chestGrowth = 0`
- `collectibleBaseCount = 1`
- `collectibleGrowth = 0.75`

### ChestMilestoneSpawner（里程碑宝箱）
- `scoreInterval = 120`
- `maxSpawnsPerTick = 2`
- 水平位置：确定性序列（非随机）

### TrackManager（赛道段选择）
- 确定性序列 + 进度偏移（非随机）

## 3) 体感分段（参考）

- `score 0~250`：敌对约 `1`，收藏品约 `1`
- `score 250~900`：敌对约 `2`（敌人通常 `1`），收藏品约 `2`
- `score 900+`：敌对约 `3`（敌人通常 `2`），收藏品约 `2`
- 宝箱：每 `120` 分稳定出现一次

## 4) 已修改文件

- `Assets/Game/Scripts/Track/SegmentContent.cs`
- `Assets/Game/Scripts/Systems/ChestMilestoneSpawner.cs`
- `Assets/Game/Scripts/Track/TrackManager.cs`
- `Assets/Game/Prefabs/TrackSegment.prefab`
- `Assets/Scenes/SampleScene.unity`
