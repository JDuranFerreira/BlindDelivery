# Blind Delivery — Technical Architecture & Build Plan

*Unity 6.6, C#, 2D, mobile portrait. Companion to the Game Design Document v2.0.*

## 1. Starting Point and Key Decisions

The template already provides pooling, input, audio, save, settings, scene loading, localization, ads and IAP. The old grid service was built for tower defense and does not fit free-form placement, so it is replaced as described below. Everything else is reused.

**Decision 1: two layers instead of one grid.**

- **Tile layer (static):** level geometry only, such as floors, walls, hazards and point A and B. Stored as a tilemap. It is the source of truth for the Safe Fall Height, measured in cells.
- **Free-form layer (dynamic):** player items are placed at any world position using real 2D colliders. Validation uses overlap checks against the tile layer and other items, not grid cells.

**Decision 2: the boy runs on a fixed-step simulation of 0.02 seconds.** Physics2D is stepped manually with a constant fixed step. This makes the level validator, ghost replays and the daily leaderboard possible. Unity physics is not guaranteed identical across devices, so the leaderboard trusts the final score, which is the points left, but replays are treated as visual only and are recorded as player inputs plus the result, not as a proof.

**Decision 3: pure logic stays in Core.** Rules such as stars, points, fall damage and upgrade gating live in plain C# with no Unity types, so they can be unit tested without a scene.

**Decision 4: game-wide tuning lives in one data asset.** A single GameBalance ScriptableObject holds Safe Fall Height (3), item point costs, cooldown lengths and crate limits, so balance changes never touch level data.

## 2. Assemblies

| Assembly | Responsibility | Status |
| --- | --- | --- |
| BlindDelivery.Core | Pure logic: scoring, points pool, upgrade gating, crate rules, level data types, simulation interfaces | Extend template |
| BlindDelivery.Services | Audio, Input, Save, Settings, Pooling, Ads, IAP, Haptics | Reuse, add Haptics |
| BlindDelivery.Gameplay | Boy, crates, items, hazards, wind, sensory system, level runtime, HUD | New |
| BlindDelivery.Editor | Level authoring, validator, balance tools | Extend template |
| BlindDelivery.Tests | Edit mode tests for Core, play mode tests for simulation | New |

Dependency rule: Gameplay depends on Core and Services. Core depends on nothing. Services never depend on Gameplay.

## 3. Systems: Reused, Changed and New

| System | Status | Notes |
| --- | --- | --- |
| Pooling | Reuse | Items, crates, particles, audio sources |
| Input | Change | Add tap-to-place, press-and-hold-drag, no pan or zoom |
| Audio | Extend | Layered mixer, spatialized echolocation hum, beacon pings |
| Save | Extend | Best stars per level, upgrades, credits, daily seed, replay data |
| Settings | Extend | Accessibility toggles, haptic intensity, assist mode |
| SceneLoader | Reuse | Level and daily route loading |
| Localization | Reuse | UI text and audio descriptions |
| Ads / IAP | Reuse | Rewarded retry, credit boost, credit bundles |
| Grid service | Replace | Becomes a small tile query service used only for static geometry and fall height |
| Boy controller | New | State machine, see section 4 |
| Crate stack | New | Stack, wobble, loss rules |
| Sensory system | New | Echolocation scan, edge detection, audio and haptic output |
| Item placement | New | Ghost, validation, drag, cooldowns |
| Points pool | New | Per-level pool, drain, floor at zero |
| Star and upgrade gating | New | Best stars per level summed, tier requirements |
| Hazards and wind | New | Gaps, spikes, wind zones, wind break interaction |
| Level validator | New | Simulates the boy to prove solvability |
| Haptics service | New | Wraps iOS Taptic and Android VibrationEffect patterns |

## 4. New Systems in Detail

### 4.1 Simulation loop

A single SimulationRunner owns time. Each fixed step it runs in this order: update item cooldowns, update the sensory scan, tick the boy state machine, apply wind and hazard forces, call Physics2D.Simulate with the constant step, then resolve crate and delivery events. Physics2D simulation mode is set to Script so nothing else advances physics. The fixed step value lives in GameBalance.

### 4.2 Boy controller

A state machine with these states: Idle (prep phase), Walking, Turning, Falling, Launched (spring or wind), Slowed, Delivered and Dead. Transitions are driven by sensor results, not by scene scripting.

- Walking moves at the level's speed and flips direction on a wall or blocking item hit.
- Falling records the start height. On landing, the fall distance in cells is compared with Safe Fall Height from GameBalance. Over the limit calls CrateStack.LoseTop(), unless a cushion or cargo insurance absorbs it.
- Slowed is only reachable if the Slow-Down upgrade is owned, and its speed factor and duration come from the upgrade level.

