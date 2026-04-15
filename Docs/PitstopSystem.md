# Pitstop System

## Purpose

Pitstops are strategic anchor points on the hex map. They serve three gameplay roles:

- give the caravan a guaranteed base resource refill on arrival
- present a small event with player choice
- shape route planning by rewarding detours and mid-run decision making

This document records the current implementation, authoring workflow, gameplay rules, and the remaining follow-up tasks for the feature.

## Gameplay Summary

### What a pitstop is

A pitstop is a placed point-of-interest on the map represented by one of these building types:

- `Mill`
- `Wall Tower`
- `Mansion`

Each pitstop sits on a valid terrain hex and is visible on the map as a special location.

### Current player flow

1. The caravan reaches a pitstop hex.
2. The pitstop immediately grants its base arrival reward.
3. A choice-based event modal opens.
4. The player selects one available choice.
5. The modal stays open and shows the result.
6. The player clicks `Map` to close the modal and return to gameplay.

### Base pitstop rewards

These are the current automatic arrival rewards:

- `Mill`: `+3 Food`
- `Wall Tower`: `+1 Morale`
- `Mansion`: `+2 Gold`

### Event choice rules

- Events are chosen from a ScriptableObject pool.
- The pool is filtered by pitstop type first.
- The event system then applies selection rules based on current run state.
- Each event choice can currently change `Food`, `Morale`, and `Gold`.
- If a choice would require a resource the caravan does not currently have, that choice is disabled.
- If an event has no affordable choices, it is filtered out and not selected.
- Each event should include at least one no-cost fallback choice so the player is never stuck.

### Current repeatability behavior

- Pitstop arrival rewards are effectively one-time in the current setup.
- The current event pool is also intended to behave as one-time-per-run unless explicitly configured otherwise.
- Revisit behavior exists, but the long-term repeatability rule is still a design follow-up item.

## UI Summary

### Left HUD

The left HUD currently shows:

- game title
- current `Food / Morale / Gold`
- status line
- travel time
- terrain and tile details
- visible obstacle info when relevant

It no longer shows:

- passable text
- bottom helper copy from the earlier prototype

### Top-right pitstop panel

Pitstop details were moved out of the tile-details panel into their own top-right panel.

This panel is used to show:

- pitstop title
- visited state
- repeatable state
- event description
- resolved event outcome after selection

### Event modal

The event modal is runtime-built and currently has this structure:

- centered on screen
- black full-screen overlay
- event title
- smaller arrival reward label below the title
- event description
- choice buttons
- per-choice gain/loss summary to the right of each option
- result state after choice
- `Map` button to close

### Input blocking

While the modal is open:

- caravan tile input is blocked
- camera input is blocked

## Technical Overview

### Main scene objects

These are the primary runtime scene objects involved in the pitstop feature:

- `Pitstop Spawner`
- `Pitstop Event System`
- `Player Controller`
- `Player Controller/Caravan Metrics`

### Main data flow

1. `PitstopSpawner` requests a shared map-object placement plan after terrain generation.
2. That plan now contains both pitstops and any requested non-pitstop map objects.
3. `PitstopSpawner` spawns the pitstop sites from that shared plan.
4. `PitstopEventController` initializes with those spawned sites.
5. `PlayerController` detects arrival on a pitstop tile.
6. `PitstopEventResolver` applies the base arrival reward.
7. `PitstopEventController` selects a matching event asset.
8. `PitstopEventModalPresenter` shows the choice UI.
9. On selection, the resolver applies the option effects.
10. The HUD updates and gameplay resumes after `Map` is clicked.

### Architecture note

Pitstops still keep their own gameplay logic, but their coordinates now come from the shared map-object placement pipeline before spawning.

That broader placement architecture lets pitstops coexist more cleanly with:

- authored terrain landmarks
- outposts
- quest markers
- future map objects driven by boons or scenario rules

This direction is documented in:

- `Docs/MapGenerationRefactor.md`

## Key Scripts

### Placement and pitstop site data

- [PitstopSpawner.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopSpawner.cs)
- [PitstopPlacementPlanner.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopPlacementPlanner.cs)
- [PitstopPlacementSettings.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopPlacementSettings.cs)
- [PitstopSite.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopSite.cs)

