# GemPop

A level-based blast puzzle (tap groups of matching gems, build rockets and bombs, clear obstacles within a
move limit) built with **Unity 6000.3.10f1** (Built-in Render Pipeline) and C#.
Playable in the Unity Editor: open `Assets/_Game/Scenes/MainScene` and press Play (portrait 9:16 Game view).

## How to run
1. Open the project with Unity 6000.3.10f1 (first open regenerates `Library/`, which is not committed).
2. Set the Game view to a 9:16 aspect and press Play. An editor script (`PlayFromMainScene`) makes Play always
   start from `MainScene`, whichever scene is open; tap the level button to enter the current level.
3. Editor menu **GemPop → Level Progress** sets / resets the persisted last-played level.
4. Editor-only debug keys inside a level: **W** win, **L** lose, **Esc** back to the menu (compiled out of builds).

## Project layout
```
Assets/_Game/
  Scenes/            MainScene (level button), LevelScene (gameplay)
  Scripts/
    Core/            LevelProgress (PlayerPrefs persistence), SceneLoader, LevelSceneBootstrap (level flow)
    Data/            LevelData (JSON model), LevelRepository (loading), ItemCode (grid codes -> typed specs)
    Items/           GridItem hierarchy + Damage model
    Board/           Board (model), BoardLayout (grid <-> world), BoardController (turn loop), ItemFactory, BoardInput
    Board/Animation/ Tween (hand-written coroutine animations), CoroutineBatch
    Systems/         MatchFinder, GravitySystem, HintSystem, ExplosionResolver, AdjacencyDamage, GoalTracker, MoveTracker
    Systems/Fx/      ParticleFx (code-built particle systems), BoardFx (which effect for which event)
    Audio/           Synth (software synthesizer), SfxLibrary, MusicLibrary, TurkishMarchScore, AudioManager, BoardSfx
    UI/              TopBar, GoalSlot, FailPopup, WinCelebration, LevelButton
    Editor/          LevelProgressMenu, ItemPrefabBuilder, LevelUiBuilder (one-click setup tools), PlayFromMainScene
  Resources/Levels/  level_01..10.json
  Prefabs/           generated item prefabs + ItemPrefabSet
  Art/               CC0 sprites by Kenney (kenney.nl), see CREDITS.md
```

## Architecture

### Model / view separation
`Board` is a plain C# grid of `GridItem` references and the single owner of cell state (`Place`, `Remove`, `Move`).
`BoardLayout` converts cells to world positions and sizes any 6..10 grid to the portrait screen.
`BoardController` owns the turn loop and applies model changes **first**, then animates the views to catch up.
While a turn resolves, input is ignored, so the model is never mutated mid-animation.

### Item hierarchy (inheritance + polymorphism)
```
GridItem (abstract)            CanFall, IsObstacle, CellSpan, OccupiedCells, TakeDamage(DamageInfo)
├── Cube                       color, hint sprite; destroyed by any explosion
├── SpecialItem (abstract)     falls; destroyed by damage and chain-explodes at its own cell
│   ├── Rocket                 horizontal / vertical
│   └── Tnt
└── Obstacle (abstract)        Health, IsCleared
    ├── Vase                   2 health, falls, at most 1 damage per event
    ├── Stone                  1 health, static, ignores blasts
    └── ChaliceBox             2x2, door phase (1 damage per event opens it) then chalice phase (10 chalices)
```
`TakeDamage` is abstract: the board never asks "what type are you?"; each item applies its own rule.
`DamageInfo.EventId` identifies the blast/explosion a hit belongs to, so "once per event" rules work without the
item knowing who hit it. Adding a new obstacle = one subclass + one prefab + one entry in `ItemCode`.

### Turn loop
```
tap → MatchFinder (flood fill)
    → cube group: adjacent obstacles take blast damage, group removed; 2-3 pop, 4-5 gather into a rocket, 6+ into a TNT
    → special item: alone → explodes; with adjacent specials → all gather at the tapped cell and explode as one combo
    → ExplosionResolver resolves explosions in waves (specials hit during a wave form the next wave)
    → GravitySystem.Collapse (vertical only, obstacles block) + FindSpawnCells (top-reachable empties)
    → animated falls (coded, no physics) → deadlock guard → HintSystem → TurnEnded
LevelSceneBootstrap: on TurnEnded → goals complete ? win : no moves ? lose
```

### Explosions
Rockets: two halves fly outward, damaging cells one by one. TNT: 5x5. Combos (spec order): ≥2 TNT → 7x7;
1 TNT + rockets → 3x3 `+` of rockets; rockets only → `+` of two rockets.

### Events, not references
`BoardController.ItemDamaged/ItemDestroyed → GoalTracker.Recount → TopBar` and `MoveTracker.Changed → TopBar`.
UI classes only render; `LevelSceneBootstrap` decides scene transitions. No DI framework;
dependencies are passed through constructors / serialized references.

### Editor tooling
`ItemPrefabBuilder` generates the item prefabs and asset references from the provided sprites;
`LevelUiBuilder` builds the level UI from measured reference proportions. Both are repeatable and diff-able.
Visual tuning lives in code rather than in scene edits: item fit (`cellFill`, `visualOffset`), particle looks
(`ParticleFx`, e.g. `RocketTrailSettings`) and sound recipes (`SfxLibrary`) are all plain values to change and commit.

## Decisions and assumptions
- Falling is **vertical only**; diagonal fill was intentionally left out to keep gravity predictable.
- Chalice box: 10 chalices per box; the door opens after one damage event; a box is removed when its chalices reach 0.
  The goal panel shows the total remaining chalices.
- A combo includes the whole connected group of special items; a special hit by an explosion explodes individually in place.
- A vase takes at most one damage per explosion event; in the chalice phase every rocket pass counts.
- Win/lose is evaluated when the board is still; clearing the last obstacle with the last move is a win.
- Deadlock guard: if no move exists, cube colors are re-rolled in place so the board can never get stuck.
- Animations are hand-written coroutines (`Tween`); no physics, no Animator, no third-party tween library.
- Cubes are pooled in `ItemFactory`; other items are rare enough to instantiate.
- Every sound is **synthesized in code** (no audio files in the repo)
  (`Audio/Synth`): effects are short signal recipes, the menu waltz is original, and the level music is our own
  rendition of Mozart's Rondo alla Turca (public-domain composition, no recording used).