### 4.3 Crate stack

A plain C# class in Core holding the crate count and capacity (3 base, up to 5). It raises events for lost, dropped and delivered. A separate view component in Gameplay renders the stack and the wobble, which grows with stack height and boy speed. Wobble is visual only and never changes the rules.

### 4.4 Sensory system

Two parts. The scan itself is a set of short 2D raycasts and overlap checks ahead of the boy, run at the refresh rate interval. It returns a result struct: ledge distance, wall distance, beacon direction. Output channels subscribe to that result: an audio channel (hum pitch, edge warning tone, beacon ping), a haptic channel, and the slow-down trigger. Keeping the scan result as data means the validator and tests can read it without audio.

### 4.5 Item placement

- **ItemDefinition** (ScriptableObject): prefab, footprint size, point move cost, cooldown, placement rules.
- **PlacementController**: tap an item in the toolbar to select it, a ghost follows the touch, tapping the map confirms.
- **Validation**: Physics2D.OverlapBox at the ghost position, using a layer mask for the tile layer and other items. Green when clear, red when overlapping or blocked. A short snap tolerance helps thumbs.
- **Move**: press and hold on a placed item picks it up, drag moves its ghost, release re-validates and drops it. If invalid, it snaps back to its previous position at no cost.
- **Cooldown**: each placed item has a timer. While it runs, the item cannot be picked up and shows a ring under it.

### 4.6 Points pool

A plain C# class in Core. It starts at the level's fixed pool and drains by the item's move cost whenever an item is moved. The first placement of each item is free. It floors at zero, never fails the level, and the final value is the score.

### 4.7 Stars and upgrade gating

- **StarCalculator** (Core): crates delivered to stars, including gold bonus stars for the 4th and 5th crate.
- **ProgressService**: stores the best stars per level and sums them into the total star count.
- **UpgradeService**: a tier is purchasable only if the total stars meet the tier threshold and the player has enough credits. Stars are never deducted.

### 4.8 Hazards and wind

Hazards are trigger colliders with a type: gap, spike, traffic. Wind is a zone with a direction and strength that applies a force each fixed step. A wind break is an item whose collider carves a no-wind region, checked each step. All of it is configured in level data.

### 4.9 Haptics service

A thin interface, IHapticsService, with one method per pattern from the design doc: edge warning, crate lost, crates delivered, item placed, beacon direction, slow pulse and ledge detected. iOS and Android implementations sit behind it, plus a null implementation for the editor and for players with haptics off.

### 4.10 Level authoring tools and validator

All levels are hand-made, so the priority is a fast authoring workflow inside the Unity editor. There is no procedural generator. The pieces are listed below.

- **LevelData (ScriptableObject):** one asset per level holding the tilemap prefab, point A and B, prep countdown, tool set, point pool, boy speed and hazard and wind references.
- **WorldData (ScriptableObject):** an ordered list of levels per world, with the mechanics introduced and the star totals the world awards.
- **ItemDefinition (ScriptableObject):** one per tool, with prefab, footprint, move cost and cooldown.
- **Level prefab template:** a ready tilemap, boy start marker, goal marker, hazard and wind zone prefabs, so a new level is duplicated and edited, not built from scratch.
- **Level editor window:** a custom editor window to create a level from the template, preview point A, B and the safe fall height as a line on platform edges, and run the level in play mode with one click.
- **Validator:** a menu command that runs the simulation headless with the level's stored reference solution and checks that the boy reaches B with all 3 crates. It runs on every level in CI, and a failing level fails the build.
- **Level of the Day:** a hand-picked rotation from a curated list of levels, using the same leaderboard. Which level is shown today is chosen from the date, with no generator involved.

## 5. Build Plan

Each phase ends with a test. Do not start the next phase until the done-when test passes. Rules and data come before visuals, so each phase can be tested without art.

