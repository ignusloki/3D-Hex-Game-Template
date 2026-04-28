# UI Animation System Architecture

## Purpose

The game uses one shared UI Toolkit root for gameplay UI, and the migrated transition flows now share one fade scheduler through `HexUiTransitionPlayer`. This document records the migration architecture, completed work, and remaining boundaries so future UI flows do not reintroduce duplicated transition logic.

The system is intentionally small: feature presenters still decide what content to show, while shared transition utilities decide how prepared UI Toolkit elements fade in or out.

## Current State

The current UI foundation is correct:

- `HexGameplayUiRootController` owns the shared UI Toolkit root.
- Gameplay UI is split into `hud`, `context`, `modal`, and `debug` layers.
- Feature presenters bind their UXML into the shared root.
- Modal presenters own their content and callbacks.

The remaining animation gap:

- Victory still uses the generic instant simple-action modal and is deferred to a separate Victory branch.
- A real main menu presenter and menu game state do not exist yet, so menu transitions are currently infrastructure-only.

Completed migration state:

- Game Over uses `HexUiTransitionPlayer` and retains its editor-facing timing component as a configuration adapter.
- Act Complete and Boon Selection use the shared `ChapterPageReveal` path.
- Pitstop modal opening uses the shared `SimpleModalFade` path.
- The shared UI root has a global transition layer for future menu and scene-like flow transitions.

Implementation snapshot:

- Shared runtime: `HexUiTransitionPlayer`, `HexUiTransitionProfile`, and `HexUiTransitionTargetSet`.
- Shared profile helpers: `HexUiTransitionProfile.CreateRuntimeProfile(...)` handles runtime profile cloning, track filtering, and reverse fades.
- Global layer: `HexGameplayUiLayerId.GlobalTransition`, `HexGlobalUiTransitionController`, and `GlobalFadeToBlack`.
- Menu hooks: `HexMainMenuTransitionService` exposes named transition entry points, but real menu wiring is deferred until a main menu exists.

## Goal

Create one reusable UI transition runner that can animate UI Toolkit elements by profile while keeping each flow responsible for its own content and gameplay decisions.

The system should support:

- full-screen fade to black
- background image fade-in
- scrim fade-in
- title and panel staggered reveals
- simple modal fade-in
- button interaction lockout until animation completes
- cancellation / duplicate-trigger protection
- editor-tweakable timing profiles

## Non-Goals

This should not become a large game state machine.

The animation system should not:

- decide when the player wins or loses
- decide which modal should open
- own gameplay state
- own act transition data
- own pitstop choices
- compute layout geometry
- replace UXML or USS layout responsibility

Feature presenters still decide what to show. The animation system decides how a prepared set of UI elements appears or disappears.

## Proposed Runtime Components

### `HexUiTransitionPlayer`

Shared runtime service that plays a transition against UI Toolkit `VisualElement` targets.

Responsibilities:

- run fade tracks using unscaled time
- evaluate easing
- set target opacity
- invoke completion callbacks
- prevent duplicate playback of the same transition
- cancel or complete an active transition safely
- enable interaction only after configured completion

Recommended location:

- A component near `HexGameplayUiRootController`, or a helper owned by it.

Important rule:

- The player should be generic. It should know element targets and timing tracks, not "Game Over", "Victory", or "Pitstop" concepts.

### `HexUiTransitionProfile`

ScriptableObject containing reusable timing data.

Suggested fields:

- profile id / name
- easing mode
- optional total lock duration override
- list of fade tracks

Each fade track should include:

- target key
- start time
- duration
- start opacity
- end opacity

Example target keys:

- `blackout`
- `background`
- `scrim`
- `title`
- `separator`
- `panel`
- `description`
- `primary-button`
- `secondary-button`

### `HexUiTransitionTargetSet`

Small runtime mapping between profile target keys and actual `VisualElement` instances for a specific screen.

Example for Game Over:

- `blackout` -> `GameOverBlackout`
- `background` -> `GameOverBackground`
- `scrim` -> `GameOverScrim`
- `title` -> `GameOverTitle`
- `separator` -> `GameOverSeparator`
- `panel` -> `GameOverPanel`
- `description` -> `GameOverDescription`
- `primary-button` -> `RetryButton`
- `secondary-button` -> `ReturnToTitleButton`

This keeps profile timing reusable without requiring every screen to use the same UXML names.

### `HexUiModalTransitionController`

Optional thin helper for common modal behavior:

- show modal layer
- prepare initial opacity
- play transition
- block buttons while playing
- enable buttons after completion
- hide modal layer on close

This should be small. It should not replace the existing presenters.

### Global Transition Layer

For menu and scene-level transitions, add a dedicated full-screen transition layer above normal UI.

Recommended root order:

- gameplay/menu content
- modal layer
- global transition layer
- debug layer

Use this for:

- starting game to main menu
- main menu to Act 1
- return to main menu
- any future scene-like transition without changing scenes

This prevents every screen from needing its own blackout element when the transition is not visually part of that screen.

## Recommended Profiles

### `BlackoutThenReveal`

Use for cinematic end-state screens.

Sequence:

- fade blackout to full black
- fade background and scrim in
- fade title
- fade panel shell
- fade description
- fade buttons

Use for:

- Game Over
- Victory

### `SimpleModalFade`

Use for small gameplay modals.

Sequence:

- fade scrim
- fade modal shell
- enable controls

Use for:

- pitstop modal
- mock quest modal
- future small confirmation modals

### `ChapterPageReveal`

Use for act transitions.

Sequence:

- fade dark overlay
- fade page shell
- fade page content
- enable CTA

