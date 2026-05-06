# UI Architecture

Last updated: 2026-05-05

## Purpose

This document describes the current runtime UI architecture. The migration from
legacy gameplay UGUI and multiple ad-hoc UI Toolkit documents is complete.

## Runtime Model

The game uses one shared UI Toolkit root for gameplay-facing UI:

- `HexGameplayUiRootController`

This controller owns the shared `UIDocument`, `PanelSettings`, root UXML, and
named layer containers. Feature presenters bind their UXML into those layers
instead of creating separate runtime UI roots.

## Layer Model

Current layers:

1. `Hud`
   - top status bar
   - run context
   - selected-hex status
   - resource display

2. `Context`
   - right-side unified map inspector
   - non-modal contextual UI

3. `Menu`
   - main menu screen
   - blocks gameplay interaction while visible

4. `Modal`
   - pitstop event modal
   - act-complete intermission
   - boon selection
   - victory overlay
   - game over overlay

5. `GlobalTransition`
   - full-screen blackout/fade layer
   - used to hide scene reloads and major flow changes

6. `Debug`
   - debug-only UI when needed

## Boot Ownership

`HexGameBootstrap` is responsible for early setup:

- applies startup resolution/fullscreen settings
- resolves core scene references
- prepares the global blackout before the user sees gameplay
- waits for map/pitstop readiness
- shows the main menu by default
- can boot directly into gameplay after scene reload requests

The game remains a single-scene flow. Returning to menu reloads the same scene and
boots back into the menu state.

Act-to-act scene reloads keep active run data through `HexRunSessionController`.
UI boot code should read the active flow/session state rather than treating scene
reloads as implicit run ownership.

## Runtime Flow Ownership

High-level UI blocking and gameplay availability are controlled by
`HexGameFlowController`, backed by `HexRunState` phase changes. Gameplay and
camera input should ask the flow controller whether input is allowed.

`HexRunUiReporter` is the adapter between gameplay results and UI presenters. It
translates `HexTurnResolutionResult` and run lifecycle results into HUD text,
pitstop modals, act-transition modals, victory overlays, and game-over overlays.
The reporter does not own the presenters' UXML/USS layout and does not compute
gameplay results.

## Feature Presenters

Current presenter ownership:

- `HexHudDocumentController`: top HUD and shared UI root access
- `HexHudPresenter`: gameplay status and inspector data formatting
- `HexMainMenuPresenter`: main menu UI and button callbacks
- `PitstopEventModalPresenter`: pitstop choice/result modal
- `HexActTransitionModalPresenter`: act-complete and boon-selection flow
- `HexRunStateModalPresenter`: Victory and Game Over overlays
- `HexGlobalUiTransitionController`: global blackout/fade layer

Feature presenters own content and callbacks. They should not own global game
state. Gameplay code should route state/result presentation through
`HexRunUiReporter` instead of duplicating HUD/modal decision trees.

## Layout Rules

- UXML owns hierarchy.
- USS owns layout, spacing, color, typography, and state classes.
- C# binds data, toggles visibility/classes, and forwards user callbacks.
- Internal UI layout should use flex layout.
- Absolute positioning is reserved for full-screen overlays and root anchors.

## Input Rules

- Passive HUD/context layers use ignored picking unless they contain explicit
  controls.
- Modal, menu, and global-transition layers own input while active.
- Gameplay and camera input are blocked through `HexGameFlowController` while
  modals, menu, or full-screen transitions are active.
- Buttons are disabled during transition playback when interaction would cause
  duplicate actions.

## Current Map UI

The old bottom-left tile inspector and top-right pitstop panel are removed.

The map screen now uses:

- larger top status bar
- click-to-inspect state
- persistent selected-hex highlight
- unified right-side inspector
- structured inspector states for biome, pitstop, generic context, and empty state

`Docs/Example.png` remains as the visual reference for the final map UI check.

## Regression Checklist

- Main menu appears on boot.
- New Game hides menu and activates gameplay.
- HUD/context are hidden while menu is active.
- Pitstop modal blocks gameplay input.
- Act transition modals block gameplay input.
- Victory and Game Over hide/suppress normal gameplay HUD/context.
- Return-to-menu and retry/restart flows are hidden by global fade.
- No gameplay UI flow depends on legacy scene UGUI objects.
