# Blind Delivery — Game Design Document (v2.0)

*Lemmings-style indirect control. A blindfolded courier carries up to 5 crates from point A to point B.*

## 1. Core Concept

**Genre:** 2D vertical puzzle-platformer with indirect control (Lemmings-inspired) **Platform:** Mobile (iOS/Android), portrait orientation, one-handed play **Session Length:** 2–5 minutes per run **Target Audience:** Casual/puzzle players, accessibility-conscious gamers

**Elevator Pitch:** A blindfolded courier walks automatically from point A to point B, carrying crates on his head. You can't control him. Instead, you place and move guidance objects (ramps, springs, beacons, tactile tiles, wind breaks, cushions) to build his route. Every crate that survives the trip earns you stars.

**Key design pillars:**

- One thumb, one screen: the whole level is always visible. No pan, no zoom.
- Constant juggling: items can be picked up and moved at any time, so the player is always doing something.
- Two layers of success: stars for delivering crates, points for playing efficiently.
- Blind identity: audio and haptics are the courier's world, and a first-class part of the experience.

## 2. Core Loop

1. **Level start:** the whole level is shown on one portrait screen. A prep countdown begins.
2. **Prep phase:** the player can already place items while the countdown runs. When it ends, the boy starts walking from point A.
3. **Run:** the boy walks automatically. The player places, grabs and moves items to guide him toward point B.
4. **Hazards and crates:** gaps, spikes, wind and traffic threaten the crates he carries.
5. **Arrival:** reaching point B ends the run. Stars are awarded for crates delivered, and a points score is recorded for the leaderboard.
6. **Meta:** stars unlock upgrade tiers, credits buy them, then the next level or the daily route.

**Win condition:** reach point B with at least 1 crate. **Fail condition:** all crates are lost, or the boy is destroyed by a hazard.

**Timer:** there is no fail timer. The only timer is the prep countdown at the start of the level, which gives the player time to read the map and place the first items. Its length is set per level.

## 3. The Boy (AI Character)

### Movement

- Constant forward velocity, configurable per level (1.5–3.5 m/s).
- Turns around 180° when he bumps into a wall or a blocking object.
- Cliff behaviour: he walks off ledges and falls under gravity.
- Levels run top to bottom, or in some levels bottom to top. Wind and springs are the main tools for going up.

### Crates

- Every run starts with 3 crates. The Box Capacity upgrade raises this to up to 5.
- Crates are stacked on his head. Stack height equals the number of crates he carries (max 5), and a taller stack wobbles more, so carrying more feels like a real risk.
- **Fall rule:** a fall longer than the Safe Fall Height loses 1 crate from the top of the stack. Safe Fall Height is a single global value stored in the game data, currently 3 cells. It is the same for every level and can be changed to 2 later as a balance decision. Cushions and ramps are the designed way to survive longer drops.
- The safe fall height is shown in-game with a faint marker at platform edges so players can read it at a glance.

| State | Behaviour | Visual |
| --- | --- | --- |
| Carried | Stacked on head, moves with boy | Stacked crates |
| Dropped | Falls with physics, can be picked up | On ground, pulsing |
| Lost | Falls off-screen or destroyed | Poof + sound |
| Delivered | Reaches point B with the boy | Confetti + score |

### Sensory Model (the "blind" mechanic)

- **Passive echolocation (always on):** a continuous forward scan cone that updates at the refresh rate.
- **Footstep rhythm:** audio pitch changes with surface (concrete, wood, metal, grass).
- **Edge warning:** a rising tone when approaching a cliff.
- **Beacon ping:** directional audio toward placed beacons.
- **Haptic compass:** vibration intensity shows proximity to the next guidance object.

### Ledge Slow-Down (unlockable)

- Not active at the start of the game. **Level 1 of the Echolocation Slow-Down upgrade unlocks it**, and levels 2 and 3 make it stronger.
- When the scan detects a ledge within range, the boy automatically slows for a short time.
- Feedback: a brief focus vignette, a low hum, and a steady slow haptic pulse while slowed.
- Early levels must be forgiving without this ability, since players won't have it yet.
- Stacks with the Haptic Sensitivity upgrade for earlier detection.

## 4. Guidance Objects (Player Tools)

### Placement and movement

