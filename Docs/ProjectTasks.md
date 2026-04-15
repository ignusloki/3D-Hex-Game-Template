# Project Tasks

Last updated: 2026-04-15

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

### 1. Map Generation Refactor

This is the next active feature to build.

Scope:

- preserve the current terrain-generation rules and map feel
- introduce a broader generation modifier path that is not limited to pitstops
- support boon-driven terrain tuning such as more or less of a biome
- support authored terrain landmarks such as mini lakes and oases
- support spawned map objects such as outposts and quest markers
- avoid rewriting the whole gameplay loop

Reference:

- `Docs/MapGenerationRefactor.md`

Current implementation progress:

- slice 1 is done: shared generation context, generalized modifier bundle, boon integration through that bundle, and initial map-generation debug logging
- slice 2 is done: terrain landmark definition assets, exact terrain landmark stamping, and a scene-level testing hook on `MapGenerator`
- slice 3 is done: generated-map placement reservations and pitstop planning consuming those reservations
- slice 4 is done: shared map-object placement results and a shared placement pass with pitstops flowing through it as the first consumer
- slice 5 is done: actual outpost / quest-marker placement content now runs through the shared placement pass, with sample definition assets and runtime spawning support

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

4. Add richer behaviors on top of the shared map-object architecture:
   - interactive outposts
   - quest logic
   - optional movement-blocking support if the design ever needs it
5. Add a cleanup / smoothing pass for the remaining odd biome scraps.
6. Refine biome prefab grouping so forest, grass, mountain, and water look more consistent.
7. Revisit map validation later if new gameplay systems change what qualifies as a good map.

### Balance

10. Balance the run economy around the current systems:
   - terrain travel costs
   - starting food
   - starting morale
   - starting gold
   - obstacle pressure
   - pitstop density
   - event payouts

### Testing / Stability

11. Expand automated tests for the full non-event gameplay loop:
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
4. Additional terrain-landmark content once the refactor architecture is in place.

## Notes

- Pitstop event system is considered a complete first playable slice.
- Win / lose / retry UI is now in place as a complete first prototype slice.
- Future event work is now polish / expansion, not a core missing system.
- The current branch is already on the first slice of the map-generation refactor described in `Docs/MapGenerationRefactor.md`.
