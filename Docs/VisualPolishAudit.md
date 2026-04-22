# Visual Polish Audit

Last updated: 2026-04-22
Project snapshot: `codex/visual-pixel-direction-audit` at commit `91f4984`
Purpose: handoff document for a future AI or collaborator focused on improving presentation quality without reworking the gameplay foundation.

Important compatibility note:

- `Docs/UIArchitecture.md` is the source of truth for gameplay UI structure
- this audit should guide visual quality and polish direction on top of that architecture
- future UI work should extend the shared UI Toolkit root and layer model, not reintroduce legacy gameplay UGUI or scene-owned gameplay canvases

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
- there is `0` gameplay overlay canvas in the scene
- there is `1` scene `EventSystem`
- there are `0` scene audio sources
- there are `0` scene animators
- there are `0` scene particle systems

Gameplay UI is now created through the shared UI Toolkit runtime root rather than a scene canvas.

Important implication:

- the game currently has almost no audiovisual feedback layer beyond mesh visibility, UI state changes, and material color

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

The gameplay UI architecture is now UI Toolkit-first.

Static scene UI currently contains:

- `0` legacy gameplay `UnityEngine.UI.Text` elements
- `0` legacy gameplay `Image` elements
- `0` gameplay overlay canvases
- `1` scene `EventSystem`

Gameplay UI is created and mounted at runtime through:

- `HexGameplayUiRootController`
- `HexHudDocumentController`
- shared `UIDocument` + UXML + USS assets in `Assets/Resources/UI/*`

The shared gameplay root currently contains these layers:

- `hud-layer`
- `context-layer`
- `modal-layer`
- `debug-layer`

The persistent gameplay UI now has a stable structural separation between:

- top HUD
- tile/context inspection
- gameplay modals

Key issue:

- the architecture is now correct, but the visual kit still needs stronger identity, typography, iconography, and final-art treatment

### 2.4 Modal UI Facts

Gameplay modals now live inside the shared UI Toolkit `modal-layer`.

Current gameplay modal presenters are:

- `Assets/Scripts/Hex/PitstopEventModalPresenter.cs`
- `Assets/Scripts/Hex/HexRunStateModalPresenter.cs`

Those presenters now load and bind UI Toolkit assets from:

- `Assets/Resources/UI/Modal/HexPitstopEventModal.*`
- `Assets/Resources/UI/Modal/HexActTransitionModal.*`
- `Assets/Resources/UI/Modal/HexSimpleActionModal.*`

Important technical detail:

- gameplay modals no longer use legacy runtime `Text`
- gameplay modals no longer depend on scene canvas sorting to receive input
- the shared root owns modal visibility and interaction state

That removes the old architecture problem.

The remaining problem is visual quality:

- some screens still lean too dark and too flat
- placeholder art is still present in some card and journal/image regions
- the UI kit is structurally coherent, but it still needs stronger themed polish

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

Important correction after scene inspection:

- the active `Map Generator` in `Assets/Scenes/Main.unity` is already configured to use the higher-quality Kenny biome prefabs for runtime terrain visuals
- the older simple tile prefabs still exist, but they are not the main live terrain presentation path in the active scene
- the current live terrain visuals pull their materials from the imported Kenny FBX assets, not from a cohesive custom terrain-shader setup

The five simple terrain prefabs are still present:

- `Assets/Prefabs/Grass_Tile.prefab`
- `Assets/Prefabs/Forest_TIle.prefab`
- `Assets/Prefabs/Desert_Tile.prefab`
- `Assets/Prefabs/Mountain_Tile.prefab`
- `Assets/Prefabs/Water_Tile.prefab`

Those fallback prefabs currently use:

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
- the terrain baseline is stronger than it first appeared
- the bigger presentation gap is now the layer above terrain:
  - HUD and modal UI
  - camera / lighting mood
  - caravan / goal / nemesis presentation
  - feedback, VFX, and motion

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

- shared UI Toolkit structure exists, but the visual language still needs refinement
- typography hierarchy is still serviceable rather than distinctive
- some modal surfaces are still flatter and darker than they should be
- placeholder art areas still need real image content
- iconography and accent language are still limited
- HUD readability is improved structurally, but it still needs more production-grade polish

Most important UI goals:

- build a consistent UI language
- make act transition and boon selection feel ceremonial and important
- make resources, warnings, and route state readable at a glance
- keep layout ownership in UXML and USS instead of reintroducing runtime layout patches

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

- `Assets/Scripts/Hex/UI/HexHudDocumentController.cs`
- `Assets/Scripts/Hex/PitstopEventModalPresenter.cs`
- `Assets/Scripts/Hex/HexRunStateModalPresenter.cs`
- `Assets/Resources/UI/Gameplay/*`
- `Assets/Resources/UI/Hud/*`
- `Assets/Resources/UI/Context/*`
- `Assets/Resources/UI/Modal/*`
- `Docs/UIArchitecture.md`

