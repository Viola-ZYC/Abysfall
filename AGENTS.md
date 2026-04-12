# Repository Guidelines

## Project Structure & Module Organization
`Assets/Game` contains the playable content. Runtime code lives in `Assets/Game/Scripts`, split into `Core`, `Systems`, `Gameplay`, `Track`, `UI`, and `Characters`. Editor-only utilities are under `Assets/Game/Editor`; configs, prefabs, abilities, audio, and Resources assets sit alongside them. Scenes live in `Assets/Scenes`, Play Mode tests in `Assets/Tests/PlayMode`, design and process docs in `Docs`, package definitions in `Packages`, and Unity engine settings in `ProjectSettings`.

## Build, Test, and Development Commands
Open the project in Unity `6000.3.0f1` or load `Graduation design.sln` in Rider/Visual Studio for script work.

```powershell
dotnet build "Graduation design.sln"
```

Use this for a fast C# compile check outside the editor.

```powershell
& "E:\AppData\Unity\Unity 6000.3.0f1\Editor\Unity.exe" -batchmode -projectPath "E:\Document\AutoProject\Graduation design" -runTests -testPlatform PlayMode -testResults "UnityLogs\PlayMode.xml" -quit
```

Use this to run the Play Mode suite. When art or codex data changes, refresh generated assets from the Unity menu: `Tools/Player/Build Visual Animator` and `Tools/Codex/Build Codex Database`.

## Coding Style & Naming Conventions
Follow the existing C# style: 4-space indentation, braces on new lines, and small focused MonoBehaviours. Use `PascalCase` for classes, methods, scenes, and ScriptableObject assets; use `camelCase` for private fields, including `[SerializeField]` members. Keep new files in the matching module folder instead of creating generic catch-all scripts.

## Testing Guidelines
Tests belong in `Assets/Tests/PlayMode` and should use `*Tests.cs` names such as `PauseManualFlowTests.cs`. Add or update Play Mode coverage when touching scene bootstrap, pause/game-over flow, resource loading, or run-state transitions. After gameplay or UI edits, manually verify the core loop: main menu -> start run -> death/result -> restart -> back to main menu.

## Commit & Pull Request Guidelines
Recent history uses `batch N: <summary>`; continue that format for normal commits. Prefer `feature/<topic>` branches and matching worktrees such as `Graduation design_wt_ui-layout-polish`, then merge back to `main` only after Unity imports cleanly and smoke checks pass. PRs should describe gameplay/UI impact, list touched scenes or prefabs, and include screenshots or short clips for visible changes.

## Unity Asset Hygiene
Never commit generated folders such as `Library/`, `Temp/`, `Logs/`, `obj/`, `UnityLogs/`, or `.codex-backup/`. Commit `.meta` files together with any added, moved, or renamed asset; Unity references depend on those GUIDs.
