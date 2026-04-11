# Project Tasks

Last updated: 2026-04-11

## Current State

These systems are already in place and count as part of the current core loop:

- hex map generation with `5x5` and `10x10` support
- caravan movement with one-hex step selection flow
- start and goal placement
- supplies split into `Food`, `Morale`, and `Gold`
- fog of war
- dynamic obstacle spawning and management
- pitstop placement
- pitstop rewards
- pitstop choice events driven by ScriptableObjects

The earlier “move-by-move route reevaluation pressure” task is considered done through the combination of:

- fog of war
- obstacles
- pitstops and event choices

The movement animation task is intentionally deferred for later.

## Current Priority

### 1. Replay Flow

This is the next active feature to build.

Scope:

- restart current run without stopping Play mode
- regenerate a fresh map without stopping Play mode
- decide whether restart preserves the current seed or not
- keep the flow simple and readable for prototype use

## Remaining Non-Event Tasks

### Core Presentation / Flow

1. Add in-game replay flow so the player can restart or regenerate without stopping Play mode.
2. Replace temporary caravan and goal visuals with better placeholder or final assets.
3. Improve board readability for:
   - start
   - goal
   - visible obstacles
   - fog states
   - visited / exhausted pitstops if needed later

### Map / Terrain

4. Add a cleanup / smoothing pass for the remaining odd biome scraps.
5. Refine biome prefab grouping so forest, grass, mountain, and water look more consistent.
6. Revisit map validation later if new gameplay systems change what qualifies as a good map.

### Balance

7. Balance the run economy around the current systems:
   - terrain travel costs
   - starting food
   - starting morale
   - starting gold
   - obstacle pressure
   - pitstop density
   - event payouts

### Testing / Stability

8. Expand automated tests for the full non-event gameplay loop:
   - movement flow
   - fog transitions
   - obstacle spawn / despawn / contact
   - victory / defeat
   - spawn / goal validity

## Deferred Improvements

These are useful, but not current blockers:

1. Caravan movement animation between hexes.
2. Additional board polish and stronger UI visuals.
3. More advanced non-resource consequences for events.
4. Further biome generation polish beyond the current region-based system.

## Notes

- Pitstop event system is considered a complete first playable slice.
- Win / lose / retry UI is now in place as a complete first prototype slice.
- Future event work is now polish / expansion, not a core missing system.
- The next branch should focus on replay flow unless priorities change.