| Phase | What to build | Done when |
| --- | --- | --- |
| 0. Foundation | Create the new assemblies and the test assembly. Add GameBalance asset with Safe Fall Height 3, fixed step, item costs. Strip the tower defense grid out of the gameplay path. | Project compiles. An edit mode test reads Safe Fall Height 3 from GameBalance. |
| 1. Core rules | CrateStack, PointsPool, StarCalculator, UpgradeService gating, LevelData types, all as plain C#. | Unit tests pass: 2 crates gives 2 stars, 5 crates gives plus-plus rating, points floor at zero and never fail, a tier will not buy without enough stars, and stars are never deducted. |
| 2. Simulation and tile layer | SimulationRunner with manual Physics2D stepping. Tilemap level loader. Tile query for fall height in cells. | A test level loads and the same inputs give the same final boy position on two runs in the editor. |
| 3. Boy walks | Boy controller states Idle, Walking, Turning, Falling, Delivered. Gravity, wall turns, A to B. | In a flat test level, the boy walks from A to B and turns at walls. A fall of 4 cells loses 1 crate, a fall of 3 does not. |
| 4. Item placement | ItemDefinition assets, PlacementController, ghost validation with OverlapBox, move by hold and drag, cooldown ring. Ramp and tactile tile first. | You can place a ramp with one thumb, it rejects overlapping spots, you can move it, and it cannot be moved again until the cooldown ends. |
| 5. Full toolkit | Spring, cushion, wind break, beacon. Hazards and wind zones. | Each item has a test level proving its effect: spring launches up, cushion absorbs a long drop, wind lifts the boy, wind break stops it. |
| 6. Vertical slice | One complete level with HUD, points meter, prep countdown, stars result screen. Placeholder art and audio. | A tester who has not seen the game finishes the level, gets stars and a score, and replays it without help. |
| 7. Blind systems | Sensory scan, echolocation hum, edge warning tone, haptics service, ledge slow-down behind the upgrade. | With headphones and eyes closed on the test level, a tester can tell when a ledge is near. The slow-down only works when the upgrade is owned. |
| 8. Progression | Save of best stars per level, total stars, upgrade shop with star gates, credits, tutorial levels 1 to 5. | A fresh save plays the tutorial, earns stars, and the shop locks and unlocks tiers correctly. Replaying a level never double counts stars. |
| 9. Level authoring tools, validator and levels | LevelData and WorldData assets, level prefab template, level editor window, headless validator, reference solutions, then the Downtown and Industrial levels. | A new level can be created from the template and played in one click. CI runs the validator on every level and fails the build if one is unsolvable. |
| 10. Level of the Day and leaderboard | Curated rotation of hand-made levels chosen by date, anonymous sign-in with Unity Authentication, Unity Leaderboards with one board per level plus a Level of the Day board, ghost replay as visual only. | Two devices on the same date show the same level of the day, and the leaderboard ranks by points. |
| 11. Accessibility pass | Screen reader, haptic-only mode, contrast and colour palettes, assist mode. | A full level is completable with haptic-only mode and with a screen reader. |
| 12. Monetization and polish | Rewarded retry, credit boost, IAP, battle pass, consent flow, performance pass. | No forced ads appear. A mid-range phone holds a steady frame rate with all systems on. |

## 6. Folder and Naming Convention

- Assets/BlindDelivery/Core, Services, Gameplay, Editor, Tests, one folder per assembly.
- Data assets in Assets/BlindDelivery/Data: GameBalance, ItemDefinitions, Levels, Worlds.
- Physics layers: Tiles, Items, Boy, Hazards, WindZones, Sensors. Set up the collision matrix in Phase 0.
- One level equals one LevelData asset plus one tilemap prefab.

## 7. Key Technical Risks

| Risk | Mitigation |
| --- | --- |
| Physics not identical across devices | Leaderboard trusts final points, replays are visual only, validator runs on the editor build |
| Thumb placement imprecise on a tall portrait screen | Snap tolerance, large hit targets, ghost offset above the finger so it is not hidden, tune in Phase 4 |
| Items moved at the last instant break the puzzle | Per-item cooldown from GameBalance, tuned per item |
| Echolocation audio expensive on mobile | Pool audio sources, one spatialized hum source, test on a low-end device in Phase 7 |
| Haptics differ between devices | Haptics service behind an interface, intensity setting, test on both platforms early in Phase 7 |
| Hand-made levels that turn out unsolvable | Validator gate on every level, with a stored reference solution |

## 8. Open Technical Questions

- **Fixed step:** decided, 0.02 seconds (50 steps per second), stored in GameBalance.
- **Wobble:** decided, visual animation only, never affects the rules.
- **Haptics:** decided, use the Unity package route plus native Android vibration calls behind IHapticsService. Write a native plugin only if patterns feel too weak on real devices.
- **Daily route generator:** decided, not needed. All levels are hand-made, and the daily feature becomes a curated Level of the Day.
- **Leaderboards: decided, Unity Leaderboards from Unity Gaming Services, so there is no custom server. Install the Leaderboards package from the Package Manager, create a Leaderboard configuration asset in the project and deploy it. Use one leaderboard per level with the Keep Best Score update type, ranked by points left, plus a Level of the Day board with a scheduled daily reset. Players sign in anonymously through Unity Authentication, and the service sits behind an ILeaderboardService interface in the Services assembly. Scores come from the client and can be tampered with, so consider server-side validation with Cloud Code after launch. Still open: how many levels to author before the soft launch**.

*End of Technical Architecture — v1.0*
