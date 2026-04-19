# Project Tasks

Last updated: 2026-04-19

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

### 1. Act Transition System

This foundation is now in place and has moved into content-expansion / validation mode.

Scope:

- add a persistent multi-act run state
- transition from Act 1 to Act 2 and from Act 2 to Act 3 without ending the game
- show transition screens with story text and boon selection
- carry resources between acts with editor-configurable between-act grants
- stack boons across acts
- lock the Act 3 boon family and nemesis archetype from the first transition choice
- generate a desert-biased Act 2 map
- generate a general Act 3 map
- enable the locked-family nemesis only in Act 3

Reference:

- `Docs/ActTransitionSystem.md`

Supporting foundation already in place:

- slice 1 is done: shared generation context, generalized modifier bundle, boon integration through that bundle, and initial map-generation debug logging
- slice 2 is done: terrain landmark definition assets, exact terrain landmark stamping, and a scene-level testing hook on `MapGenerator`
- slice 3 is done: generated-map placement reservations and pitstop planning consuming those reservations
- slice 4 is done: shared map-object placement results and a shared placement pass with pitstops flowing through it as the first consumer
- slice 5 is done: actual outpost / quest-marker placement content now runs through the shared placement pass, with sample definition assets, runtime spawning support, and a mock quest-marker arrival dialog for playtesting

Act-transition implementation status:

- slice 1 is done: persistent multi-act run session, transition modal, scene reload between acts, resource carryover, configurable between-act grants, and Act 2 nemesis suppression
- slice 2 is done: act-specific map generation profiles, scene-level act-transition configuration, and desert-fallback support for Act 2 generation
- slice 3 is done: transition boon choice UI, family lock, and multi-boon stacking
- slice 4 is done: Act 3 nemesis activation from the locked family, boon-driven nemesis runtime modifiers, and the first authored Hunter Act 3 boon pool
- current follow-up: author Echo and Corruptor final boon pools, replace the mock quest-marker flow with real quest content, and balance the Hunter Act 3 boon set through playtesting

### 2. Map Generation Follow-Up

The map-generation refactor is complete enough for the act-transition feature.

Remaining follow-up work on top of that foundation is still valuable, but it is no longer the primary blocker.

## Remaining Non-Event Tasks

### Core Presentation / Flow

1. Implement the multi-act transition flow from `Docs/ActTransitionSystem.md`.
2. Add in-game replay flow so the player can restart or regenerate without stopping Play mode.
3. Replace temporary caravan and goal visuals with better placeholder or final assets.
4. Improve board readability for:
   - start
   - goal
   - visible obstacles
   - fog states
   - visited / exhausted pitstops if needed later

### Map / Terrain

5. Add richer behaviors on top of the shared map-object architecture:
   - interactive outposts
   - real quest logic beyond the current mock quest-marker dialog
   - per-marker quest data / rewards / completion state
   - optional movement-blocking support if the design ever needs it
6. Add a cleanup / smoothing pass for the remaining odd biome scraps.
7. Refine biome prefab grouping so forest, grass, mountain, and water look more consistent.
8. Revisit map validation later if new gameplay systems change what qualifies as a good map.

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
- The map-generation refactor is now through the shared placement and mock quest-marker slice described in `Docs/MapGenerationRefactor.md`.
- The next design source of truth for production progression is `Docs/ActTransitionSystem.md`.
