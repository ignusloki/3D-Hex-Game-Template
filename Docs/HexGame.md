# Hex Game

Last updated: 2026-05-04

## Game Summary

This project is a turn-based hex-map caravan survival and route-planning game.

The player guides a caravan across a procedural map by reading terrain, spending
resources, choosing detours, using pitstops, and surviving escalating pressure.
The game is currently built as a single-scene Unity 6 prototype.

## Current Playable Flow

1. Game boots into the main menu.
2. New Game starts Act 1.
3. The caravan travels from the start side of the map toward the goal.
4. Reaching the goal in Act 1 opens the act-complete/boon transition.
5. The game generates Act 2 behind a global fade.
6. Reaching the goal in Act 2 opens the second act-complete/boon transition.
7. The game generates Act 3 behind a global fade.
8. Reaching the goal in Act 3 opens Victory.
9. Food or Morale reaching zero, or certain Nemesis contact rules, opens Game Over.

The game remains one scene. Menu, gameplay, retry, return-to-menu, and act-to-act
flows are handled by scene reload/state bootstrapping rather than separate scenes.

## Core Systems

Implemented:

- procedural hex map generation
- `5x5` and `10x10` map support
- start and goal placement
- one-step caravan movement
- click-to-inspect map interaction
- persistent selected-hex highlight
- `Food`, `Morale`, and `Gold`
- fog of war
- dynamic obstacles
- pitstops and pitstop events
- shared map-object placement pipeline
- data-driven boons
- multi-act run state
- Act 3 Nemesis activation from locked boon family
- main menu
- Victory and Game Over overlays
- shared UI Toolkit architecture
- shared UI transition system

## Resources

- `Food`: movement pressure. Reaching zero can cause defeat after move resolution.
- `Morale`: survival pressure. Reaching zero can cause defeat after move resolution.
- `Gold`: secondary event/resource pressure. Reaching zero does not directly defeat
  the player.

Important edge case:

- if a move reaches the goal and also drops a defeat resource to zero, goal
  resolution wins for that move.

## Acts And Boons

The current run structure has three acts:

- Act 1: starting map, no locked-family Nemesis pressure.
- Act 2: desert-biased map, no Nemesis.
- Act 3: final map, locked-family Nemesis enabled.

Between acts:

- current resources carry forward
- configured between-act grants are applied
- the player selects one boon
- Act 1 -> Act 2 choice locks the Act 3 family
- Act 2 -> Act 3 choice is filtered by the locked family

Currently authored final-act boon content is strongest for Hunter. Echo and
Corruptor final boon pools remain content follow-up.

## Map Generation

The map generator uses layered terrain generation plus shared runtime modifiers.

Current generation supports:

- act-specific map profiles
- biome odds and fallback biome tuning
- terrain landmarks
- map-object placement reservations
- shared placement for pitstops and other map objects
- scene/debug map-object requests

See `Docs/MapGenerationRefactor.md` for the current architecture summary.

## Pitstops

Pitstops are placed points of interest:

- `Mill`: food-oriented
- `Wall Tower`: morale-oriented
- `Mansion`: gold-oriented

Pitstop arrival can grant a base resource reward and open a choice event modal.
Pitstop details now appear in the unified right-side map inspector.

See `Docs/PitstopSystem.md` for details.

## Dynamic Obstacles

Obstacles are turn pressure objects tied to movement and fog of war.

Current obstacle rules:

- obstacle spawn rolls happen only after the player completes a move
- obstacle spawn candidates must be newly discovered by that move
- obstacles must not spawn on previously revealed or remembered hexes
- obstacles despawn when they leave the player's current line of sight
- normal active obstacle cap is `2`
- the visibility-radius boon raises the normal active obstacle cap to `4`
- Echo Nemesis pressure can independently raise the active obstacle cap through
  its archetype profile
- obstacles must not spawn on water, impassable terrain, the caravan hex, the
  start hex, the goal hex, pitstops, the Nemesis actor hex, or another active
  obstacle

## Nemesis

The Nemesis system supports three archetypes:

- `Hunter`
- `Echo`
- `Corruptor`

Act 3 can resolve the active archetype from the family locked by the first boon
choice. Runtime boon modifiers can affect Nemesis movement cadence, spawn
position, and visibility rules.

## UI

The UI is implemented in UI Toolkit through one shared gameplay UI root.

Major UI surfaces:

- main menu
- top map status bar
- right-side unified map inspector
- pitstop modal
- act-complete intermission
- boon selection
- Victory overlay
- Game Over overlay
- global fade/blackout transition layer

See:

- `Docs/UIArchitecture.md`
- `Docs/UIAnimationSystemArchitecture.md`

## Current Prototype Gaps

Remaining design/production gaps:

- Echo and Corruptor final boon content
- real quest-marker system beyond mock interaction
- interactive outpost behavior
- stronger start/goal/obstacle/fog visual readability
- caravan movement animation
- resource and event balance
- expanded automated test coverage