Use for:

- Act Complete page
- Boon Selection page

### `GlobalFadeToBlack`

Use for screen-to-screen flow changes.

Sequence:

- fade global blackout in
- run flow callback while black
- optionally fade new screen in
- fade blackout out

Use for:

- app start to main menu
- main menu to Act 1
- return to main menu

## Completed Migration Work

### Slice 1: Extract Shared Transition Player

Complete. The shared transition infrastructure now exists and is available for flow migration.

### Slice 2: Migrate Game Over To Shared Player

Complete. Game Over now uses the shared transition player while preserving the existing editor-facing timing settings, visual sequence, retry behavior, and interaction lockout.

### Slice 3: Migrate Act Transition Screens

Complete. Act Complete and Boon Selection now use the shared transition player with a `ChapterPageReveal` profile and transition-time button lockout.

### Slice 4: Migrate Pitstop Modal

Complete. Pitstop choice opening now uses `SimpleModalFade` with transition-time option lockout. The choice-to-result swap intentionally does not animate.

### Slice 5: Add Global Transition Layer

Complete. The shared UI root now has a `GlobalTransition` layer, a runtime full-screen blackout element, a `GlobalFadeToBlack` profile, and `HexGlobalUiTransitionController` callback APIs for running flow changes while the screen is black.

### Slice 6: Apply To Main Menu Flows

Infrastructure complete for the current project state. There is no main menu presenter or menu game state yet, so this slice cannot be wired to a real menu flow without inventing a fake one. `HexMainMenuTransitionService` now exposes named entry points for:

- startup to main menu
- main menu to Act 1
- return to main menu

When the real menu screen exists, those methods should be wired to the menu presenter and game-flow controller callbacks.

## Remaining Migration Plan

### Slice 7: Build Victory Screen On The Same Profile

Skipped in this branch. Victory will be handled in a separate branch and should not be changed as part of this cleanup pass.

Future work:

- replace or extend the simple-action Victory path
- create Victory-specific UXML/USS if needed
- reuse `BlackoutThenReveal` or a Victory variant profile
- keep Retry behavior unchanged

Future acceptance:

- Victory does not use a copied Game Over scheduler
- Victory can be tuned from a profile
- Victory blocks gameplay input immediately

### Slice 8: Cleanup

Complete. Runtime profile cloning, track filtering, and reverse-track generation now live in `HexUiTransitionProfile.CreateRuntimeProfile(...)`, so migrated flows no longer duplicate that construction logic.

Notes:

- Victory code was intentionally left untouched.
- `HexGameOverTransitionSettings` is retained because it exposes the user-requested Game Over timing controls in the scene inspector; it no longer owns a custom scheduler.
- Each migrated presenter still owns only its screen-specific target mapping, content binding, and callbacks.

## Flow Mapping

### Starting Game To Main Menu

Use:

- `GlobalFadeToBlack`
- main menu reveal profile

Notes:

- this should use the global transition layer, not a modal-local blackout

### Main Menu To Act 1

Use:

- `GlobalFadeToBlack`

Sequence:

- fade to black
- initialize Act 1/gameplay state
- show gameplay UI
- fade out from black

### Triggering A Pitstop Modal

Use:

- `SimpleModalFade`

Sequence:

- gameplay input is already blocked by pitstop state
- modal layer appears
- scrim and shell fade in
- options become clickable after fade completes

### Transition Between Acts

Use:

- `ChapterPageReveal`

Sequence:

- Act Complete page reveal
- CTA moves to Boon Selection page
- Boon Selection page reveal
- Continue advances act

### Victory Screen

Use:

- `BlackoutThenReveal`

Notes:

- should share Game Over transition mechanics
- can have a different background, color palette, and text content

### Game Over Screen

Use:

- `BlackoutThenReveal`

Notes:

- current behavior becomes the reference implementation
- migrate without visual change first

### Return To Main Menu

Use:

- `GlobalFadeToBlack`

Sequence:

- block gameplay/menu input
- fade to black
- reset/hide gameplay
- show main menu
- fade from black

## Architecture Rules

- UXML owns hierarchy.
- USS owns layout and default style.
- Transition profiles own timings.
- Presenters own content and callbacks.
- The transition player owns opacity playback and completion.
- Gameplay systems own game state.
- Do not put gameplay decisions inside animation profiles.
- Do not duplicate transition loops per screen.

## Implementation Notes

- Use UI Toolkit `VisualElement.schedule.Execute(...).Every(16)` or one centralized MonoBehaviour update loop.
- Use `Time.unscaledTime` so transitions still run while gameplay is frozen.
- Use opacity for the first version. Do not add scale, movement, or blur until the fade system is stable.
- Buttons and interactive controls should use `PickingMode.Ignore` while a transition is playing.
- Completion time should be calculated as the maximum `start + duration` across tracks.
- If a target key is missing, log a warning and continue unless the track is marked required.
- Keep default profiles in Resources or assigned scene assets so they are editor-tweakable.

## Risks

- Over-centralizing flow control would make the system harder to reason about. Keep it animation-only.
- Using target names as strings can become fragile. Prefer constants or serialized target mappings where practical.
- A global transition layer can hide modal bugs if overused. Use it only for screen-level transitions.
- Migrating all flows at once would create avoidable regression risk. Use the slice plan.

## Recommended Next Step

Open a dedicated Victory branch and build the Victory screen on top of the existing shared transition system. Use Game Over as the reference for target mapping and lifecycle, but keep Victory-specific UXML/USS and presentation code isolated from the completed migration cleanup.