### 5.3 Tile, Fog, And Board Readability

- `Assets/Scripts/Hex/HexagonTile.cs`
- `Assets/Scripts/Hex/HexFogOfWarController.cs`
- `Assets/Scripts/Hex/MapGenerator.cs`
- `Assets/Art/Materials/*.mat`
- `Assets/Prefabs/*_Tile.prefab`
- `Assets/Prefabs/Kenny/Used/*`
- `Assets/Prefabs/Kenny/FBX format/*`

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

### 5.6 External Style Reference Evaluation

The project was compared against this external reference:

- `https://github.com/bababuyyy/unity-isometric-pixel-pipeline`

That reference is useful as inspiration, but it should **not** be treated as a direct integration target for this project.

Why it does not fit directly:

- it is built for `Unity 6` + `URP`
- it depends on a custom `ScriptableRendererFeature` low-resolution 5-pass pipeline
- it assumes an orthographic isometric camera with pixel snapping
- it assumes custom flat toon materials instead of the current mixed Standard / imported FBX material path
- it is optimized for a low-resolution pixel-art look, which creates board-readability risk for a hex survival / route-planning game

Specific incompatibilities with this project:

- current project render setup is still built-in, Gamma, and perspective
- current camera behavior allows free pan, yaw rotation, and zoom around a perspective board
- current runtime presentation code often relies on `renderer.material.color` and Standard-style material assumptions
- current tile-highlighting logic uses `_EmissionColor` and duplicated highlight materials
- active terrain uses imported Kenny FBX materials rather than a unified toon-shader material family

Conclusion:

- do not migrate this project to that pipeline as part of the current polish phase
- do not switch the core game camera to a full orthographic isometric presentation at this stage
- do borrow selected ideas that are compatible with the current project structure

Approved selective borrow list for this project:

- flatter palette-driven lighting
- restrained outlines on key actors only
- cloud shadow mood treatment
- low-frequency palette variation on terrain

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

That direction should now borrow a **small subset** of the isometric pixel-pipeline reference without importing its whole rendering stack.

Use these influences:

- flatter palette-driven lighting instead of richer physically based shading
- selective outline treatment for the caravan, goal, nemesis, and possibly quest / boon-critical objects
- slow cloud-shadow movement to give the board atmosphere and pressure
- low-frequency terrain color variation so biomes feel authored instead of flat

Do **not** copy these parts of the reference into the main game:

- full low-resolution pixel render pipeline
- full-scene outline pass on every object
- orthographic isometric camera conversion
- mandatory URP migration just to match the demo visually

## 7. Recommended Improvement Roadmap

This is the order I would recommend for a future AI session.

Current scope decision:

- audio is intentionally deferred to a later project stage
- current work should stay focused on visual identity, atmosphere, readability, and feedback

### Pass 1: UI Foundation

Do this first because the architecture is now in place and the next gain is visual quality.

Tasks:

- refine the shared UI Toolkit design language instead of replacing the architecture
- strengthen typography, spacing, iconography, and accent use across the existing shared layers
- create resource row with icons and stronger hierarchy
- replace remaining placeholder art regions with deliberate content
- improve spacing, padding, and readability on all overlays
- define one visual UI kit for:
  - HUD
  - pitstop events
  - run-state modals
  - act transitions
  - boon cards

Important caution:

- do not break current gameplay flow while reworking the visuals
- do not reintroduce separate gameplay canvases or legacy gameplay UGUI
- prefer extending the current UXML + USS + shared-root approach

### Pass 2: Lighting And Stylization Foundation

Do this second because it will improve every screenshot immediately.

Tasks:

- enable and tune directional-light shadows
- replace default skybox or camera background with a deliberate horizon solution
- push the scene toward flatter palette-driven lighting while preserving readability
- prototype cloud-shadow mood treatment that can run on the current board without a full pipeline swap
- add act-sensitive ambient and fog settings
- tune camera framing and zoom so the board feels more deliberate and less accidental
- consider mild camera easing for pan/zoom/reset

Decision point:

- either stay on built-in render pipeline for lower risk
- or explicitly choose a controlled URP migration if the user agrees to higher rendering risk

Recommendation:

- for a polish-first pass, stay on built-in first and exhaust the easy wins before migrating pipeline

### Pass 3: Key Actor Readability

Do this third because the caravan, goal, nemesis, and hazards are still the weakest live presentation layer.

Tasks:

- replace fallback caravan and goal presentation with authored visuals from the existing repo inventory
- replace nemesis fallback primitives with stronger silhouettes and deliberate placement treatment
- test restrained outline treatment on key actors only:
  - caravan
  - goal
  - nemesis
  - optionally quest-critical markers
- improve hazard and obstacle readability without outlining the entire board

Important caution:

- outline treatment should stay selective
- a full-screen edge-detect outline pass is high-risk for this game because it can clutter the board

