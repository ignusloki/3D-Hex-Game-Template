# Project Tasks

Last updated: 2026-05-04

## Current Core State

The project is a playable single-scene Unity 6 prototype with:

- boot-to-main-menu flow
- full-screen main menu built in UI Toolkit
- one-scene gameplay start/restart/return-to-menu flow through `HexGameBootstrap`
- procedural hex map generation
- one-step caravan movement and click-to-inspect map interaction
- `Food`, `Morale`, and `Gold` resource economy
- fog of war
- obstacle spawning/contact pressure tied to newly discovered fog tiles
- pitstop placement and choice events
- multi-act run flow across Acts 1, 2, and 3
- act-complete and boon-selection modals
- stacked boon runtime by Act 3
- locked-family Act 3 nemesis activation
- dedicated Victory and Game Over overlays
- shared UI Toolkit root and shared UI transition system

## Current Priority

### 1. Final Map Screen UI Regression

Use `Docs/Example.png` as the visual reference for the final map UI check.

Verify:

- top status bar readability at 1920x1080
- center selected-hex status text
- resource chips
- selected hex visual state
- right-side unified inspector
- biome, pitstop, obstacle, nemesis, and unknown-tile inspector states

### 2. Content Expansion

The systems are in place, but authored content is still thin.

High-value content work:

- author Echo final-act boon pool
- author Corruptor final-act boon pool
- expand final-act boon balance beyond the current Hunter set
- replace mock quest-marker interactions with real quest data and state
- add real outpost behavior if outposts stay in scope

### 3. Presentation Polish

Remaining polish work:

- improve start and goal readability
- continue biome visual cleanup
- improve obstacle/fog state readability
- replace temporary caravan/goal visuals with stronger art
- add caravan movement animation later
- review final UI flow consistency across main menu, act transition, victory, and game over

### 4. Balance And Testing

Balance remains open across:

- starting resources
- terrain travel costs
- obstacle pressure within the documented spawn/cap rules
- pitstop density
- event rewards
- act grants
- nemesis pressure

Testing gaps:

- full gameplay-loop automated tests
- act transition regression tests
- victory/defeat/retry/return-to-menu tests
- map-generation validity tests
- boon-data validation tests

## Deferred Work

- Audio layer
- Full quest system
- Advanced movement animation
- Advanced render-pipeline migration
- More complex map-object behaviors
- Save/load
- Settings menu
