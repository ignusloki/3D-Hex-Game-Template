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

    public static HexUiTransitionProfile CreateRuntimeProfile(
        HexUiTransitionProfile source,
        string fallbackProfileId,
        HexUiTransitionEasing fallbackEasing,
        IReadOnlyList<HexUiTransitionFadeTrack> fallbackTracks,
        Func<HexUiTransitionFadeTrack, bool> includeTrack = null,
        bool reverseTracks = false)
    {
        HexUiTransitionProfile runtimeProfile = CreateInstance<HexUiTransitionProfile>();
        runtimeProfile.hideFlags = HideFlags.DontSave;
        runtimeProfile.profileId = ResolveRuntimeProfileId(source, fallbackProfileId);
        runtimeProfile.easing = source != null ? source.easing : fallbackEasing;
        runtimeProfile.overrideInteractionUnlockTime = source != null && source.overrideInteractionUnlockTime;
        runtimeProfile.interactionUnlockTime = source != null ? source.interactionUnlockTime : 0f;
        runtimeProfile.fadeTracks = new List<HexUiTransitionFadeTrack>();

        IReadOnlyList<HexUiTransitionFadeTrack> tracks = source != null
            ? source.FadeTracks
            : fallbackTracks ?? Array.Empty<HexUiTransitionFadeTrack>();
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey || (includeTrack != null && !includeTrack(track)))
            {
                continue;
            }

            runtimeProfile.fadeTracks.Add(CopyTrack(track, reverseTracks));
        }

        return runtimeProfile;
    }

    private static string ResolveRuntimeProfileId(HexUiTransitionProfile source, string fallbackProfileId)
    {
        if (source != null && !string.IsNullOrWhiteSpace(source.profileId))
        {
            return source.profileId.Trim();
        }

        return string.IsNullOrWhiteSpace(fallbackProfileId)
            ? "ui-transition"
            : fallbackProfileId.Trim();
    }

    private static HexUiTransitionFadeTrack CopyTrack(HexUiTransitionFadeTrack source, bool reverse)
    {
        return new HexUiTransitionFadeTrack
        {
            targetKey = string.IsNullOrWhiteSpace(source.targetKey) ? string.Empty : source.targetKey.Trim(),
            startTime = Mathf.Max(0f, source.startTime),
            duration = Mathf.Max(0f, source.duration),
            startOpacity = reverse ? Mathf.Clamp01(source.endOpacity) : Mathf.Clamp01(source.startOpacity),
            endOpacity = reverse ? Mathf.Clamp01(source.startOpacity) : Mathf.Clamp01(source.endOpacity),
            isRequired = source.isRequired
        };
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
