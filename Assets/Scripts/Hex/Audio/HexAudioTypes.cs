using System;
using UnityEngine;

public enum HexMusicStage
{
    None,
    MainMenu,
    Act1,
    Act2,
    Act3,
    Victory,
    Defeat
}

public enum HexSfxId
{
    UiHover,
    UiClick,
    UiBack,
    ModalOpen,
    ModalClose,
    TransitionStart,
    TransitionComplete,
    HexSelect,
    RoutePreview,
    CaravanMove,
    PitstopOpen,
    PitstopChoice,
    ActComplete,
    BoonSelect,
    Victory,
    Defeat
}

[Serializable]
public sealed class HexMusicEntry
{
    public HexMusicStage stage = HexMusicStage.None;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float fadeInSeconds = 0.75f;
    [Min(0f)] public float fadeOutSeconds = 0.75f;
    public bool loop = true;
}

[Serializable]
public sealed class HexSfxEntry
{
    public HexSfxId id = HexSfxId.UiClick;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 pitchRange = Vector2.one;
    public bool interruptSameId;
}
