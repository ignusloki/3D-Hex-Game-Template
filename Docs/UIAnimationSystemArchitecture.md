# UI Animation System Architecture

Last updated: 2026-05-05

## Purpose

The project uses a shared UI Toolkit fade system for screen and modal
transitions. The goal is to avoid duplicated per-screen animation loops while
keeping each presenter responsible for its own content and callbacks.

## Core Runtime

Main runtime types:

- `HexUiTransitionPlayer`
- `HexUiTransitionProfile`
- `HexUiTransitionFadeTrack`
- `HexUiTransitionTargetSet`
- `HexGlobalUiTransitionController`
- `HexMainMenuTransitionService`

## Responsibilities

`HexUiTransitionPlayer`:

- plays opacity tracks against registered `VisualElement` targets
- uses unscaled time
- applies easing
- invokes completion callbacks
- prevents duplicate transition playback on the same player

`HexUiTransitionProfile`:

- stores reusable timing data
- supports runtime cloning
- supports track filtering and reverse fades

`HexGlobalUiTransitionController`:

- owns the full-screen blackout element in the `GlobalTransition` layer
- runs fade-to-black and fade-from-black sequences
- masks scene reloads and map generation
- contributes to input blocking by driving visible transition state; gameplay
  availability is still answered by `HexGameFlowController`

`HexMainMenuTransitionService`:

- provides named menu transition entry points
- handles startup-to-menu, menu-to-gameplay, and return-to-menu transitions through
  the global fade system

## Current Flow Mapping

### Boot To Main Menu

- `HexGameBootstrap` shows blackout immediately.
- Map generation/readiness happens behind blackout.
- `HexMainMenuPresenter` is shown.
- `HexMainMenuTransitionService` fades from black into the main menu.

### Main Menu To Act 1

- New Game disables menu interaction.
- Global fade hides the transition.
- Menu is hidden and gameplay is activated while black.
- `HexGameFlowController` moves from menu/preparing flow into gameplay input
  availability after activation.
- Gameplay fades in.

### Act Complete / Boon Selection

- Screen reveal uses the shared chapter-page transition profile.
- Buttons are locked during transition playback.
- Act-to-act map reload after boon selection is hidden behind global fade.
- The active run session is advanced by `HexRunSessionController`; the animation
  layer only masks the reload.

### Pitstop Modal

- Opening uses `SimpleModalFade`.
- Choice-to-result swap intentionally does not animate.
- Gameplay input is blocked while the modal is active.

### Victory

- Victory overlay opens through the shared modal fade path.
- Continue triggers global fade before scene reload/return flow.
- The new map/menu state is prepared while black.

### Game Over

- Game Over uses the shared transition player with scene-exposed timing settings
  through `HexGameOverTransitionSettings`.
- Retry and return-to-menu are hidden behind global fade.

## Transition Profile Rules

- Profiles own timing, not gameplay decisions.
- Target keys should be stable constants where practical.
- Missing optional targets should warn and continue.
- Buttons should not become interactive until required fade tracks complete.
- Use global fade only for screen-level or scene-like transitions.
- Use modal-local fade for simple modal appearance.

## Non-Goals

The animation system should not:

- own gameplay state
- decide when victory/defeat/act transition happens
- replace `HexGameFlowController` as the phase/input owner
- load scenes directly
- compute layout
- replace UXML/USS
- become a game state machine

## Regression Checklist

- Startup fades from black after map/menu readiness.
- New Game transition hides gameplay activation.
- Act-to-act reloads do not reveal map generation.
- Pitstop modal opens cleanly and result swap remains instant.
- Victory Continue hides reload/return behind global fade.
- Game Over sequence remains editor-tweakable.
- Buttons cannot double-trigger during transitions.
