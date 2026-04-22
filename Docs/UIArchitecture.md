# UI Architecture

## Goal

This project is migrating gameplay UI from legacy UGUI and multiple ad-hoc UI Toolkit documents to one shared UI Toolkit runtime architecture.

The target architecture is designed to:

- follow Unity UI Toolkit runtime layout practices
- keep layout in UXML and USS rather than runtime positioning patches
- make modal input ownership predictable
- reduce regressions when changing one screen
- support phased migration without redesigning gameplay systems

## Current Problem

The current gameplay UI evolved into multiple separate runtime `UIDocument` instances:

- HUD
- pitstop modal
- act transition / boon selection
- additional legacy UGUI overlays

That causes several recurring problems:

- document sorting order conflicts
- pointer picking conflicts between passive HUD and interactive modal UI
- duplicated panel setup logic
- runtime layout overrides fighting USS
- regressions where fixing one screen breaks another

## Target Runtime Architecture

Use one shared gameplay UI Toolkit root document for all gameplay-facing UI.

### Layer Model

The shared root contains these top-level layers:

1. `hud-layer`
- persistent gameplay HUD
- top status bar
- tile inspector
- contextual pitstop info card

2. `context-layer`
- temporary non-modal contextual overlays
- route preview helpers
- future contextual panels that should not block the whole screen

3. `modal-layer`
- interactive modal states
- pitstop event choice and result
- act intermission
- boon selection
- victory
- defeat

4. `debug-layer`
- debug-only overlays
- development instrumentation

## Ownership Rules

### Root Ownership

One controller owns the shared gameplay root:

- `HexGameplayUiRootController`

Its responsibilities are:

- load the shared `UIDocument`
- load root UXML and root USS
- expose named layer containers
- centralize shared `PanelSettings`
- control layer visibility and modal interaction state

It should not contain gameplay-specific presentation logic.

### Feature Ownership

Each feature keeps its own presenter/controller, but binds into the shared root instead of creating its own `UIDocument`.

Examples:

- `HexHudDocumentController` binds into `hud-layer`
- `PitstopEventModalPresenter` binds into `modal-layer`
- `HexActTransitionModalPresenter` binds into `modal-layer`
- future victory / defeat presenters bind into `modal-layer`

### Layout Ownership

Visual structure belongs in:

- UXML for hierarchy
- USS for layout, spacing, typography, state classes, and visual variants

C# should only:

- populate dynamic content
- toggle visibility
- toggle state classes
- respond to user input
- forward gameplay callbacks

C# should not:

- compute routine layout geometry
- force modal width/position unless there is a strong technical constraint
- fight USS sizing with inline values as a normal workflow

## Input And Picking Rules

### Passive Layers

Passive layers should not intercept pointer input.

Examples:

- `hud-layer` when only showing status information
- `context-layer` when showing non-interactive inspection panels

These layers should use non-interactive picking by default.

### Modal Layer

When a modal is open:

- `modal-layer` becomes the active interactive layer
- world input should already be blocked by gameplay state
- modal controls must own pointer and keyboard focus
- HUD can remain visible but must be visually secondary and non-blocking

Interactive modals must not rely on document sorting conflicts to receive input.

## Styling Strategy

Use shared USS tokens and component classes so screens feel consistent.

Shared primitives should cover:

- colors
- typography scale
- spacing scale
- card surfaces
- chip variants
- button variants
- muted gameplay-under-modal state

Feature-specific USS should extend shared primitives instead of redefining the entire system.

## Migration Rules

The migration must happen in small slices.

Each slice should:

1. add the new shared-root implementation for one area
2. wire existing gameplay data into it
3. test that area in isolation
4. remove the old path for that area only after validation

Do not migrate multiple interactive flows at once unless they share the same infrastructure and are already stable.

## Migration Order

### Slice 1: Foundation

- create shared gameplay UI root assets
- create `HexGameplayUiRootController`
- create empty runtime layers
- initialize the shared root at runtime
- no intentional visual change yet

### Slice 2: HUD

- migrate top status bar
- keep the top status bar in `hud-layer`
- remove standalone HUD `UIDocument` path after validation

### Slice 3: Pitstop Modal

- move pitstop event choice and result UI into `modal-layer`
- remove standalone pitstop modal `UIDocument`
- validate choice and result flow

### Slice 4: Act Transition

- move act intermission screen into `modal-layer`
- move boon selection screen into `modal-layer`
- remove standalone act transition `UIDocument`

### Slice 5: Run-End Modals

- move victory UI into `modal-layer`
- move defeat UI into `modal-layer`
- remove remaining legacy gameplay modal paths

### Slice 6: Cleanup

- move tile inspector and pitstop intel into `context-layer`
- remove dead UGUI gameplay UI objects
- remove unused modal/document bootstrap code
- remove temporary debug migration helpers

## Testing Standard

Every migrated slice must be tested before the next one starts.

### Per-Slice Checklist

- correct visibility state
- correct input behavior
- correct focus/selection behavior
- no overlap with unrelated gameplay UI
- no missing resource/status data
- no regression in startup flow
- no regression in resolution scaling at the main target size

### Full Gameplay UI Checklist

- startup HUD
- tile selection
- route preview
- pitstop inspection
- pitstop event choice
- pitstop result
- act 1 to act 2 transition
- act 2 to act 3 boon selection
- victory
- defeat

## Current Status

This document defines the target architecture and phased migration plan.

Current implementation status:

- slice 1 complete: shared gameplay UI root and runtime layers
- slice 2 complete: top status bar migrated to the shared HUD layer
- slice 3 complete: pitstop event choice/result migrated to the shared modal layer
- slice 4 complete: act intermission and boon selection migrated to the shared modal layer
- slice 5 in progress: run-end modal migration
- slice 6 in progress: context-layer extraction and final gameplay UI cleanup
