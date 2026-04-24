# CLI A Agent Brief

本文件定义 CLI A 在本项目中的工作范围。仓库通用规范仍以 [`AGENTS.md`](E:/Document/AutoProject/Graduation%20design/AGENTS.md) 为准；当两者冲突时，先遵守仓库安全与提交流程，再遵守本文件的任务边界。

## 角色

CLI A 负责：
- gameplay scene 内的暂停菜单与隐藏图鉴页整合
- 暂停态下的 manual/codex 子面板切换
- 不影响首次发现弹窗的 HUD 侧 UI 落地

## 本轮目标

完成游戏内隐藏图鉴页和暂停菜单的整合，保证：
1. 暂停菜单里有 `Manual` 按钮。
2. 点击 `Manual` 打开 gameplay scene 内的隐藏图鉴页。
3. 图鉴页支持 `Creatures` / `Obstacles` / `Collections` 三个 tab。
4. 图鉴页关闭后回到暂停菜单。
5. `Continue` 后恢复游戏。
6. 支持 `Esc` / 返回键关闭当前图鉴或设置子面板。
7. 不破坏现有首次发现弹窗逻辑。

## 允许修改

- `Assets/Game/Scripts/UI/HUDController.cs`
- `Assets/Game/Resources/UI/GameUI.uxml`
- `Assets/Game/Resources/UI/GameUI.uss`

## 只读参考

- `Assets/Game/Scripts/UI/MainMenuSceneController.cs`
- `Assets/Game/Scripts/UI/AbilityAcquiredUI.cs`

## 禁止修改

- `Assets/Game/Scripts/Systems/RunProgressStore.cs`
- `Assets/Game/Scripts/Gameplay/Collectible.cs`
- `Assets/Game/Scripts/Gameplay/SpecialCreature.cs`
- `Assets/Game/Scripts/Gameplay/Obstacle.cs`
- 背景 / `Track` / `Scene` 资源

## 工作原则

- 先阅读现有 `HUD` 和 `MainMenu` 的手册页实现，再决定怎么接到 gameplay scene。
- 尽量复用已有 manual/codex 的数据读取思路，不额外发明一套数据源。
- 优先在 `HUDController` 内完成状态流转，避免把暂停、手册、首次发现弹窗拆成互相竞争的状态机。
- 必须保证首次发现弹窗逻辑继续可用；若发现潜在冲突，要先说明冲突点，再最小化修复。
- 静态检查可以做，Unity 编辑器内运行与视觉验证不能声称已完成，除非真的手测过。

## 最低检查项

- 检查暂停菜单、手册页、设置子面板之间的返回路径是否闭合。
- 检查 `Esc` / 返回键在以下状态下的行为：暂停菜单、手册页、设置页、首次发现弹窗显示期间。
- 检查 `Continue` 是否只恢复游戏，不会错误关闭首次发现弹窗或清空图鉴选择状态。
- 检查 `git diff --check` 是否通过。
- 明确列出仍需在 Unity 编辑器内手测的场景。

## 输出格式

- 准备怎么改
- 实际改了什么
- 需要用户验证什么
