# Visual Polish Audit

Last updated: 2026-04-19
Project snapshot: `main` at commit `7352afb`
Purpose: handoff document for a future AI or collaborator focused on improving presentation quality without reworking the gameplay foundation.

## 1. Scope And Intent

This project is already a playable prototype with:

- procedural hex-map generation
- a 3-act run structure
- boon selection and Act 3 nemesis behavior
- pitstops, events, fog of war, obstacles, and map objects

The next step is presentation polish, not systems replacement.

The goal of the next polish pass should be:

- improve readability
- improve atmosphere
- improve moment-to-moment feedback
- improve the sense of progression between acts
- keep the existing gameplay rules intact unless a visual improvement absolutely requires a tiny supporting code change

## 2. Current Presentation Baseline

These are hard facts from the current project state.

### 2.1 Scene-Level Facts

The main gameplay scene is `Assets/Scenes/Main.unity`.

Loaded scene hierarchy currently has 4 root objects:

- `World`
- `Systems`
- `UI`
- `Archive`

Within the loaded scene:

- there is `1` active camera
- there is `1` active directional light
- there is `1` overlay canvas
- there are `0` scene audio sources
- there are `0` scene animators
- there are `0` scene particle systems

Important implication:

- the game currently has almost no audiovisual feedback layer beyond mesh visibility, text, and material color

### 2.2 Rendering Facts

The current render setup is very prototype-oriented:

- built-in render pipeline
- no custom render pipeline asset in `ProjectSettings/GraphicsSettings.asset`
- active color space is `Gamma`
- default skybox is still in use
- fog is disabled
- ambient mode is `Skybox`
- the live directional light currently has `shadows = None`
- active quality level is `Good`
- anti-aliasing is `0`

Main camera baseline:

- `fov = 60`
- perspective camera
- background color is a flat blue tone
- current transform is roughly top-down / angled strategy view

Immediate read:

- the board works functionally
- the world has very little depth, mood, or material richness
- act transitions are mechanically different, but the render layer does not yet sell those differences strongly

### 2.3 UI Facts

The scene UI is still extremely lightweight.

Static scene UI currently contains:

- `7` legacy `UnityEngine.UI.Text` elements
- `2` `Image` elements
- `0` TMPro text components in the loaded scene
- `0` static scene buttons
- `0` static scene layout groups on the HUD panels

The canvas does use:

- `CanvasScaler`
- `Scale With Screen Size`
- reference resolution `1920x1080`

The HUD is mostly text blocks inside:

- `HUD Panel`
- `Pitstop Info Panel`

Key issue:

- the persistent HUD is still a text-first debug-style interface rather than a production-quality game HUD

### 2.4 Modal UI Facts

The pitstop event modal and run-state modal are not prefab-based UI screens.

They are assembled in code at runtime in:

- `Assets/Scripts/Hex/PitstopEventModalPresenter.cs`
- `Assets/Scripts/Hex/HexRunStateModalPresenter.cs`

Important technical detail:

- both presenters still use legacy `Text`
- both presenters resolve the builtin font through `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`
- this is one of the reasons the UI still feels prototype-grade and required repeated overlap fixes

The run-state modal is in better shape structurally than the HUD because it now creates:

- scroll areas
- a vertical layout group
- content size fitting

But visually it is still:

- flat
- text-heavy
- very dark
- not strongly themed

### 2.5 Audio Facts

There is currently no real audio layer in the project.

Observed baseline:

- no `wav`, `mp3`, `ogg`, `aiff`, or mixer assets in `Assets`
- no scene `AudioSource`
- no meaningful audio playback code in gameplay systems

That means:

- there are no move sounds
- no UI clicks
- no pitstop/event cues
- no boon or transition stingers
- no ambient loops
- no nemesis warning language through sound

### 2.6 Animation And FX Facts

There are currently no authored animation clips or animator controllers in `Assets`.

Observed baseline:

- no `.anim` files
- no `.controller` files
- no particle systems in the loaded scene

Important code note:

- `Assets/Scripts/Hex/HexObstaclePresenter.cs` explicitly disables `Animator` components on spawned obstacle visuals

That means:

- even if an obstacle prefab is given an animator later, it will not animate until that behavior is intentionally changed

### 2.7 Core Art Inventory Facts

The project already contains a large amount of model source content:

- about `325` `.fbx` files
- `18` `.prefab` files
- `5` core terrain materials in `Assets/Art/Materials`

