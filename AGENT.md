# CLI B Agent Brief

本文件定义 CLI B 在本项目中的工作范围。仓库通用规范仍以 [`AGENTS.md`](E:/Document/AutoProject/Graduation%20design/AGENTS.md) 为准；当两者冲突时，先遵守仓库安全与提交流程，再遵守本文件的任务边界。

## 角色

CLI B 负责：
- 首次发现图鉴弹窗
- 暂停恢复
- 图鉴数据一致性

## 本轮目标

检查并在必要时修复：
1. 首次遇到新生物时暂停并弹出图鉴。
2. 首次遇到新收藏品时暂停并弹出图鉴。
3. 重复遇到已解锁条目时不重复弹窗。
4. 弹窗关闭后正确恢复游戏。
5. `creature` / `obstacle` / `collectible` 使用的 `codexEntryId` 与 `CodexDatabase` 对齐。
6. 不改 HUD 图鉴页 UI 文件。

## 允许修改

- `Assets/Game/Scripts/UI/AbilityAcquiredUI.cs`
- `Assets/Game/Scripts/Gameplay/Collectible.cs`
- `Assets/Game/Scripts/Gameplay/SpecialCreature.cs`
- `Assets/Game/Scripts/Gameplay/Obstacle.cs`
- `Assets/Game/Scripts/Systems/RunProgressStore.cs`
- `Assets/Game/Resources/Codex/CodexDatabase.asset`
- 如确有必要，可改相关 prefab 的 `codexEntryId` 配置

## 只读参考

- `Assets/Game/Scripts/UI/HUDController.cs`
- `Assets/Game/Scripts/UI/MainMenuSceneController.cs`

## 禁止修改

- `Assets/Game/Scripts/UI/HUDController.cs`
- `Assets/Game/Resources/UI/GameUI.uxml`
- `Assets/Game/Resources/UI/GameUI.uss`
- 背景 / Track / Scene 资源

## 工作原则

- 先审计，再决定是否改动。
- 只有发现真实问题时才改；不要为了制造产出而改代码。
- 若数据一致性正常，要明确写出“无需改动”。
- 尽量复用现有 codex 解锁、暂停、弹窗逻辑，不额外发明一套状态机。
- 静态检查可以做，Unity 运行时验证不能声称已完成，除非真的在编辑器内手测。

## 最低检查项

- 检查首次解锁与重复解锁分支是否区分。
- 检查弹窗显示和关闭时的暂停 token / `Time.timeScale` 恢复路径。
- 检查 `creature` / `obstacle` prefab 与 `CodexDatabase` 的 `id` 对齐情况。
- 检查 `collectible` 的运行时 `codexEntryId` 来源是否与 `CodexDatabase` 一致。
- 改完后运行静态检查，至少包含 `git diff --check`。

## 输出格式

- 审计结论
- 改了哪些文件
- 剩余风险 / 需要用户手测什么
