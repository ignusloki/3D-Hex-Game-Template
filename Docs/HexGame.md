# Hex Game

Snapshot date: 2026-04-16  
Scope of this document: current playable prototype plus the documented target for the next multi-act transition feature.  
Audience: another chat session focused on game design decisions, not implementation details only.

## 1. Game Summary

This project is a turn-based hex-map caravan survival / route-planning game.

The player controls a caravan trying to cross a procedural map from a start corner on the left side to a goal corner on the right side. The central tension of the game is not tactical combat. The core challenge is:

- read the board
- evaluate resource cost
- decide when to detour
- decide when to commit
- survive changing pressure while moving toward the goal

At a high level, the game is currently about:

- spending `Food` to move
- protecting `Morale`
- using `Gold` as a secondary pressure resource
- reading terrain cost
- using pitstops intelligently
- reacting to dynamically appearing hazards
- dealing with fog-of-war limited obstacle information
- optionally dealing with a late-game `Nemesis` pressure mechanic

The current playable prototype already supports a full run from spawn to goal, including win and lose screens.

The next production step is no longer just "reach the goal once."

The intended run is a 3-act structure with:

- transition screens between acts
- boon selection after Act 1 and Act 2
- stacked boons by Act 3
- Act 3 nemesis activation based on the family locked by the first boon choice

That target flow is documented in:

- `Docs/ActTransitionSystem.md`

## 2. Current Prototype Scope

### 2.1 What is already implemented

- procedural hex map generation
- map size selection: `5x5` or `10x10`
- opposite-corner start and goal placement
- one-step caravan movement
- split resource economy: `Food`, `Morale`, `Gold`
- fog of war
- dynamic obstacle spawning and despawning
- pitstop placement
- pitstop rewards
- pitstop events with choice-based modal UI
- data-driven boon framework with reusable map-generation integration
- win / lose / retry modal
- optional Nemesis system with three archetypes:
  - `Hunter`
  - `Echo`
  - `Corruptor`

### 2.2 What the current run actually is

The long-term design intent is a 3-act structure.

The current prototype does **not** yet play through all 3 acts as a full production flow. Right now:

- the run ends when the player reaches the goal on the current map
- the run also ends if `Food` reaches `0`
- the run also ends if `Morale` reaches `0`
- `Gold` reaching `0` does **not** cause defeat

The `Nemesis` exists now as a configurable testable system that can be enabled in the Unity editor. It is architected as if it belongs to later acts, but can currently be turned on for prototype iteration.

The first runtime slice of the act-transition system now exists:

- reaching the goal in Act 1 or Act 2 can hand off into the next act instead of ending the whole run
- resources can carry between acts with configurable grants
- the run still reloads the same gameplay scene between acts for now

But the full intended act flow is still incomplete because boon selection, act-profile generation, and Act 3 family-locked nemesis activation are not implemented yet.

The next target production flow is:

- Act 1 ends at the goal and leads into a transition screen
- the first transition chooses the Act 2 boon and locks the Act 3 nemesis family
- Act 2 uses a desert-biased procedural map and has no nemesis
- the second transition chooses the Act 3 boon from the locked family
- Act 3 uses a general procedural map and enables the locked-family nemesis

The detailed target for that feature lives in:

- `Docs/ActTransitionSystem.md`

## 3. Core Player Fantasy

The player is not commanding an army. The player is managing a fragile expedition.

The intended fantasy is:

- “I am trying to get this caravan across hostile land.”
- “I do not have enough margin to move carelessly.”
- “The map is never fully stable.”
- “Every detour can save me or ruin me.”
- “Pitstops are opportunities, not guaranteed safety.”
- “I must keep re-evaluating the route.”

The game’s strongest identity in the current prototype is route pressure. It does not come from one single feature. It comes from multiple systems layering together:

