# 无尽深渊 - 版本控制与 Worktree 流程

更新时间：2026-04-04

## 一、当前结论
- 当前项目默认采用 `main + feature/* + worktree`。
- `develop` 不作为常驻分支，只有在多个功能分支需要提前联调时才临时启用。
- 禁止直接在 `main` 上开发、试错、解决半成品冲突。
- 每个 `worktree` 只对应一个明确任务分支，不再使用 `agent1`、`agent2` 这类模糊命名。

## 二、分支职责

### 1) `main`
- 只保存“随时可以打开 Unity 并继续工作”的稳定版本。
- 只接收已经自检过的功能分支，或者已经完成联调的 `develop`。
- 不直接做实验性修改，不直接保留冲突状态。

### 2) `feature/<topic>`
- 日常开发分支。
- 一个任务一个分支，一个分支一个 `worktree`。
- 示例：
  - `feature/playmode-tests`
  - `feature/ui-layout-polish`
  - `feature/chest-balance-tuning`

### 3) `hotfix/<topic>`
- 从 `main` 拉出的紧急修复分支。
- 仅用于修复会阻塞继续开发或影响主线稳定的问题。

### 4) `develop`
- 仅在以下场景启用：
  - 两个及以上功能分支必须先合并验证兼容性。
  - 一段时间内要集中联调多个改动，但暂时不想进入 `main`。
  - 需要给自己留一个“集成区”，而不是把试验性合并直接打到 `main`。
- 不满足上面条件时，不必维护 `develop`。

## 三、目录与命名约定
- 主仓库目录固定为：`E:\Document\AutoProject\Graduation design`
- `worktree` 目录统一命名为：`E:\Document\AutoProject\Graduation design_wt_<topic>`
- 分支名与目录名尽量一致，便于识别当前上下文。

示例：

```powershell
git worktree add "..\Graduation design_wt_playmode-tests" -b feature/playmode-tests main
git worktree add "..\Graduation design_wt_ui-polish" -b feature/ui-layout-polish main
```

## 四、推荐日常流程

### 1) 从稳定基线创建任务分支

在主仓库目录执行：

```powershell
git switch main
git pull origin main
git worktree add "..\Graduation design_wt_playmode-tests" -b feature/playmode-tests main
```

说明：
- `main` 先更新，再从 `main` 开新分支。
- 不要从一个已经脏掉的 `worktree` 再拉另一个新分支。

### 2) 在对应 `worktree` 内开发

进入对应目录后，只做该任务相关修改：

```powershell
git status --short --branch
```

如果发现当前目录不是预期分支，先停下，不要继续改文件。

### 3) 提交前自检

每次提交前至少执行以下检查：

```powershell
git status --short
git diff --check
rg -n "^(<<<<<<<|=======|>>>>>>>)" -g "!Library/**" -g "!Temp/**" -g "!Logs/**" -S .
```

同时确认：
- `git status` 里没有 `UnityLogs/`、`.codex-backup/`、`Library/`、`Temp/`、`obj/` 之类生成物。
- Unity 可以正常导入，没有新的脚本编译报错。
- 对主流程做一轮烟雾验证：开始 -> 游玩 -> 结算 -> 重开 -> 返回主菜单。

### 4) 提交与推送

```powershell
git add <files>
git commit -m "batch N: <简要说明>"
git push -u origin feature/playmode-tests
```

要求：
- 提交信息延续现有风格。
- 不把“半解决状态”提交上去。
- 如果冲突没有完全解决，宁可不提交。

### 5) 合并回主线

独立功能分支验证完成后：

```powershell
git switch main
git pull origin main
git merge --no-ff feature/playmode-tests
```

合并后再次检查：

```powershell
git status --short
git diff --check
rg -n "^(<<<<<<<|=======|>>>>>>>)" -g "!Library/**" -g "!Temp/**" -g "!Logs/**" -S .
```

确认无误后再推送：

```powershell
git push origin main
```

## 五、什么时候需要 `develop`
- 同时有两个以上功能分支需要一起验证。
- 两个分支都还没准备好进 `main`，但已经互相依赖。
- 你预计未来几天会频繁做“先集成、再筛选”的操作。

启用方式：

```powershell
git switch main
git pull origin main
git switch -c develop
git push -u origin develop
```

后续流程：
- 功能分支先合并到 `develop`
- 在 `develop` 做联调和回归
- 验证通过后再把 `develop` 合并到 `main`

如果只是单个功能分支开发，不启用 `develop` 更简单。

## 六、Unity 项目的特殊规则

### 1) `.meta` 文件不是普通文本
- `.meta` 的 `guid` 会影响资源引用。
- 冲突时必须只保留一个最终 `guid`，不能把两边内容拼在一起。
- 如果不确定保留哪边，先搜索引用再决定：

```powershell
rg -n "<guid>" Assets ProjectSettings Packages
```

### 2) 绝不提交冲突标记

以下内容一旦进仓库，Unity 和编译器都会报错：

```text
<<<<<<< HEAD
=======
>>>>>>> branch
```

任何一次 merge 后，都必须执行冲突标记扫描。

### 3) 生成物与临时文件必须忽略
- 必须忽略：`Library/`、`Temp/`、`Logs/`、`obj/`
- 当前项目也应忽略：`UnityLogs/`、`.codex-backup/`
- 如果这些文件已经被 Git 跟踪，仅修改 `.gitignore` 不够，还要单独从索引移除

示例：

```powershell
git rm --cached -r UnityLogs .codex-backup
```

执行前先确认路径和影响范围。

## 七、合并前固定检查清单
- 当前目录是正确的 `worktree`
- 当前分支是预期分支
- `git status` 没有无关改动
- `git diff --check` 通过
- 冲突标记扫描为空
- Unity 编译通过
- 关键流程烟雾测试通过
- 生成日志、缓存、备份文件没有进入本次提交
- `.meta` 冲突已经确认 `guid` 合法且唯一

## 八、当前仓库的落地建议
- 主仓库 `Graduation design` 保持为 `main`
- 后续新增任务统一改用 `feature/<topic>` 命名
- 现有 `agent1`、`agent2` 分支建议在各自任务收尾后改成语义化名称
- 当多个任务确实需要先联调时，再创建 `develop`
- 不要再把“集成尝试”直接打到 `main`

## 九、故障恢复原则
- 如果坏提交还没推送：直接在当前分支补一个修复提交，不要带着冲突继续开发。
- 如果坏提交已经推送：追加修复提交，不要在共享分支上强推重写历史。
- 如果不确定某个 `.meta` 的 `guid` 是否该保留：先停下查引用，再决定，不要猜。

这份流程的目标不是让分支模型变复杂，而是让 `main` 始终可用、让每个 `worktree` 职责单一、让 Unity 的资源引用不再因为错误合并被破坏。
