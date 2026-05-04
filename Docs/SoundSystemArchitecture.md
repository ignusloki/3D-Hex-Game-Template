# Sound System Architecture

Last updated: 2026-05-04

## Purpose

The sound system provides a small, editor-configurable audio layer for the
single-scene game flow. It supports:

- looping music by game stage
- UI sound effects
- gameplay sound effects
- master/music/SFX volume controls
- safe no-op behavior when clips are not assigned yet

The system is intentionally simple. It uses standard Unity `AudioSource`
components and a single ScriptableObject library for clip assignment.

## Runtime Components

### `HexAudioSystem`

`HexAudioSystem` is the scene-level audio service.

Scene location:

```text
Systems
└── Audio System
    └── HexAudioSystem
```

Responsibilities:

- load or reference `HexAudioLibrary`
- create missing runtime `AudioSource` children
- play and crossfade music stages
- play UI and gameplay SFX one-shots
- apply master/music/SFX volume
- persist volume values with `PlayerPrefs`
- tolerate missing clips without throwing errors

Runtime AudioSources:

- `Music Source A`
- `Music Source B`
- `UI SFX Source`
- `Gameplay SFX Source`

Music uses two sources so one track can fade into another without hard cuts.
SFX are split into UI and gameplay sources so they can be routed separately
later if needed.

### `HexAudioLibrary`

`HexAudioLibrary` is the editor-facing clip configuration asset.

Path:

```text
Assets/Resources/Audio/HexAudioLibrary.asset
```

It contains:

- `musicEntries`: maps `HexMusicStage` values to looping music clips
- `sfxEntries`: maps `HexSfxId` values to one-shot SFX clips

Design rule:

- gameplay and UI code should call enum ids, not direct clip references
- designers can replace clips in the Unity editor without code changes

## Asset Folders

Current audio asset folders:

```text
Assets/Audio
Assets/Audio/Music
Assets/Audio/SFX
Assets/Audio/Mixers
Assets/Resources/Audio
```

Music and SFX files can be imported into the music/SFX folders, then assigned to
entries in `HexAudioLibrary.asset`.

## Music Stages

`HexMusicStage` values:

- `None`
- `MainMenu`
- `Act1`
- `Act2`
- `Act3`
- `Victory`
- `Defeat`

Current stage mapping:

- main menu visible -> `MainMenu`
- gameplay activated in Act 1 -> `Act1`
- gameplay activated in Act 2 -> `Act2`
- gameplay activated in Act 3 -> `Act3`
- victory overlay shown -> `Victory`
- game over overlay shown -> `Defeat`
- return to menu -> `MainMenu`

Duplicate requests for the currently-playing stage are ignored when the same
clip is already playing.

## SFX IDs

`HexSfxId` values:

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
- `ObstacleSelect`
- `ObstacleTravel`
- `NemesisMove`

Important serialization rule:

- append new SFX ids to the end of the enum when possible
- do not insert new ids in the middle unless the audio library asset is migrated
- Unity serializes enum values numerically inside `HexAudioLibrary.asset`

## UI SFX

UI Toolkit controls use `HexAudioUiBinder`.

Default behavior:

- pointer enter -> `UiHover`
- click -> `UiClick`

Currently bound controls:

- main menu buttons
- pitstop modal continue button
- pitstop option buttons
- act transition intermission button
- boon selection cards and continue button
- victory continue button
- game over retry and return buttons
- simple action modal button

Pitstop choice selection plays `PitstopChoice` from the actual option-selection
handler so it is tied to the command path, not only to UI Toolkit click bubbling.

Boon cards play `BoonSelect` when clicked.

## Gameplay SFX

Gameplay SFX are fired from `PlayerController`, where gameplay context is
available.

Current behavior:

- selecting a normal visible hex -> `HexSelect`
- selecting a visible obstacle hex -> `ObstacleSelect`
- moving to a normal destination -> `CaravanMove`
- moving into an obstacle/contact destination -> `ObstacleTravel`
- nemesis turn result with `Moved == true` -> `NemesisMove`

Obstacle selection uses:

```csharp
HexObstacleController.TryGetVisibleObstacle(tile.Coordinates, out _)
```

Obstacle travel uses:

```csharp
HexObstacleTurnResult.ContactResult.HasContact
```

If a boon ignores the obstacle penalty, the movement still plays
`ObstacleTravel` because the caravan still interacted with an obstacle.

## Volume Model

`HexAudioSystem` exposes normalized volume controls:

```csharp
SetMasterVolume(float normalized);
SetMusicVolume(float normalized);
SetSfxVolume(float normalized);
```

Values use `0.0` to `1.0`.

Current persistence keys:

- `audio.masterVolume`
- `audio.musicVolume`
- `audio.sfxVolume`

If an `AudioMixer` is assigned, the system writes to these mixer parameters:

- `MasterVolume`
- `MusicVolume`
- `SfxVolume`

If no mixer is assigned, the system applies volume directly to the runtime
AudioSources.

## Boot Integration

`HexGameBootstrap` resolves and initializes `HexAudioSystem` during boot.

This ensures audio is ready before the main menu or gameplay flow starts.

The audio system can also resolve itself through:

```csharp
HexAudioSystem.ResolveShared(owner)
```

If the scene object is missing, it can create an `Audio System` GameObject at
runtime. The scene object is still preferred because it exposes configuration in
the editor.

## Missing Clip Behavior

Missing clips are allowed during development.

If a music or SFX entry has no clip:

- playback no-ops
- one missing-clip diagnostic is logged per missing id/stage when logging is enabled
- gameplay continues normally

This lets incomplete audio libraries exist without breaking Play Mode.

## How To Add Or Replace Sounds

1. Import the audio file into `Assets/Audio/Music` or `Assets/Audio/SFX`.
2. Open `Assets/Resources/Audio/HexAudioLibrary.asset`.
3. Assign the clip to the desired `musicEntries` or `sfxEntries` row.
4. Adjust per-entry volume or pitch range if needed.
5. Enter Play Mode and trigger the relevant flow.

No code change is needed when replacing clips for existing ids.

## Extension Guidelines

Add a new SFX id only when a real caller needs it.

Preferred process:

1. Append the new value to `HexSfxId`.
2. Add a new entry to `HexAudioLibrary.asset`.
3. Call `HexAudioSystem.PlaySfx` or `PlayGameplaySfx` from the gameplay/UI
   owner that has the right context.
4. Keep missing clips safe.

Avoid:

- direct clip references in gameplay code
- per-screen ad-hoc AudioSources
- USS/UXML-driven sound behavior
- inserting enum values in the middle of `HexSfxId`