- terrain travel cost
- limited food
- morale as a failure resource
- fog hiding obstacle state
- obstacle churn
- pitstop opportunity cost
- event choices
- optional Nemesis pressure

## 4. Current Gameplay Loop

## 4.1 High-level loop

1. A map is generated.
2. The caravan spawns at one corner on the left side.
3. The goal spawns at the opposite corner on the right side.
4. The player inspects nearby terrain and pitstops.
5. The player selects the caravan, previews a one-step move, and commits it.
6. Systems resolve after movement.
7. The player repeats this process until:
   - the caravan reaches the goal, or
   - `Food` reaches `0`, or
   - `Morale` reaches `0`

## 4.2 Per-move loop

The current actual move resolution order is important:

1. The player commits a valid one-hex move.
2. The caravan spends `Food` equal to the destination move cost.
3. The caravan is moved to the destination hex.
4. Fog of war updates.
5. The obstacle system resolves.
6. The Nemesis system resolves.
7. Nemesis same-hex defeat is checked.
8. Goal victory is checked.
9. Pitstop arrival resolves if the caravan landed on a pitstop.
10. Resource defeat is checked.
11. The appropriate HUD state / modal is shown.

This order has a few important consequences:

- The player can make a move that takes `Food` or `Morale` to `0`.
- That move is still allowed to resolve.
- If the player reaches the goal on that move, victory takes priority over normal post-move resource defeat.
- If a pitstop and the Corruptor reach the same tile on the same move, the caravan has priority. The caravan gets the pitstop first, then the Corruptor destroys it afterward for future use.

## 4.3 Target multi-act run flow

Once the act-transition system is implemented, the target loop becomes:

1. Generate and play Act 1.
2. Reach the goal.
3. Show an act transition screen with story text and boon selection.
4. Apply the chosen boon to Act 2 and lock the boon family for Act 3.
5. Carry caravan resources forward and apply any configured between-act grant.
6. Generate and play Act 2 with a desert-biased map profile and no nemesis.
7. Reach the goal.
8. Show a second transition screen with story text and boon selection from the locked family.
9. Carry caravan resources forward again and apply any configured between-act grant.
10. Generate and play Act 3 with the stacked boon loadout.
11. Enable the Act 3 nemesis based on the family locked after Act 1.
12. Reach the goal to end the game.

## 4.4 Act transition responsibilities

The transition screen is expected to handle:

- story / omen text
- boon selection
- act handoff to the next map

The first production slice does not require final cinematic presentation.

## 5. Board and Map Structure

## 5.1 Map sizes

The generator currently supports:

- `5x5`
- `10x10`

The main design target is `10x10`, but `5x5` is supported as a smaller prototype board.

## 5.2 Start and goal placement

Start and goal are currently placed on opposite corners.

The two current possible pairings are:

- `top-left start -> bottom-right goal`
- `bottom-left start -> top-right goal`

This gives the board an immediate diagonal crossing shape.

## 5.3 Terrain biomes

The current terrain biomes are:

- `Grass`
- `Forest`
- `Mountain`
- `Water`
- `Desert`

### Current travel costs

- `Grass`: `1`
- `Forest`: `2`
- `Mountain`: `3`
- `Water`: `3`
- `Desert`: `5`

### Important current rule

All biomes are currently passable.

This includes:

- `Water`
- `Desert`
- `Mountain`

So terrain is a cost layer, not a hard-blocking layer.

## 5.4 Current map-generation style

The current generator is no longer pure per-tile noise. It uses layered logic:

- biome noise / classification
- macro land regions
- feature overlays
- quality checks / rerolls
- special-tile enforcement

This means the board is intended to feel more region-based and less like random scattered single-hex noise.

Special tiles such as the start and goal biome overrides have higher priority than normal terrain-generation rules.

### Current map-generation state

The map generator now goes beyond simple biome biasing.

It is currently good at:

