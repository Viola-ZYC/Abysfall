Endless Runner Framework (2D Vertical)

Folders
- Scripts/Core: game state and manager
- Scripts/Systems: input, scoring, pooling, hit stop
- Scripts/Gameplay: player, enemies, obstacles, collectibles
- Scripts/Systems/Abilities: roguelike ability system (definitions, effects, manager)
- Scripts/Track: segment and track spawning
- Scripts/UI: HUD wiring
- Configs: ScriptableObject configs
- Abilities: sample ability assets and effects
- Physics: physics materials (NoFriction default)

Quick Setup (Unity Editor)
1) Create RunnerConfig and TrackConfig assets from the Create menu.
2) Create a TrackSegment prefab with an EndPoint child transform at the bottom edge.
3) Add TrackManager, ObjectPool, GameManager, ScoreManager, HitStopper to a scene.
4) Create a player with Rigidbody2D + Collider2D + RunnerController.
5) Assign references in GameManager, TrackManager, ScoreManager, RunnerController, and InputRouter.
6) Hook InputRouter actions or rely on keyboard/touch swipe fallback.
7) (Optional) Add HUDController and bind Score/Health texts + Menu/GameOver panels.
8) Add AbilityManager and assign ability assets (sample assets under `Assets/Game/Abilities`).
9) Add AbilitySelectionUI (auto-creates selection panel + EventSystem if missing).
10) Add AbilityChest prefab to TrackSegment (already wired) to pause and choose an ability on contact.