Main art sources already present:

- `Assets/Prefabs/Kay/*`
- `Assets/Prefabs/Kenny/*`

Current gameplay terrain, however, is still driven by only five simple tile prefabs:

- `Assets/Prefabs/Grass_Tile.prefab`
- `Assets/Prefabs/Forest_TIle.prefab`
- `Assets/Prefabs/Desert_Tile.prefab`
- `Assets/Prefabs/Mountain_Tile.prefab`
- `Assets/Prefabs/Water_Tile.prefab`

Those prefabs currently use:

- one mesh each
- one renderer each
- one collider each
- one flat `Standard` material each

The five current terrain materials are:

- `grass.mat`
- `forest.mat`
- `desert.mat`
- `mountain.mat`
- `water.mat`

They are all simple `Standard` shader materials with flat color-driven identity.

Important implication:

- there is enough existing model inventory to improve visuals without importing a new asset pack immediately
- but that inventory is not yet being used in a cohesive art direction pass

## 3. Current Visual Read

Based on the current implementation and recent runtime screenshots from testing, the game currently reads as:

- mechanically clear enough to play
- visually flat
- atmospheric only in a minimal sense
- more like a board-test environment than a finished world

The strongest current presentation qualities are:

- clean board readability at a prototype level
- simple low-poly asset language that can still be developed into a cohesive style
- strong systemic differentiation between map objects and terrain types

The weakest current presentation qualities are:

- lack of lighting depth
- lack of audio feedback
- lack of animation and VFX
- UI that still looks temporary
- world space that ends in a flat void / simple background rather than a deliberate horizon treatment
- limited visual distinction between “functional object” and “important object”

## 4. Biggest Polish Problems By Category

### 4.1 UI

Current issues:

- legacy text instead of a deliberate typography system
- manual runtime-built panels instead of reusable UI prefabs
- text-heavy presentation
- weak hierarchy between title, body, reward summary, and action
- no iconography for resources
- HUD looks like debug output rather than a shipping strategy/survival interface

Most important UI goals:

- build a consistent UI language
- make act transition and boon selection feel ceremonial and important
- make resources, warnings, and route state readable at a glance
- stop solving layout with manual rect math whenever a prefab/layout solution would be cleaner

### 4.2 Lighting And Rendering

Current issues:

- only one directional light
- no shadows on the active light
- default skybox
- gamma color space
- no fog
- no post-processing stack
- no scene mood shifts per act

Most important render goals:

- give the board volume
- create a stronger horizon / background treatment
- support act-specific mood
- improve depth separation between terrain, units, map objects, and fog

### 4.3 World, Models, And Terrain

Current issues:

- terrain identity is still mostly color-based
- many available Kay/Kenny assets are not yet integrated into actual play
- hunter and other runtime visuals still fall back to primitive or near-primitive presentation
- important objects need better silhouette language
- the board edge / empty world treatment is not yet authored as a strong visual frame

Most important world goals:

- make each biome feel distinct at a glance
- make Act 2 immediately read as a harsh desert crossing
- make Act 3 feel like the mythic final stretch
- increase landmark value through visual composition, not just rules

### 4.4 SFX And Music

Current issues:

- the game is effectively silent from a gameplay-feedback standpoint
- there is no audio identity for movement, danger, boon selection, or act transitions

Most important audio goals:

- establish a basic feedback layer first
- then add atmosphere
- then add escalation cues for pressure systems like the nemesis

### 4.5 Animation And Motion

Current issues:

- units and objects feel static
- interaction confirmation comes mostly from text changes
- no particles, no movement accents, no reveal effects, no arrival effects

Most important motion goals:

- give player actions a readable response
- give the nemesis a stronger presence
- make major state changes feel consequential

## 5. Key Technical Hotspots

These are the first files another AI should inspect before changing presentation.

### 5.1 Scene And Core Setup

