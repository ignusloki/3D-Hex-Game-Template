# UI Animation System Architecture

## Purpose

The game now uses one shared UI Toolkit root for gameplay UI, but transition animations are still owned by individual flows. The Game Over screen has its own fade scheduler, timing settings, interaction lockout, and element opacity sequence. That solved the immediate screen, but copying the same logic into Victory, main menu, act transitions, and pitstops would create duplicated behavior and inconsistent timing.

This document defines a small shared UI animation system for screen and modal transitions.

## Current State

The current UI foundation is correct:

- `HexGameplayUiRootController` owns the shared UI Toolkit root.
- Gameplay UI is split into `hud`, `context`, `modal`, and `debug` layers.
- Feature presenters bind their UXML into the shared root.
- Modal presenters own their content and callbacks.

The current animation gap:

- Game Over owns a custom transition loop.
- Victory still uses the generic instant simple-action modal.
- Pitstop and act transition modals mostly appear immediately.
- Future main menu transitions do not have a shared runtime path yet.

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

## Migration Plan

### Slice 1: Extract Shared Transition Player

Create the generic transition runtime without changing any visible behavior.

Work:

- add `HexUiTransitionPlayer`
- add `HexUiTransitionProfile`
- add fade track data structures
- support opacity-only tracks first
- support ease-out and linear easing
- support completion callback
- support cancellation
- support interaction lock callback

Acceptance:

- no existing screen behavior changes
- build passes

### Slice 2: Migrate Game Over To Shared Player

Replace the custom Game Over scheduler with the shared player.

Work:

- convert current Game Over timings into a `BlackoutThenReveal` profile
- map Game Over UXML elements into a target set
- keep current visual result
- keep current button lockout behavior
- keep existing retry callback

Acceptance:

- Game Over still fades to black first
- background and UI reveal in the same order
- buttons remain disabled until fully visible
- no duplicate trigger behavior

### Slice 3: Build Victory Screen On The Same Profile

Before polishing Victory visually, route it through the same transition system.

Work:

- replace or extend the simple-action Victory path
- create Victory-specific UXML/USS if needed
- reuse `BlackoutThenReveal` or a Victory variant profile
- keep Retry behavior unchanged

Acceptance:

- Victory does not use a copied Game Over scheduler
- Victory can be tuned from a profile
- Victory blocks gameplay input immediately

### Slice 4: Migrate Act Transition Screens

Use the transition player for the act intermission and boon selection screens.

Work:

- add target sets for the Act Complete page and Boon Selection page
- use `ChapterPageReveal`
- lock buttons until reveal completes
- avoid resizing or layout changes during animation

Acceptance:

- Act Complete appears through shared animation
- Boon Selection appears through shared animation
- switching from intermission to boon selection remains a two-screen flow

### Slice 5: Migrate Pitstop Modal

Add a lightweight modal fade, not a cinematic blackout.

Work:

- use `SimpleModalFade`
- animate overlay/shell opacity
- keep choice and result logic unchanged
- ensure options cannot be clicked until the modal is ready

Acceptance:

- pitstop modal feels polished but fast
- existing choice/result callbacks remain unchanged

### Slice 6: Add Global Transition Layer

Prepare for menu and screen-level transitions.

Work:

- add `global-transition-layer` to shared UI root
- create a reusable full-screen blackout element
- add `GlobalFadeToBlack` profile
- expose callbacks for "run action while black"

Acceptance:

- global transition can fade to black, execute a callback, then fade out
- does not depend on a specific modal

### Slice 7: Apply To Main Menu Flows

Use the global layer for future menu flow.

Work:

- starting game to main menu: fade in menu after initial black
- main menu to Act 1: fade to black, initialize gameplay, fade into gameplay
- return to main menu: fade to black, hide/reset gameplay, show menu, fade in

Acceptance:

- menu/game transitions use the same runtime system
- no separate custom menu animation scheduler is introduced

### Slice 8: Cleanup

Remove duplicated transition code and deprecated settings.

Work:

- remove Game Over-specific scheduler helpers
- remove one-off transition settings that are replaced by profiles
- keep only screen-specific target mapping
- update docs with final implementation notes

Acceptance:

- one transition runner owns UI fade scheduling
- presenters only bind data, open/close screens, and register callbacks

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

Before polishing Victory, implement Slice 1 and Slice 2:

1. Build the shared transition player.
2. Migrate Game Over to it with no visual changes.
3. Then build Victory on top of the shared system.

This gives a tested reference path before applying the system to more flows.
