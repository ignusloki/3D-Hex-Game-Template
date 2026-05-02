# Map Generation Architecture

Last updated: 2026-05-02

## Purpose

This document describes the current map-generation architecture. The earlier
refactor work is complete enough for the current game loop and act system.

## Current Architecture

The map generator keeps the original layered terrain approach and adds a shared
runtime context for feature systems.

Current generation layers include:

- base biome sampling
- macro land-region painting
- feature overlays
- quality rerolls
- start and goal placement
- act-specific profile overrides
- terrain landmark stamping
- shared placement reservations
- shared map-object placement pass

## Core Runtime Types

- `MapGenerator`
- `HexBiomeMapGenerator`
- `HexBiomeMacroRegionPainter`
- `HexBiomeFeaturePainter`
- `HexMapGenerationContext`
- `HexMapGenerationModifiers`
- `HexTerrainLandmarkDefinition`
- `HexTerrainLandmarkStampPass`
- `HexMapPlacementReservations`
- `HexMapObjectPlacementPass`

## Modifier Flow

`HexMapGenerationModifiers` is the shared bundle for generation changes from:

- act profiles
- boons
- scene/debug requests
- future scenario rules

Terrain generation and pitstop/map-object placement consume the same context so
new systems do not need to patch the generator with one-off hooks.

## Placement Flow

The map-object placement architecture supports:

- pitstops
- outposts
- quest markers
- future authored map objects

The placement pass respects reservations for:

- start tile
- goal tile
- terrain landmarks
- pitstops
- other placed map objects

## Current Content State

Implemented:

- pitstop placement through the shared placement pipeline
- terrain landmark definition assets
- exact terrain landmark stamping
- sample outpost and quest-marker definitions
- runtime spawning of non-pitstop map objects
- mock quest-marker arrival interaction for testing

Not yet production-complete:

- real quest data and quest progression
- interactive outpost behavior
- landmark rotation/mirroring
- shipped boon content that meaningfully uses terrain landmarks or map-object requests
- optional movement-blocking map objects

## Act Integration

The act system can select act-specific map profiles:

- Act 1: general starting map
- Act 2: desert-biased map
- Act 3: general/final map with locked-family nemesis support

Act-to-act map reloads are hidden behind the shared global UI fade.

## Design Constraints

- Keep all biomes passable unless explicitly changed.
- Terrain continues to define travel cost.
- Start and goal remain highest-priority placements.
- Pitstops remain important route anchors.
- Preserve both `5x5` and `10x10` support.
- Do not replace the current generator wholesale without a specific design reason.