- **One-handed flow:** tap an item in the toolbar, then tap the map to place it anywhere. No dragging from the toolbar is needed.
- Items can be placed anywhere on the map as long as the spot is valid. Green ghost means valid. Red ghost means invalid (overlapping the floor, a wall or another object).
- **Move at any time:** the player can press and hold a placed item and drag it to a new spot, including while the boy is walking. This keeps the player constantly engaged.
- **Item cooldown:** after an item is placed or moved, a small cooldown ring shows under the item. It cannot be moved again until the cooldown ends. This stops last-instant teleporting of a ramp under the boy.
- **Tool set per level:** each level grants a fixed set of tools (for example, 3 items) that the player moves around.
- Cancel a placement with a long press on the ghost or by tapping outside the map.

### Object types

| Object | Effect | Physics |
| --- | --- | --- |
| Ramp | Bridges a 1-cell height gap | Static, friction 0.8 |
| Spring | Launches the boy 3 cells up or forward | Impulse force, 0.3s cooldown |
| Tactile Tile | Forces turn, stop or speed change | Trigger zone |
| Audio Beacon | Emits a directional ping (5s) | Audio only |
| Wind Break | Blocks horizontal wind in a 3-cell radius | Static |
| Cushion | Prevents crate loss on long drops | Soft body, deforms |

## 5. Scoring: Stars and Points

### Stars (crates delivered)

- Every level has a target of 3 crates.
- 3 crates delivered = 3 stars. 2 crates = 2 stars. 1 crate = 1 star. 0 crates = fail.
- **Bonus stars:** with the Box Capacity upgrade the boy can start with up to 5 crates. Delivering 4 gives a bonus star, and delivering all 5 gives the top **"3 stars plus plus"** rating.
- Bonus stars are shown in gold, separate from the normal three, so players without the upgrade never feel punished.
- Stars come only from crates. Nothing else affects them.

### Points (efficiency, leaderboard)

- Each level has its own **fixed starting point pool**, set by the designer. Puzzle-style levels can have bigger pools than fast-paced ones, which makes the pool a level balancing tool.
- Moving or placing an item drains a small fixed amount from the pool. The first placement of each item is free, and only moving it costs points.
- Costs are fixed per item type so players can learn them.
- Points floor at zero. They never cause a fail, and nothing adds points back.
- The final score is whatever is left when the boy reaches point B. Crates do not add points.
- The pool is shown as a visible meter on the HUD.
- Points feed the leaderboard and the daily route ranking.

## 6. Level Structure

Every level fits on one portrait screen. The grid is narrow and tall, roughly 8–10 cells wide and 14–20 cells tall, so the boy zigzags between platforms like a pinball. Most levels run from A at the top to B at the bottom. Some run from the bottom up, using wind and springs.

**Level data (per level):**

- seed
- width and height in cells
- start point A and end point B
- crate target: always 3
- prep countdown length (seconds)
- tool set: the items granted for this level
- point pool: the fixed starting points for this level
- boy speed
- hazards and wind configuration

**Game data (global, not per level):**

- Safe Fall Height: 3 cells (may become 2 after balance testing)
- point cost per item type

### Progression

Each mechanic is introduced exactly once. Later worlds combine or intensify it.

| World | Levels | New mechanics introduced |
| --- | --- | --- |
| Tutorial | 1–5 | Ramp, spring, basic audio (footsteps, edge warning) |
| Downtown | 6–15 | Audio beacons, tactile tiles, wind, wind break |
| Industrial | 16–30 | Cushions, moving platforms |
| Harbor | 31–50 | Fog (limits echolocation), currents |
| Skyline | 51+ | Verticality, multi-path, time dilation |

Each world must award enough stars to unlock the upgrades players need next.

### Daily Route

- Procedural seed from date + player ID.
- 3 modifiers from: wind, ice, fog, narrow, reverse.
- Ranked by points on a global leaderboard, with ghost replay.
- Reward: cosmetic or upgrade token.

## 7. Upgrade System (Persistent)

Every upgrade tier has two requirements. **Stars** act as a gate: the player must have a total number of stars to unlock the tier, and stars are never spent. **Credits** are the spending currency that buys the tier. The star total counts the best result per level, summed, so replaying a level never counts twice. The shop shows the requirement clearly, such as "needs 30 stars" with a progress bar.

| Upgrade | Max level | Effect | Credit cost curve |
| --- | --- | --- | --- |
| Items on Map | 5 | More tools active on the map at once | 100/250/500/1000/2000 |
| Echolocation Range | 4 | +1.5m scan radius per level | 150/400/800/1600 |
| Echolocation Refresh Rate | 4 | −0.25s scan interval per level (base 1.0s) | 150/400/800/1600 |
| Echolocation Slow-Down | 3 | Lv1 unlocks the ledge slow. Lv2 and Lv3 make it stronger and longer | 200/600/1800 |
| Box Capacity | 2 | Start with 4, then 5 crates. Extra crates are bonus-star crates | 500/1500 |
| Prep Time | 3 | +15s prep countdown per level | 200/600/1800 |
| Haptic Sensitivity | 3 | Detects ledges 1 cell earlier per level | 100/300/900 |
| Beacon Duration | 3 | +2s ping duration | 100/300/900 |
| Cargo Insurance | 3 | 1 crate survives loss per run | 300/900/2700 |