- region shaping
- water / forest / mountain feature biasing
- rerolling weak maps
- stamping exact terrain landmarks such as mini lakes and oases
- reserving placement space for landmarks, pitstops, and map objects
- planning pitstops, outposts, and quest markers through a shared placement pass

### Current refactor direction

The refactor keeps the current layered approach but has restructured it around explicit phases:

- base biome pass
- macro region pass
- feature pass
- authored landmark stamp pass
- special-tile enforcement
- validation / reroll
- map-object placement pass

The target architecture and remaining follow-up work are documented in:

- `Docs/MapGenerationRefactor.md`

## 5.5 Planned act-specific map profiles

The next production flow introduces act-specific generation profiles.

### Act 1

- general procedural profile
- no special theme required

### Act 2

- desert-dominant profile
- forest should be nearly absent: `0` to `2` tiles total is acceptable
- water should stay rare, but not as rare as forest
- mountains remain allowed
- exact tuning should remain editable in the Unity editor

### Act 3

- general procedural profile again
- no special biome theme by default
- boon and future quest effects may still modify generation

## 6. Resources and Failure Conditions

The caravan currently has three tracked resources:

- `Food`
- `Morale`
- `Gold`

## 6.1 Food

`Food` is the main movement fuel.

- moving to a hex spends food equal to that hex’s travel cost
- some obstacles drain food
- some pitstops and events restore food
- if `Food` reaches `0`, the player loses

## 6.2 Morale

`Morale` is a secondary failure resource.

- some obstacles drain morale
- some pitstops and events restore morale
- if `Morale` reaches `0`, the player loses

## 6.3 Gold

`Gold` is currently a strategic secondary resource.

- some events give or consume gold
- some obstacles drain gold
- gold can reach `0` without immediate defeat

## 6.4 Current starting values

The current default starting values from `PlayerController` are:

- `Food = 30`
- `Morale = 3`
- `Gold = 3`

There is also a runtime `Caravan Metrics` child object under `Player Controller` that lets the developer edit these during play mode.

## 7. Controls

## 7.1 Camera controls

Current camera controls are:

- `WASD` or arrow keys: pan
- `Q` / `E`: rotate
- mouse wheel: zoom
- `Space`: reset camera position / zoom / rotation

Camera input is blocked while:

- a pitstop event modal is open
- a win / lose modal is open

## 7.2 Caravan movement input

Current caravan input is click-driven:

1. Click the caravan hex to select the caravan.
2. Click an adjacent hex to preview that move.
3. Click that same hex again to commit the move.

Current movement is strictly one hex at a time.

## 7.3 Inspection behavior

The player can click other tiles to inspect them.

Current selection color behavior:

- green highlight: caravan tile or valid nearby move target
- soft blue highlight: selected tile outside immediate movement range

Current design note:

- visually, fog still affects the board
- but the current HUD tile inspection shows terrain/travel information even for tiles that are not in immediate movement range

So the current fog system is strongest as:

- board visibility pressure
- obstacle visibility pressure
- route readability pressure

rather than a strict “you know nothing about unseen terrain” system.

## 8. UI Summary

## 8.1 Main HUD

The current left HUD includes:

- game title
- current `Food / Morale / Gold`
- status text
- travel-time text
- selected tile details

Tile details currently show:

- coordinates
- terrain
- travel cost
- visible obstacle info if present
- Nemesis tile info if relevant

## 8.2 Pitstop panel

Pitstop information was moved to a dedicated top-right panel.

This panel shows:

- pitstop title
- destroyed state
- refuel state
- repeatable state
- visited state
- event description
- resolved event result summary when relevant

## 8.3 Pitstop event modal

The current pitstop event modal is a centered full-screen black overlay.

It includes:

- event title
- a smaller arrival bonus line below the title
- event description
- clickable choice buttons
- gain/loss summary to the right of each choice
- a result state after the player chooses
- a `Map` button to return to gameplay

## 8.4 Run-state modal

When the run ends, the game shows a simple black modal:

- `You win!` + `Retry`
- `Game over!` + `Retry`

This is intentionally prototype-simple.

## 9. Fog of War

## 9.1 Current intent

Fog of war exists to create uncertainty and to support dynamic obstacle behavior.

## 9.2 Fog knowledge states

Each tile conceptually supports:

- `Unseen`
- `Remembered`
- `Visible`

## 9.3 Current visibility rule

Visibility is recalculated around the caravan position using a vision radius.

The default is:

- `vision radius = 1`

## 9.4 Terrain and special visibility

The fog system supports:

- persistent terrain discovery
- always-known special tiles
- obstacle visibility tied only to current visibility

Pitstops and the goal are treated as always-known specials in the presentation layer.

## 9.5 Obstacle-specific fog interaction

Obstacles only exist visually while visible.

The obstacle system uses fog transitions directly:

- entered visibility
- left visibility
- currently visible tiles

This is one of the main reasons the map keeps feeling unstable from move to move.

## 10. Obstacle System

## 10.1 Role

Obstacles are not walls. They are hazards that tax the caravan if entered.

Their purpose is to force route reevaluation, not to stop movement entirely.

## 10.2 Core obstacle rules

Current core rules:

- base maximum active obstacles: `2`
- obstacle spawn only rolls when at least one hex entered visibility that move
- at most one spawn roll per caravan move
- obstacles are one-time hazards
- obstacles despawn when they leave visibility
- obstacles are shown only while their tile is currently visible

## 10.3 Current spawn location rule

The current implementation is intentionally near the player’s frontier. Obstacles currently spawn only if the candidate tile is:

- currently visible
- newly revealed by that move
- adjacent to the caravan’s current tile
- not water
- not the caravan tile
- not another obstacle
- not start / goal
- not a pitstop
- not a protected hex

This is a stricter and more immediate spawn rule than the earlier “hidden elsewhere on the map” concept.

## 10.4 Pity system

The obstacle system currently includes a pity rule:

- if two eligible spawn rolls fail in a row
- the third eligible attempt is forced to spawn if a valid candidate exists

## 10.5 Current obstacle types

### Wolves

- drains `Food`

### Bandits

- drains `Gold`
- if the caravan has no gold, drains `1 Morale` instead

### Ruined Caravan

- drains `Morale`

## 10.6 Obstacle presentation

Current visible obstacle presentation uses a single model with color variation:

- the `Barbarian` model is used as the current obstacle visual base
- color and scale differentiate obstacle types

## 11. Pitstop System

Pitstops are the game’s most important support nodes on the board.

They are not just refill points. They are strategic anchors.

## 11.1 Current pitstop building types

- `Mill`
- `Wall Tower`
- `Mansion`

## 11.2 Base arrival rewards

- `Mill`: `+3 Food`
- `Wall Tower`: `+1 Morale`
- `Mansion`: `+2 Gold`

## 11.3 Event flow

When the caravan enters a pitstop:

1. base reward is applied
2. a ScriptableObject-driven event is chosen
3. the player sees a modal and makes a choice
4. the result is shown
5. the player closes the modal with `Map`

## 11.4 Current event rules

- event pool is filtered by pitstop type
- selection rules can also filter by:
  - first visit
  - visited pitstop count
  - current food
  - current morale
  - current gold
  - repeatability rules
- options that the player cannot afford are disabled
- events with no affordable options are filtered out

## 11.5 Current pitstop placement logic

Pitstop placement is structured, not random scatter.

### `10x10`

Supports two layouts:

- `4 strategic anchors`
- `7 strategic anchors`

These use progress bands and lane logic to spread pitstops across the map.

### `5x5`

Currently uses:

- `2 pitstops`

The placement system tries to:

- distribute them from early to late route space
- avoid clustering
- avoid spawn / goal adjacency
- create route-planning anchors

### Planned integration note

