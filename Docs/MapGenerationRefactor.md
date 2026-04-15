# Map Generation Refactor Architecture

Last updated: 2026-04-15

## Purpose

This document defines the target architecture for the next map-generation refactor.

The goal is not to replace the current generator from scratch.

The goal is to preserve the current playable rules while making the system flexible enough to support:

- biome-odds tuning from boons or future scenario rules
- explicit terrain landmarks such as mini lakes and oases
- custom spawned map objects such as outposts or quest markers
- future act-based map modifiers without hardcoding one-off cases

## Strict Assessment

### What should stay

The current layered terrain generator is a good foundation and should stay:

- base biome sampling
- macro land regions
- feature overlays
- quality rerolls
- start / goal biome enforcement

This is already more structured than a pure random-noise generator and is worth extending.

### What is not good enough

The current system is not expressive enough for planned boon and landmark work because:

- boon map modifiers currently only support extra pitstops
- terrain tuning is mostly threshold-driven, not exposed as a reusable modifier bundle
- water blobs are random compact features, not explicit authored landmarks
- pitstops are placed by a specialized post-generation system instead of a broader map-placement architecture
- tile occupancy is too simple to represent multiple non-blocking map objects safely

### Recommendation

Do not switch to a completely different map-generation approach.

Do not rewrite the whole game.

Refactor the current generator into a clearer pipeline with explicit data passed between phases.

That is the lowest-risk path and keeps compatibility with the current prototype.

## Design Constraints

### Preserve current gameplay assumptions

The refactor must preserve these current rules unless explicitly changed later:

- all biomes remain passable
- terrain continues to define travel cost
- start and goal still have highest placement priority
- pitstops remain important route anchors
- current `5x5` and `10x10` support remains intact

### Do not describe hex landmarks as square grids

Future requests such as `2x2 lake` or `4x4 oasis` should not be implemented literally as square-grid logic.

This is a hex game.

The architecture should define landmark footprints as explicit hex patterns rather than square dimensions.

Examples:

- `MiniLake4`: a 4-hex compact water cluster
- `OasisCore`: a compact water cluster with a surrounding desert ring
- `QuestOutpostSmall`: a one-hex or multi-hex reserved placement footprint for visuals

## Target Architecture

### 1. Generation context

Add a single runtime context object passed into map generation.

Suggested runtime type:

- `HexMapGenerationContext`

It should carry:

- map size
- resolved seed
- base generation settings
- special tile settings
- act / boon generation modifiers
- optional scenario directives

This becomes the main contract between the boon system and the generator.

### 2. Generation phases

The target terrain pipeline should be split into explicit phases.

#### Phase A: base terrain pass

Responsibilities:

- generate initial biome map from noise and thresholds

Input:

- base biome settings
- threshold modifiers
- biome enable / disable modifiers

#### Phase B: macro region pass

Responsibilities:

- reshape land regions to avoid noisy scatter

Input:

- region settings
- modifier multipliers or deltas if needed later

#### Phase C: feature pass

Responsibilities:

- paint random compact biome features such as water or forest blobs
- paint mountain ridges

Input:

- feature settings
- modifier multipliers for counts and ratios

#### Phase D: authored landmark stamp pass

Responsibilities:

- place exact biome landmarks defined by asset-driven footprints
- examples: mini lakes, oasis clusters, corrupted ground, sacred groves

Input:

- list of `HexTerrainLandmarkPlacementRequest`
- list of available `HexTerrainLandmarkDefinition` assets

This is the new phase the current system does not have.

#### Phase E: special tile enforcement pass

Responsibilities:

- force start / goal biome rules
- enforce any protected special tiles

#### Phase F: terrain validation and reroll pass

Responsibilities:

- run current quality validation
- reject impossible or poor terrain layouts before object placement

#### Phase G: map-object placement pass

Responsibilities:

- place pitstops
- place outposts
- place quest markers
- place future non-terrain landmarks

Input:

- placement rules
- placement reservations
- generation context

This pass should operate on the final terrain result rather than directly on raw noise output.

#### Phase H: scene spawn pass

Responsibilities:

- instantiate tile visuals
- instantiate object prefabs on chosen placements
- preserve access to runtime metadata for HUD and gameplay systems

## Data Model Changes

### A. Generalized map modifier bundle

Replace the current narrow map modifier model with a broader generation modifier bundle.

Suggested runtime type:

- `HexMapGenerationModifiers`

It should be able to carry values such as:

- biome threshold deltas
- biome feature count multipliers
- biome feature size multipliers
- landmark placement requests
- extra pitstop count
- future object placement requests

This should be the runtime output consumed from boon selection, scenario setup, or act progression.

### B. Terrain landmark definitions

Add an authoring asset type for exact biome stamps.

Suggested asset type:

- `HexTerrainLandmarkDefinition : ScriptableObject`

Suggested fields:

- `Id`
- `DisplayName`
- `FootprintPattern`
- `BiomeAssignments`
- `PlacementRules`
- `MinDistanceFromStart`
- `MinDistanceFromGoal`
- `AllowedBaseBiomes`
- `ForbiddenBiomes`
- `CanRotate`
- `CanMirror`
- `Weight`

This lets designers author a mini lake or oasis as a reusable landmark asset instead of burying the rule in code.

### C. Object placement definitions

Add a separate authoring asset type for non-terrain spawned objects.

Suggested asset type:

- `HexMapObjectDefinition : ScriptableObject`

Suggested fields:

- `Id`
- `DisplayName`
- `Prefab`
- `FootprintPattern`
- `PlacementRules`
- `VisualHeightOffset`
- `BlocksPlacementOfOtherObjects`
- `BlocksMovement` only if needed later
- `GameplayTags`

This should support objects such as:

- outposts
- quest markers
- relic sites
- future narrative landmarks

### D. Placement reservations

Do not overload runtime tile occupancy for map-generation placement.

Current `HexTileData.IsOccupied` is too narrow because it mainly represents runtime unit occupation.

Instead add a dedicated generation-time reservation model.

Suggested runtime type:

- `HexMapPlacementReservations`

It should track separate placement layers such as:

- start / goal protected tiles
- terrain landmark reserved tiles
- pitstop reserved tiles
- object reserved tiles
- optional blocked-for-placement-only tiles

This avoids breaking pathfinding and unit movement when decorative or interactive objects are added.

## How Pitstops Fit The New Architecture

Pitstops should remain a special gameplay system, but their coordinates should eventually come from the shared map-object placement phase rather than a completely isolated post-process.

Recommended migration path:

1. Keep `PitstopPlacementPlanner` and `PitstopSpawner` for now.
2. Make them consume shared placement reservations.
3. Later move pitstop placement under the common map-object placement pipeline.

This keeps current gameplay stable while preparing for landmarks and outposts.

## How Boons Fit The New Architecture

### Current problem

The current boon system can only express extra pitstops as map modification.

That is too narrow for:

- more water
- less desert
- spawned lakes
- spawned quest outposts

### Target solution

Boons should author data into the generalized generation modifier bundle.

Examples:

- `+25% water feature size`
- `-0.03 water threshold`
- `place 2 MiniLake4 landmarks`
- `spawn 1 quest outpost in mid-progress band`

This keeps boon logic declarative instead of hardcoded into terrain generation.

## What The Current Code Already Supports Well

The current code already provides useful foundations:

- `HexBiomeMapGenerator` is a clear central terrain entry point
- `HexBiomeFeaturePainter` already paints compact biome blobs
- `MapGenerator` already turns biome output into runtime tile views
- `PitstopSpawner` already proves post-terrain prefab placement works

These are strong enough to build on.

## What Must Change First

The first refactor slice should be:

1. Introduce `HexMapGenerationContext`.
2. Replace the current pitstop-only modifier model with `HexMapGenerationModifiers`.
3. Allow the terrain generator to consume modifier deltas for biome thresholds and feature counts.
4. Add a terrain landmark stamp pass with a minimal landmark-definition asset.
5. Add placement reservations so terrain landmarks, pitstops, and future objects can coexist safely.

That is the minimum architecture needed before implementing new biome-spawn boons.

## What Can Wait

These parts can be deferred until after the first refactor slice:

- migrating pitstops fully into the shared placement pipeline
- sophisticated landmark rarity tables
- multi-act generation profiles
- complex quest-object behaviors
- movement-blocking objects

## Expected Outcome

After this refactor, the project should be able to support without hacks:

- “increase water generation”
- “reduce mountain generation”
- “place two mini lakes”
- “stamp an oasis landmark”
- “spawn an outpost in the mid band”
- “spawn a quest marker near a pitstop but not on it”

That is the design target.
