using System;
using System.Collections.Generic;
using UnityEngine;

public enum HexUiTransitionEasing
{
    Linear,
    EaseOut
}

[Serializable]
public sealed class HexUiTransitionFadeTrack
{
    public string targetKey;
    [Min(0f)] public float startTime;
    [Min(0f)] public float duration = 0.2f;
    [Range(0f, 1f)] public float startOpacity;
    [Range(0f, 1f)] public float endOpacity = 1f;
    public bool isRequired;

    public bool HasTargetKey => !string.IsNullOrWhiteSpace(targetKey);

    public float ResolveStartTime()
    {
        return Mathf.Max(0f, startTime);
    }

    public float ResolveDuration()
    {
        return Mathf.Max(0f, duration);
    }

    public float ResolveEndTime()
    {
        return ResolveStartTime() + ResolveDuration();
    }

    public float EvaluateOpacity(float elapsed, HexUiTransitionEasing easing)
    {
        float resolvedStart = ResolveStartTime();
        if (elapsed <= resolvedStart)
        {
            return Mathf.Clamp01(startOpacity);
        }

        float resolvedDuration = ResolveDuration();
        if (resolvedDuration <= 0f)
        {
            return Mathf.Clamp01(endOpacity);
        }

        float progress = Mathf.Clamp01((elapsed - resolvedStart) / resolvedDuration);
        if (easing == HexUiTransitionEasing.EaseOut)
        {
            progress = 1f - ((1f - progress) * (1f - progress));
        }

        return Mathf.Clamp01(Mathf.Lerp(startOpacity, endOpacity, progress));
    }
}

[CreateAssetMenu(
    fileName = "UiTransitionProfile",
    menuName = "Hex/UI/Transition Profile")]
public sealed class HexUiTransitionProfile : ScriptableObject
{
    public string profileId = "ui-transition";
    public HexUiTransitionEasing easing = HexUiTransitionEasing.EaseOut;
    public bool overrideInteractionUnlockTime;
    [Min(0f)] public float interactionUnlockTime;
    public List<HexUiTransitionFadeTrack> fadeTracks = new();

    public IReadOnlyList<HexUiTransitionFadeTrack> FadeTracks
    {
        get
        {
            if (fadeTracks != null)
            {
                return fadeTracks;
            }

            return Array.Empty<HexUiTransitionFadeTrack>();
        }
    }

    public float ResolveCompleteTime()
    {
        float completeTime = 0f;
        IReadOnlyList<HexUiTransitionFadeTrack> tracks = FadeTracks;
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey)
            {
                continue;
            }

            completeTime = Mathf.Max(completeTime, track.ResolveEndTime());
        }

        return completeTime;
    }

    public float ResolveInteractionUnlockTime()
    {
        return overrideInteractionUnlockTime
            ? Mathf.Max(0f, interactionUnlockTime)
            : ResolveCompleteTime();
    }

    private void OnValidate()
    {
        profileId = string.IsNullOrWhiteSpace(profileId) ? name : profileId.Trim();
        interactionUnlockTime = Mathf.Max(0f, interactionUnlockTime);
        fadeTracks ??= new List<HexUiTransitionFadeTrack>();

        for (int index = 0; index < fadeTracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = fadeTracks[index];
            if (track == null)
            {
                continue;
            }

            track.targetKey = string.IsNullOrWhiteSpace(track.targetKey) ? string.Empty : track.targetKey.Trim();
            track.startTime = Mathf.Max(0f, track.startTime);
            track.duration = Mathf.Max(0f, track.duration);
            track.startOpacity = Mathf.Clamp01(track.startOpacity);
            track.endOpacity = Mathf.Clamp01(track.endOpacity);
        }
    }
}