Pitstops still use their own structured gameplay planner, but their coordinates now flow through the broader shared map-object placement architecture before spawning so they can coexist cleanly with:

- terrain landmarks
- outposts
- quest markers
- future authored points of interest

## 11.6 Destroyed pitstops

Pitstops can now also be destroyed by the `Corruptor` Nemesis.

A destroyed pitstop:

- remains on the map
- is marked visually
- no longer provides reward or event functionality

## 12. Nemesis System

## 12.1 Status

The Nemesis system exists in the current development branch and is intended as a late-run pressure mechanic.

It is designed for the future 3-act structure, but can already be enabled manually for testing in the editor.

In the documented target flow:

- Act 2 has no nemesis
- Act 3 enables the nemesis archetype locked by the first boon choice

## 12.2 Shared Nemesis rules

- always visible
- not hidden by fog
- ignores normal terrain-cost movement
- ignores normal obstacle movement constraints
- designed to create route pressure, not tactical combat

## 12.3 Data model

Nemesis archetype tuning now comes from ScriptableObjects, not just one settings blob.

There are separate profile assets for:

- `Hunter`
- `Echo`
- `Corruptor`

Each profile can currently define:

- movement cadence
- step count per activation
- visuals
- model prefab
- corruption tuning
- destroyed pitstop tuning

## 12.4 Hunter

Fantasy:

- direct pursuer

Behavior:

- starts on an unused corner
- moves toward the caravan
- defeats the player on same-hex contact

Tuning model:

- `Caravan Moves Per Activation`
- `Steps Per Activation`

Example:

- `3 caravan moves per activation`
- `2 steps per activation`

means:

- the Hunter waits for 3 caravan moves
- then moves 2 hexes in one activation

## 12.5 Echo

Fantasy:

- irreversible commitment

Behavior:

- after every caravan move, Echo occupies the hex the caravan just left
- the caravan cannot move back into that hex
- Echo does not defeat by same-hex pursuit

Additional effect:

- raises the obstacle cap from `2` to `3`

## 12.6 Corruptor

Fantasy:

- destroys support before directly pursuing the player

Behavior:

- starts on an unused corner
- targets the nearest undestroyed pitstop first
- leaves corruption behind as it moves
- destroys pitstops on contact
- once all pitstops are destroyed, switches to direct pursuit

### Current corruption behavior

When the Corruptor moves through a hex:

- the hex is marked as corrupted
- the tile is visually converted to a desert tile
- the tile is mechanically treated as `Desert`
- that means it now uses desert travel cost and biome identity

Corrupted tiles currently also contribute to obstacle pressure nearby.

### Contested pitstop rule

If caravan and Corruptor reach the same pitstop on the same move:

- caravan resolves first
- then the Corruptor destroys the pitstop afterward

This was explicitly adjusted so the player gets the pitstop benefit on a tie.

## 13. Win / Lose Rules

The run currently ends under these conditions:

### Victory

- caravan reaches the goal hex

### Defeat

- `Food <= 0`
- `Morale <= 0`
- certain Nemesis contact conditions

Current important edge-case behavior:

- the player is allowed to make a move that takes a resource to `0`
- if that move reaches the goal, victory resolves
- otherwise the run ends in defeat after the move resolution finishes

## 14. Technical Architecture

## 14.1 Main scene objects

The main gameplay runtime is currently split across these scene objects:

- `Map Generator`
- `Player Controller`
- `Player Controller / Caravan Metrics`
- `Pitstop Spawner`
- `Pitstop Event System`
- `Obstacle System`
- `Nemesis System`

## 14.2 Main runtime controllers

### Map / terrain

- `MapGenerator`
- `HexBiomeMapGenerator`
- `HexBiomeMacroRegionPainter`
- `HexBiomeFeaturePainter`
- `HexTerrainLandmarkStampPass`
- `HexMapGenerationContext`
- `HexMapGenerationModifiers`
- `HexMapPlacementReservations`
- `HexMapObjectPlacementPass`