### Pass 4: Board Readability And Terrain Identity

Do this fourth because it improves gameplay clarity and atmosphere at the same time.

Tasks:

- give each biome stronger shape language, not just different colors
- add low-frequency palette variation to terrain so repeated tiles do not feel flat or copy-pasted
- integrate more of the existing Kay/Kenny low-poly environment assets into terrain and landmark presentation
- make start, goal, pitstops, quest markers, and nemesis tiles visually unmistakable
- add better edge-of-world treatment so the board feels framed

Specific high-value targets:

- stronger desert dressing for Act 2
- stronger final-journey tone for Act 3
- better fog-of-war visual treatment

### Pass 5: Transition And Modal Presentation

Do this fifth once the core world mood and actor readability are stronger.

Tasks:

- make act transitions feel like meaningful chapter breaks
- give each boon card stronger visual identity
- give Act 3 start more gravity
- give final victory more payoff
- keep the HUD and modal language visually cohesive with the new world palette direction

This pass should align with the game’s mythic tone rather than generic sci-fi or RPG menu language.

### Pass 6: Motion And VFX

Do this sixth because the game currently lacks presentation punctuation even after the static look improves.

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

### Deferred: Audio Foundation

This work is still valid, but it is intentionally deferred by current scope.

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
- Push toward flatter palette-driven lighting rather than richer physically based shading.
- Use cloud shadows as a slow-moving mood layer rather than a heavy simulation feature.
- Use soft fog or atmospheric haze to break the empty-background look.
- Add stronger specular and contrast separation only where it helps readability.

### 8.3 Terrain And Model Improvement Ideas

- Keep the low-poly style rather than mixing incompatible realism.
- Reuse the existing Kay/Kenny asset libraries before importing new packs.
- Treat the active Kenny terrain as the base and build variation on top of it instead of discarding it.
- Add small biome dressing clusters rather than huge clutter fields.
- Add low-frequency terrain color variation so repeated biome pieces read as intentional regions.
- Give the goal a stronger landmark treatment.
- Give the caravan a more authored silhouette than a generic placeholder.
- Replace the hunter capsule / fallback actor with a deliberate profile prefab.
- If outlines are used, keep them on key actors only instead of the whole board.

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
- adding low-frequency terrain palette variation
- adding restrained outlines on key actors only
- adding cloud-shadow mood treatment in a controlled way
- adding simple particles

### 9.2 Medium-Risk Improvements

- changing fog-of-war visual rules
- changing camera behavior significantly
- introducing selective outline rendering if it requires custom replacement materials or extra render layers
- introducing many animated prefabs without checking runtime presenters
- large-scale scene hierarchy rework

### 9.3 High-Risk Improvements

- migrating the whole project to URP without a controlled plan
- attempting to copy the full `unity-isometric-pixel-pipeline` renderer into the main project
- converting the main game camera to a full orthographic isometric presentation without readability validation
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
- the board lighting feels flatter and more deliberate without losing gameplay readability
- cloud-shadow mood treatment adds atmosphere without obscuring tile information
- any outline treatment improves key-actor readability without cluttering the board
- there is at least a minimal motion / VFX layer for actions and pressure
- Act 2 clearly feels desert-biased visually
- Act 3 clearly feels like the final crossing

## 11. Suggested Prompt Guidance For The Next AI Session

If another AI session is going to implement polish work, it should be told:

- do not redesign gameplay systems
- prefer improving presentation on top of existing systems
- prefer existing asset libraries already inside the repo before importing new art
- inspect `Main.unity`, the modal presenters, the HUD presenter, the tile system, and the nemesis presenter first
- treat `Docs/UIArchitecture.md` as the baseline for all gameplay UI work
- do not reintroduce gameplay overlay canvases, legacy gameplay `Text`, or per-screen document ownership
- keep changes testable in small slices
- preserve editor configurability where possible
- audio is intentionally deferred for now
- explicitly ask before attempting a full render-pipeline migration
- do not copy the full `unity-isometric-pixel-pipeline` into the project
- only borrow these ideas from that reference unless the user later approves a separate rendering spike:
  - flatter palette-driven lighting
  - restrained outlines on key actors only
  - cloud shadow mood treatment
  - low-frequency palette variation on terrain

## 12. Short Summary

The project already has enough gameplay and enough raw low-poly asset inventory to support a strong presentation pass.

The main missing pieces are not “more mechanics.”

They are:

- visual language
- lighting
- motion
- stronger use of the art already present in the project

The fastest path to a better-looking game is:

- better UI kit
- flatter palette-driven lighting and better background treatment
- selective atmosphere borrowing from the pixel/isometric reference without copying its whole renderer
- stronger board readability
- minimal but effective VFX
- stronger presentation for caravan / goal / nemesis

Audio should be handled later in a separate stage.

That should happen before any risky rendering migration.