### Event system

- [PitstopEventController.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEventController.cs)
- [PitstopEventResolver.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEventResolver.cs)
- [PitstopEventRuntime.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEventRuntime.cs)
- [PitstopEventDefinition.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEventDefinition.cs)
- [PitstopEncounterAsset.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEncounterAsset.cs)
- [PitstopEventModalPresenter.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PitstopEventModalPresenter.cs)

### Player and resource integration

- [PlayerController.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/PlayerController.cs)
- [CaravanResourceState.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/CaravanResourceState.cs)
- [CaravanMetricsController.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/CaravanMetricsController.cs)
- [HexHudPresenter.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/HexHudPresenter.cs)
- [HexCameraController.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Scripts/Hex/HexCameraController.cs)

### Tests

- [PitstopEventResolverTests.cs](/Users/suporte/3D-Hex-Game-Template/Assets/Tests/EditMode/Editor/PitstopEventResolverTests.cs)

## ScriptableObject Event Authoring

### Event asset location

Current encounter assets live in:

- [Assets/Resources/PitstopEvents](/Users/suporte/3D-Hex-Game-Template/Assets/Resources/PitstopEvents)

### Current sample assets

- `Mill_GrainLedger`
- `Mill_NightOven`
- `WallTower_Watchfire`
- `WallTower_SignalFire`
- `Mansion_PatronsTable`
- `Mansion_AuctionHall`

### Authoring fields

Each encounter asset currently supports:

- `eventId`
- `pitstopKind`
- `rarity`
- `tags`
- `authoringNotes`
- `selectionWeight`
- `selectionRules`
- `title`
- `description`
- `options`

### Selection rules currently supported

- `requireFirstVisit`
- `allowRepeatSelectionInRun`
- `minimumVisitedPitstops`
- `maximumVisitedPitstops`
- `minimumFood`
- `maximumFood`
- `minimumMorale`
- `maximumMorale`
- `minimumGold`
- `maximumGold`

### Choice authoring rules

Each option currently contains:

- button label
- outcome text
- a list of resource effects

Each option should be authored with these rules in mind:

- negative values mean resource cost
- positive values mean reward
- avoid creating events where every option requires payment
- always provide at least one affordable fallback option

## Resource Editing During Play Mode

### Caravan metrics object

A dedicated child object exists under `Player Controller`:

- `Caravan Metrics`

It exists so the developer can change live caravan values during play mode in the Inspector.

### What it can edit

- `Food`
- `Morale`
- `Gold`

### Safety note

The live metrics editor applies inspector changes with a short delay so temporary blank numeric states do not immediately trigger defeat while typing.

## Current Design Constraints

- Pitstops are strategic anchors, not random bonus dots.
- Base reward happens before the event choice.
- Pitstop events are designed to be short, readable, and low-friction.
- The current event system is intentionally lightweight and resource-driven.
- More advanced non-resource consequences are not required for the current milestone.

## Pending Follow-up Tasks

These are not core blockers anymore. They are improvements and follow-up work.

1. Add clearer board feedback for pitstop state:
   - unvisited
   - visited
   - exhausted

2. Decide the long-term repeatability rule:
   - one-time only
   - cooldown
   - reusable pools

3. Add more authored event assets per pitstop type to avoid repetition.

4. Balance the event pool and selection rules against the real run economy:
   - food pressure
   - morale pressure
   - gold pressure
   - obstacle frequency

5. Improve result presentation if needed:
   - icons
   - clearer consequence formatting
   - better distinction between arrival reward and event result

6. Expand automated tests for:
   - full modal flow
   - revisit behavior
   - disabled options
   - event selection filtering
   - state transitions after choice resolution

7. If needed later, add non-resource consequences such as:
   - scouting
   - obstacle mitigation
   - route reveal
   - temporary movement or safety bonuses

## Quick Reference

### Core essence complete

The current pitstop feature already supports:

- map placement
- base refill by building type
- choice-based event modal
- ScriptableObject event pool
- selection rules by run state
- blocked unaffordable options
- top-right pitstop info panel
- play-mode live resource editing

That means the pitstop system is already complete as a first playable feature slice.