### Player / movement / HUD

- `PlayerController`
- `HexHudPresenter`
- `HexTravelTimePresenter`
- `HexPathHighlighter`

### Fog

- `HexFogOfWarController`
- `HexFogOfWarState`
- `HexFogOfWarPresenter`

### Obstacles

- `HexObstacleController`
- `HexObstacleSpawnPlanner`
- `HexObstaclePresenter`
- `HexObstaclePenaltyResolver`

### Pitstops

- `PitstopSpawner`
- `PitstopSite`
- `PitstopEventController`
- `PitstopEventResolver`
- `PitstopEventModalPresenter`

### Nemesis

- `HexNemesisController`
- `HexNemesisPresenter`
- `HexNemesisSettings`
- `HexNemesisArchetypeProfile`

## 14.3 Data-driven assets

### Terrain data

Current terrain data is driven by `HexScriptableObject` assets in:

- `Assets/Resources/Scriptable Object`

Authored terrain landmark definitions now live in:

- `Assets/Resources/TerrainLandmarks`

Authored map-object definitions now live in:

- `Assets/Resources/MapObjects/Definitions`

### Pitstop events

Pitstop encounter assets live in:

- `Assets/Resources/PitstopEvents`

### Nemesis profiles

Nemesis archetype tuning assets live in:

- `Assets/Resources/NemesisProfiles`

## 15. Current Design Strengths

From a design point of view, the strongest current qualities are:

- strong route-planning identity
- multiple overlapping pressure systems
- pitstops as meaningful anchors
- low-level move-by-move decision pressure
- good support for tuning through ScriptableObjects and serialized settings
- enough systemic interaction to support deeper design iteration

## 16. Current Design Weaknesses / Prototype Gaps

These are important context points for future design discussion:

- replay flow is still minimal
- movement animation is not done
- some board-state readability still needs polish
- the game currently ends after the first map even though the long-term structure is 3 acts
- the multi-act transition flow now has a first runtime slice, but boon selection, act-profile generation, and Act 3 family-locked nemesis activation are still missing
- some map-generation visuals still need cleanup
- the map generator now has a shared generation context, modifier path, terrain-landmark stamp pass, placement reservations, scene-level map-object requests, and a shared map-object placement pass that can plan pitstops, outposts, and quest markers together
- quest markers can now be placed and spawned through the shared map-object pipeline, and they currently use a prototype-only mock interaction that shows placeholder dialog text on arrival; a real quest system does not exist yet
- resource balance is still very tunable and not final

## 17. Current Non-Event Pending Work

Based on the current backlog, the main remaining non-event tasks are:

- implement the documented act-transition system from `Docs/ActTransitionSystem.md`
- add in-game replay flow without stopping Play mode
- continue the map-generation refactor from the current shared placement slice into richer object behaviors, movement-blocking support if ever needed, and additional map-object content
- improve board readability for start, goal, obstacles, fog, and pitstop state
- continue map visual cleanup and biome consistency work
- further balance terrain costs, obstacle pressure, pitstop density, and starting resources
- expand automated tests for the full gameplay loop

## 18. Design Constraints Another Chat Should Know

If another chat is helping with game design, it should assume:

- the game is currently strongest as a route-planning survival prototype
- one-step movement is intentional for now
- all terrain is currently passable
- pressure comes from cost and state change, not tactical combat
- pitstops are a major part of the identity
- fog currently matters most for obstacle information and map readability
- Nemesis is optional right now but intended as a major future act-layer system
- current UI is prototype-simple and should not be mistaken for final presentation ambition

## 19. Short Design Framing

If this game had to be summarized in one sentence for design discussion:

> A procedural hex-route survival game where the player manages a fragile caravan across a shifting board, weighing terrain cost, support opportunities, hidden hazards, and late-run pursuit pressure on every move.
