# Sound System Architecture

Last updated: 2026-05-02

## Purpose

This document defines a simple, flexible Unity sound system for the current
single-scene game architecture.

The system should support:

- editor-configurable music and SFX assets
- master/music/SFX volume control
- UI SFX
- gameplay/animation SFX
- looping background music by game stage
- safe no-op behavior while clips are still missing

## Current Project Context

The project currently has no runtime audio system.

Relevant architecture:

- the game uses one scene
- `HexGameBootstrap` owns early boot flow and main menu/gameplay destination
- UI is built through a shared UI Toolkit root
- major flow states already exist:
  - Main Menu
  - Act 1
  - Act 2
  - Act 3
  - Victory
  - Game Over

The sound system should integrate with this architecture rather than adding a
separate scene, separate bootstrap path, or per-screen ad-hoc AudioSources.

## Design Goals

- Keep it small.
- Use standard Unity audio primitives.
- Keep audio clip assignment in the Unity editor.
- Avoid hardcoded asset paths in gameplay code.
- Allow missing clips during development without errors.
- Keep music and one-shot SFX separated.
- Make volume settings controllable globally.

## Non-Goals

Do not build:

- a complex adaptive music system
- spatial/3D audio for this first pass
- runtime audio asset loading by addressables
- a full settings menu
- audio ducking/sidechain behavior
- per-biome ambience layering

Those can be added later if the game needs them.

## Runtime Components

### `HexAudioSystem`

Scene-level MonoBehaviour responsible for audio playback.

Recommended location:

- `Systems/Audio System`

Responsibilities:

- own AudioSources
- own current music stage
- play music loops
- stop/fade music
- play SFX one-shots
- apply volume settings
- expose simple public methods for presenters/controllers

Suggested serialized fields:

- `HexAudioLibrary audioLibrary`
- `AudioMixer audioMixer`
- `AudioMixerGroup musicMixerGroup`
- `AudioMixerGroup sfxMixerGroup`
- `AudioSource musicSourceA`
- `AudioSource musicSourceB`
- `AudioSource uiSfxSource`
- `AudioSource gameplaySfxSource`
- `int sfxPoolSize`
- `float defaultMusicFadeSeconds`

Use two music sources so music can crossfade without stopping abruptly.

### `HexAudioLibrary`

ScriptableObject that stores editor-configurable audio clips and per-clip
settings.

Recommended path:

- `Assets/Resources/Audio/HexAudioLibrary.asset`

Responsibilities:

- map music stages to loop clips
- map SFX IDs to clips
- expose per-entry volume/pitch variation
- keep all clip assignment editor-driven

Suggested fields:

- `List<HexMusicEntry> musicEntries`
- `List<HexSfxEntry> sfxEntries`

### `HexMusicEntry`

Data type for looping music.

Fields:

- `HexMusicStage stage`
- `AudioClip clip`
- `float volume`
- `float fadeInSeconds`
- `float fadeOutSeconds`
- `bool loop`

Default `loop` should be true for stage music.

### `HexSfxEntry`

Data type for one-shot sounds.

Fields:

- `HexSfxId id`
- `AudioClip clip`
- `float volume`
- `Vector2 pitchRange`
- `bool interruptSameId`

`interruptSameId` is useful for repeated UI hover/click sounds if needed, but it
should default to false.

## Enums

### `HexMusicStage`

Initial values:

- `None`
- `MainMenu`
- `Act1`
- `Act2`
- `Act3`
- `Victory`
- `Defeat`

### `HexSfxId`

Initial values should cover the first integration points:

- `UiHover`
- `UiClick`
- `UiBack`
- `ModalOpen`
- `ModalClose`
- `TransitionStart`
- `TransitionComplete`
- `HexSelect`
- `RoutePreview`
- `CaravanMove`
- `PitstopOpen`
- `PitstopChoice`
- `ActComplete`
- `BoonSelect`
- `Victory`
- `Defeat`

Keep the enum small. Add values only when a real caller needs them.

## Mixer And Volume Model

Use one Unity `AudioMixer` with three exposed volume parameters:

- `MasterVolume`
- `MusicVolume`
- `SfxVolume`

Recommended groups:

- `Master`
- `Music`
- `SFX`

Optional later:

- `UI` under `SFX`
- `Gameplay` under `SFX`

For the first pass, a single SFX group is enough because the user only requested
overall, music, and SFX volume control.

### Volume API

`HexAudioSystem` should expose:

- `SetMasterVolume(float normalized)`
- `SetMusicVolume(float normalized)`
- `SetSfxVolume(float normalized)`
- `float MasterVolume { get; }`
- `float MusicVolume { get; }`
- `float SfxVolume { get; }`

Use normalized values from `0.0` to `1.0` in code/UI.

Convert normalized volume to mixer dB:

- `0.0` -> `-80 dB`
- `1.0` -> `0 dB`
- use logarithmic conversion for values above zero

Store settings with `PlayerPrefs`:

- `audio.masterVolume`
- `audio.musicVolume`
- `audio.sfxVolume`

This allows a future settings menu to bind to the same API without changing the
audio backend.

## Playback API

Recommended public methods:

```csharp
public void PlayMusic(HexMusicStage stage, float? fadeSeconds = null);
public void StopMusic(float? fadeSeconds = null);
public void PlaySfx(HexSfxId id);
public void PlaySfx(HexSfxId id, float volumeScale);
public void SetMasterVolume(float normalized);
public void SetMusicVolume(float normalized);
public void SetSfxVolume(float normalized);
```

Music calls should ignore duplicate requests for the current stage unless
`forceRestart` is added later.

SFX calls should no-op if the clip is missing.

## Integration Points

### Bootstrap And Game Stage Music

`HexGameBootstrap` should resolve `HexAudioSystem` during boot.

Recommended stage mapping:

- boot to main menu -> `MainMenu`
- New Game / Act 1 gameplay -> `Act1`
- Act 2 gameplay -> `Act2`
- Act 3 gameplay -> `Act3`
- Victory overlay -> `Victory`
- Game Over overlay -> `Defeat`
- Return to menu -> `MainMenu`

Act music can be resolved from `HexActTransitionService.GetCurrentActNumber()`.

### UI Toolkit Buttons

Use small helper binding methods so presenters do not duplicate callback wiring:

- `HexAudioUiBinder.BindButton(Button button)`
- or `HexAudioSystem.BindButtonAudio(Button button)`

Behavior:

- pointer enter -> `UiHover`
- click -> `UiClick`

Do not put sound logic in USS or UXML.

### Modal And Flow SFX

Recommended first callers:

- main menu button click
- pitstop modal open
- pitstop choice selected
- act-complete screen open
- boon selected
- victory shown
- defeat shown

### Gameplay / Animation SFX

Recommended first callers:

- selected hex changes -> `HexSelect`
- valid route preview created -> `RoutePreview`
- caravan movement begins or completes -> `CaravanMove`

If movement animation is added later, the same `CaravanMove` hook can move from
instant movement completion to animation start/footstep timing.

## Scene Setup

Add one scene object:

```text
Systems
└── Audio System
    └── HexAudioSystem
```

AudioSources:

- `Music Source A`
- `Music Source B`
- `UI SFX Source`
- `Gameplay SFX Source`
- optional pooled SFX sources as children

The component should auto-create missing child AudioSources in editor or at
runtime only if that keeps setup simple. Prefer serialized scene references once
the object exists.

## Asset Setup

Recommended folders:

```text
Assets/Audio
Assets/Audio/Music
Assets/Audio/SFX
Assets/Audio/Mixers
Assets/Resources/Audio
```

Required assets:

- `HexAudioMixer.mixer`
- `HexAudioLibrary.asset`

Music and SFX clips can remain unassigned while the system is implemented.

## Implementation Slices

### Slice 1 - Foundation

- add `HexMusicStage`
- add `HexSfxId`
- add `HexAudioLibrary`
- add `HexAudioSystem`
- support serialized clip configuration
- support master/music/SFX volume API
- create editor-safe no-op behavior for missing clips

Test:

- add the component to the scene
- assign or leave clips empty
- call volume setters without errors

### Slice 2 - Mixer And Scene Setup

- create AudioMixer with Master/Music/SFX groups
- expose volume parameters
- create scene `Audio System` object
- wire sources and mixer groups
- load/save volume with PlayerPrefs

Test:

- changing serialized/default volumes changes mixer values
- missing clips do not throw errors

### Slice 3 - Music Stage Integration

- hook `HexAudioSystem` into `HexGameBootstrap`
- play `MainMenu` music on menu boot
- play `Act1`, `Act2`, or `Act3` music when gameplay activates
- play `Victory` and `Defeat` music from run-end overlay flow
- return to `MainMenu` music on return-to-menu

Test:

- music stage switches across menu, gameplay, victory, and defeat
- duplicate stage requests do not restart the same track

### Slice 4 - UI SFX

- add button audio helper
- bind main menu buttons
- bind pitstop modal buttons
- bind act transition and boon selection buttons
- bind victory/game over buttons

Test:

- hover/click UI sounds play once per event
- buttons remain silent if clips are missing

### Slice 5 - Gameplay SFX

- add hex select SFX
- add route preview SFX if it does not become noisy
- add caravan move SFX
- add pitstop open/choice SFX
- add victory/defeat sting SFX

Test:

- repeated map interaction does not spam sounds excessively
- gameplay SFX respect SFX volume

### Slice 6 - QA And Tuning

- verify all audio respects master volume
- verify music respects music volume
- verify UI/gameplay SFX respect SFX volume
- verify scene reloads do not create duplicate audio systems
- verify return-to-menu, retry, and act-to-act flows switch music correctly

## Risks And Constraints

- No clips are currently available in the project, so early implementation must
  tolerate missing assets.
- UI Toolkit has no USS-driven sound events; presenters must bind button events.
- Repeated hover/selection sounds can become annoying; keep SFX hooks deliberate.
- The game reloads the same scene for major flows, so the audio system must avoid
  duplicate instances and should initialize cleanly on reload.

## Recommended First Implementation

Start with Slice 1 and Slice 2 together only if scene setup is straightforward.
Do not wire all gameplay/UI callers until the core mixer, library, and sources are
stable.