Star thresholds per tier are still to be defined and tuned with the world star counts.

**Currency:** Credits, earned per level completed, daily bonus and ad reward.

## 8. Controls & UI

The whole level is always visible in portrait. There is no pan and no zoom. Everything is reachable with one thumb in the lower 60% of the screen.

| Action | Gesture |
| --- | --- |
| Select item | Tap it in the toolbar |
| Place item | Tap a valid spot on the map |
| Move placed item | Press and hold it, then drag to a new spot |
| Cancel placement | Long-press the ghost, or tap outside the map |
| Pause / menu | Top-right button |

**Toolbar (bottom):** shows the level's tools with icon and a cooldown ring under each placed item. Locked objects are greyed out, and tapping one opens the upgrade screen.

**HUD (top):**

- Crate counter (icons: carried, lost)
- Prep countdown, shown only before the boy starts
- Point pool meter, draining as items are moved
- Echolocation status (sweep indicator showing the refresh rate)

## 9. Audio & Haptics (The "Blind" Identity)

| Layer | Implementation |
| --- | --- |
| Footsteps | 4 surface types × 3 speeds × 2 weights (light/loaded) |
| Passive echolocation | Continuous low hum, pitch rises near ledges, spatialized, with occlusion |
| Beacon ping | 880Hz pulse, Doppler if moving |
| Edge warning | Rising Shepard tone, volume = proximity |
| Crate events | Distinct sounds for pickup, drop, loss and delivery |
| Item placement | Satisfying "thock", pitch = object type |
| Ambient | District-specific (traffic, wind, machinery) |

**Haptic patterns (iOS Taptic / Android VibrationEffect):**

| Event | Pattern |
| --- | --- |
| Edge warning | 3 rapid pulses, increasing intensity |
| Crate lost | Heavy double-thud |
| Crates delivered | Crisp triple-tap |
| Item placed or moved | Light tap |
| Beacon ping direction | Left/right motor differential |
| Ledge slow active | Steady slow pulse (1Hz) while slowed |
| Ledge detected | Single sharp tap when the passive scan finds a ledge |

## 10. Accessibility (First-Class)

| Feature | Implementation |
| --- | --- |
| Screen reader | Full UI narration, grid coordinates announced |
| Haptic-only mode | All audio translated to haptics, zero audio required |
| High contrast | 4.5:1 minimum, toggleable outlines |
| Colour blind | Deuteranope, Protanope and Tritanope palettes |
| One-hand mode | Default design. All UI reachable in the lower 60% of the screen |
| Reduced motion | Disable camera shake, parallax and particles |
| Text scaling | 100%–200% Dynamic Type support |
| Audio description | Optional narrator for level events |
| Assist mode | Optional slow-motion or pause-and-place for item placement |

## 11. Monetization

| Vector | Implementation |
| --- | --- |
| IAP | Credit bundles, upgrade bundles, cosmetic packs, no-ads |
| Rewarded ads | Retry a failed level, credit boost, extra prep time |
| Battle pass (seasonal) | Cosmetic trail, exclusive item skins, bonus credits |
| No forced ads | Opt-in only, respects consent |

Stars gate upgrades and cannot be bought, so progression stays skill-based.

## 12. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Blind mechanic too frustrating | Medium | High | Extensive tutorial, generous echolocation, assist modes |
| Tap-and-move controls fiddly on a small tall screen | Medium | High | Large hit targets, snap-to-grid, generous touch tolerance, item cooldown ring |
| Audio/haptics not distinctive enough | Medium | High | Iterative playtesting with blind and low-vision consultants |
| Levels unsolvable or too hard | Low | Medium | Validator pass plus manual curation for the first 50 levels |
| Point pool and star gates poorly balanced | Medium | Medium | Per-level playtesting of pools, tune star thresholds per world |
| Accessibility scope creep | Medium | Medium | Define MVP accessibility first, iterate post-launch |

## 13. Open Questions

- Star thresholds for each upgrade tier.
- Point pool size and per-item move costs for each level.
- Cooldown length per item type.
- Whether Safe Fall Height changes from 3 to 2 after balance testing.
- Launch checklist and technical architecture, moved to a separate tech doc.

*End of Design Document — v2.0*
