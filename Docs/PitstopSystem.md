# Pitstop System

Last updated: 2026-05-05

## Purpose

Pitstops are strategic map anchors. They provide resources, offer small
choice-based events, and reward route-planning detours.

## Gameplay Flow

1. The caravan reaches a pitstop hex.
2. `HexTurnResolver` applies the pitstop arrival/recharge result if applicable.
3. `HexRunUiReporter` opens the choice-based event modal.
4. The player selects one available choice.
5. The result state appears in the same modal.
6. The player returns to the map.

## Pitstop Types

- `Mill`: base `+3 Food`
- `Wall Tower`: base `+1 Morale`
- `Mansion`: base `+2 Gold`

## Event Rules

- Events are ScriptableObject assets.
- Event pools filter by pitstop type and run state.
- Choices can change `Food`, `Morale`, and `Gold`.
- Choices that require unavailable resources are disabled.
- Events should include a no-cost fallback so the player is not stuck.
- Revisit behavior exists; repeatability remains a tuning/content decision.

## UI Behavior

Pitstop information now appears in the unified right-side map inspector, not in a
separate top-right panel.

The pitstop inspector shows:

- pitstop name
- pitstop type
- hex coordinates
- terrain
- travel cost
- working/destroyed/refuel/repeatable/visited status
- one short description

The pitstop event modal is an interactive UI Toolkit modal in the shared modal
layer. While it is open:

- gameplay/map input is blocked through `HexGameFlowController`
- HUD/context layers are visually suppressed as needed
- choice buttons are disabled until modal open transition completes
- the choice-to-result swap intentionally does not use a transition

## Technical Components

Primary runtime objects:

- `PitstopSpawner`
- `PitstopSite`
- `PitstopEventController`
- `PitstopEventResolver`
- `PitstopEventModalPresenter`
- `HexHudPresenter`
- `HexTurnResolver`
- `HexRunUiReporter`
- `HexGameFlowController`

`HexTurnResolver` owns the movement-turn decision to process pitstop arrival and
recharge. `PitstopEventResolver` owns the event choice result. `HexRunUiReporter`
bridges those results into the modal presenter, and `HexGameFlowController`
keeps map input blocked while the modal flow is active.

Data assets:

- pitstop events live in `Assets/Resources/PitstopEvents`
- pitstop placement uses the shared map-object placement architecture

## Current Status

Complete as a first playable system:

- placement
- base refill by type
- choice event modal
- event ScriptableObject pool
- unaffordable-choice blocking
- unified inspector integration
- modal input blocking

Open content/system follow-up:

- more event content
- real quest/outpost integration
- deeper non-resource event consequences
- repeatability tuning
- balance pass for rewards and pitstop density