- `Assets/Scenes/Main.unity`
- `Assets/Scripts/Hex/HexCameraController.cs`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/ProjectSettings.asset`

### 5.2 UI

- `Assets/Scripts/Hex/HexHudPresenter.cs`
- `Assets/Scripts/Hex/HexRunStateModalPresenter.cs`
- `Assets/Scripts/Hex/PitstopEventModalPresenter.cs`
- `Assets/Scripts/Hex/CaravanMetricsController.cs`

### 5.3 Tile, Fog, And Board Readability

- `Assets/Scripts/Hex/HexagonTile.cs`
- `Assets/Scripts/Hex/HexFogOfWarController.cs`
- `Assets/Scripts/Hex/MapGenerator.cs`
- `Assets/Art/Materials/*.mat`
- `Assets/Prefabs/*_Tile.prefab`

### 5.4 Runtime Visual Actors

- `Assets/Scripts/Hex/HexNemesisPresenter.cs`
- `Assets/Scripts/Hex/HexObstaclePresenter.cs`
- `Assets/Resources/NemesisProfiles/*.asset`
- `Assets/Resources/MapObjects/Definitions/*.asset`

### 5.5 Relevant Data Notes

`HunterNemesisProfile.asset` currently has:

- `actorPrefab = null`

That means:

- the Hunter is still using fallback presentation instead of a real authored enemy visual

`QuestMarker.asset` already supports:

- a prefab reference
- a height offset
- a scale override

That means:

- map-object presentation can be improved without redesigning map generation

## 6. Recommended Art Direction Direction

The game fantasy should not look like generic clean fantasy tactics.

It should feel like:

- a mythic migration
- a people crossing dangerous land
- survival under spiritual pressure
- scarce safety and costly movement
- a final promised threshold in Act 3

That suggests the visual direction should favor:

- austere but memorable terrain
- strong silhouettes
- restrained palette per act
- high readability over dense clutter
- ceremonial presentation for boon and act transition moments
- subtle ritual / omen / fate motifs rather than loud high-fantasy spectacle everywhere

## 7. Recommended Improvement Roadmap

This is the order I would recommend for a future AI session.

### Pass 1: UI Foundation

Do this first because it is visible everywhere and currently the weakest polished layer.

Tasks:

- replace legacy `Text` with a consistent text system
- create a reusable modal prefab style instead of building every screen procedurally
- create resource row with icons and stronger hierarchy
- make boon options look like selectable cards, not stacked text blocks
- improve spacing, padding, and readability on all overlays
- define one visual UI kit for:
  - HUD
  - pitstop events
  - run-state modals
  - act transitions
  - boon cards

Important caution:

- do not break current gameplay flow while reworking the visuals
- if possible, keep existing presenters but let them drive prefab-based UI

### Pass 2: Lighting And Camera Foundation

Do this second because it will improve every screenshot immediately.

Tasks:

- enable and tune directional-light shadows
- replace default skybox or camera background with a deliberate horizon solution
- add act-sensitive ambient and fog settings
- tune camera framing and zoom so the board feels more deliberate and less accidental
- consider mild camera easing for pan/zoom/reset

Decision point:

- either stay on built-in render pipeline for lower risk
- or explicitly choose a controlled URP migration if the user agrees to higher rendering risk

Recommendation:

- for a polish-first pass, stay on built-in first and exhaust the easy wins before migrating pipeline

### Pass 3: Board Readability And Terrain Identity

Do this third because it improves gameplay clarity and atmosphere at the same time.

Tasks:

- give each biome stronger shape language, not just different colors
- integrate more of the existing Kay/Kenny low-poly environment assets into terrain and landmark presentation
- make start, goal, pitstops, quest markers, and nemesis tiles visually unmistakable
- add better edge-of-world treatment so the board feels framed

Specific high-value targets:

- stronger desert dressing for Act 2
- stronger final-journey tone for Act 3
- better fog-of-war visual treatment

### Pass 4: Motion And VFX

Do this fourth because the game currently lacks audiovisual punctuation.

Tasks:

- caravan move feedback
- hover / selection / path-preview pulses
- pitstop arrival effect
- boon choice selection response
- nemesis spawn / movement / concealment feedback
- corruption spread effects if Corruptor is enabled later
- reveal / hide behavior for Hunter grass concealment

Good low-risk approach:

- start with transform-based motion and simple particles
- only introduce animator-driven behavior where it clearly pays off

Important caution:

- obstacle visuals currently disable animators in `HexObstaclePresenter.cs`
- that file must be updated if animated obstacle prefabs are desired

### Pass 5: Audio Foundation

Do this fifth because the project currently has no sound language at all.

Tasks:

- add a basic audio manager and mixer structure
- add UI click / confirm / cancel sounds
- add move, invalid move, arrival, and resource-change sounds
- add pitstop, event, boon, and transition stingers
- add at least one ambient loop per act
- add nemesis warning and pressure cues for Act 3

Priority rule:

- first add feedback SFX
- then add ambient / music
- then add layered pressure cues

### Pass 6: Transition And Finale Presentation

Do this once the base presentation stack is stronger.

Tasks:

- make act transitions feel like meaningful chapter breaks
- give each boon card stronger visual identity
- give Act 3 start more gravity
- give final victory more payoff

This pass should align with the game’s mythic tone rather than generic sci-fi or RPG menu language.

## 8. Concrete Improvement Ideas By Domain

### 8.1 UI Improvement Ideas

- Use a dedicated card treatment for boon choices.
- Give each boon family a color accent and icon language.
- Turn the resource display into compact visual chips instead of plain text lines.
- Add icons or badges for:
  - Food
  - Morale
  - Gold
  - pitstop type
  - boon family
  - act number
- Keep the HUD narrow and legible instead of text-dense.
- Reserve large full-screen overlays for:
  - pitstop events
  - act transitions
  - victory / defeat

### 8.2 Lighting Improvement Ideas

- Warm, hopeful daylight in Act 1.
- Dry, high-contrast, harsh sun in Act 2.
- Cooler or more ominous mythic-final lighting in Act 3.
- Use soft fog or atmospheric haze to break the empty-background look.
- Add stronger specular and contrast separation only where it helps readability.

### 8.3 Terrain And Model Improvement Ideas

- Keep the low-poly style rather than mixing incompatible realism.
- Reuse the existing Kay/Kenny asset libraries before importing new packs.
- Add small biome dressing clusters rather than huge clutter fields.
- Give the goal a stronger landmark treatment.
- Give the caravan a more authored silhouette than a generic placeholder.
- Replace the hunter capsule / fallback actor with a deliberate profile prefab.

### 8.4 VFX Improvement Ideas

- Dust on caravan move.
- Water shimmer or subtle ripples.
- Selection ring pulse.
- Boon card glow on hover / select.
- Fog reveal dissolve instead of pure binary state.
- Nemesis visibility change effect when hidden or obscured by boon rules.

### 8.5 Audio Improvement Ideas

- one click family for UI
- one movement family for caravan traversal
- one pitstop reward family
- one threat family for nemesis cues
- one ambient bed per act
- one transition / boon ritual cue family

## 9. Constraints And Risks

### 9.1 Low-Risk Improvements

- UI prefab redesign
- better spacing and typography
- custom fonts
- icons
- shadows on the current light
- better skybox / background
- replacing placeholder prefabs with existing art assets
- adding simple SFX
- adding simple particles

### 9.2 Medium-Risk Improvements

- changing fog-of-war visual rules
- changing camera behavior significantly
- introducing many animated prefabs without checking runtime presenters
- large-scale scene hierarchy rework

### 9.3 High-Risk Improvements

- migrating the whole project to URP without a controlled plan
- changing color space from Gamma to Linear without testing materials and UI
- replacing UI logic and visual logic simultaneously in one pass

## 10. Suggested Acceptance Criteria For A First Visual Polish Pass

A good first polish milestone would mean:

- the game still plays exactly the same
- the HUD feels intentional rather than debug-like
- act transitions look like chapter breaks
- each biome reads instantly
- the Hunter is visually memorable
- start, goal, pitstops, quest markers, and hazards are unmistakable
- there is at least a minimal SFX layer for feedback
- there is at least a minimal motion / VFX layer for actions and pressure
- Act 2 clearly feels desert-biased visually
- Act 3 clearly feels like the final crossing

## 11. Suggested Prompt Guidance For The Next AI Session

If another AI session is going to implement polish work, it should be told:

- do not redesign gameplay systems
- prefer improving presentation on top of existing systems
- prefer existing asset libraries already inside the repo before importing new art
- inspect `Main.unity`, the modal presenters, the HUD presenter, the tile system, and the nemesis presenter first
- keep changes testable in small slices
- preserve editor configurability where possible
- explicitly ask before attempting a full render-pipeline migration

## 12. Short Summary

The project already has enough gameplay and enough raw low-poly asset inventory to support a strong presentation pass.

The main missing pieces are not “more mechanics.”

They are:

- visual language
- lighting
- audio
- motion
- stronger use of the art already present in the project

The fastest path to a better-looking game is:

- better UI kit
- better lighting and background treatment
- stronger board readability
- minimal but effective VFX
- foundational SFX

That should happen before any risky rendering migration.
